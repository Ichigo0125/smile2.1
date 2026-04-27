using UnityEngine;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Globalization;

[RequireComponent(typeof(OVRFaceExpressions))]
public class ARKitReceiver : MonoBehaviour
{
    private OVRFaceExpressions faceExp;


    [Header("Send Settings")]
    public float sendFPS = 60f;
    public string remoteIP = "0.0.0.0";
    public int remotePort = 9000;

    [Header("Control Signal Defaults")]
    public string defaultTimelineEventName = "timeline_trigger";
    public string defaultTimelineClipName = "default";

    [Header("Head Pose Source")]
    public Transform headPoseSource;
    public bool useLocalHeadRotation = true;
    public bool zeroHeadRotationAtStartup = true;

    private float sendTimer = 0f;

    private readonly float[] arkitWeights = new float[(int)ARKitBlendshape.NoseSneerRight + 1];
    private readonly float[] eyeRot = new float[6];
    private readonly float[] headRot = new float[3];
    public float[] ARKitWeights => arkitWeights;
    private OVRPlugin.EyeGazesState gazeState = new OVRPlugin.EyeGazesState();
    private Quaternion headPoseReference = Quaternion.identity;
    private bool hasHeadPoseReference = false;

    // UDP
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private readonly CultureInfo inv = CultureInfo.InvariantCulture;

#if UNITY_ANDROID && !UNITY_EDITOR
    const string FACE = "com.oculus.permission.FACE_TRACKING";
    const string EYE  = "com.oculus.permission.EYE_TRACKING";
#endif

    void Awake()
    {
        faceExp = GetComponent<OVRFaceExpressions>();
        ResolveHeadPoseSource(logIfFound: true);

        try
        {
            udpClient = new UdpClient();
            IPAddress ip;
            if (!IPAddress.TryParse(remoteIP, out ip))
            {
                Debug.LogWarning($"[ARKitReceiver] Invalid IP '{remoteIP}', fallback to 127.0.0.1");
                ip = IPAddress.Loopback;
            }
            remoteEndPoint = new IPEndPoint(ip, remotePort);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ARKitReceiver] UDP init failed: {e.Message}");
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        bool hasFacePerm = UnityEngine.Android.Permission.HasUserAuthorizedPermission(FACE);
        bool hasEyePerm  = UnityEngine.Android.Permission.HasUserAuthorizedPermission(EYE);
        Debug.Log($"[Diag] Face permission : {hasFacePerm}, Eye permission : {hasEyePerm}");
#else
        Debug.Log("[Diag] Permissions N/A (Editor or non-Android build)");
#endif
    }

    IEnumerator Start()
    {
        yield return null;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(FACE))
            UnityEngine.Android.Permission.RequestUserPermission(FACE);
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(EYE))
            UnityEngine.Android.Permission.RequestUserPermission(EYE);

        for (int i = 0; i < 20; i++) yield return null;

        bool faceOK = UnityEngine.Android.Permission.HasUserAuthorizedPermission(FACE);
        bool eyeOK  = UnityEngine.Android.Permission.HasUserAuthorizedPermission(EYE);
        Debug.Log($"[Diag] After request → Face permission : {faceOK}, Eye permission : {eyeOK}");

        var sources = new OVRPlugin.FaceTrackingDataSource[] {
            OVRPlugin.FaceTrackingDataSource.Visual
        };

        try {
            OVRPlugin.StartFaceTracking2(sources);
            Debug.Log("[Diag] Called StartFaceTracking2");
        } catch (System.Exception e) {
            Debug.LogWarning($"[Diag] Start tracking failed: {e.Message}");
        }

        try {
            bool eyeStarted = OVRPlugin.StartEyeTracking();
            Debug.Log($"[Diag] StartEyeTracking: {eyeStarted}");
        }
        catch (System.Exception e) {
            Debug.LogWarning($"[Diag] StartEyeTracking failed: {e.Message}");
        }
#endif
    }

    void OnDestroy()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
    }

    void Update()
    {
        if (sendFPS <= 0f) return;

        if (!faceExp.FaceTrackingEnabled)
        {
            // Debug.LogWarning("[FaceTracking] Not enabled.");
            return;
        }
        if (!faceExp.ValidExpressions)
        {
            // Debug.Log("[FaceTracking] Expressions not valid yet...");
            return;
        }

        sendTimer += Time.deltaTime;
        float interval = 1.0f / sendFPS;
        if (sendTimer < interval) return;
        sendTimer -= interval;

        // 更新 ARKit 係數 + forward
        UpdateARKitWeights();
        TryUpdateEyeRotation();
        TryUpdateHeadRotation();
        SendARKitWeights();
    }

    private static float NormalizeDeg(float deg)
    {
        deg %= 360f;
        if (deg > 180f) deg -= 360f;
        if (deg < -180f) deg += 360f;
        return deg;
    }

    private bool TryUpdateHeadRotation()
    {
        // Use an explicit eye anchor / HMD transform instead of Camera.main world
        // rotation, because exported packages may land in projects with multiple
        // cameras or a rig root rotated near 180 degrees.
        if (!TryGetHeadPoseRotation(out Quaternion sourceRotation))
        {
            for (int i = 0; i < 3; i++) headRot[i] = 0f;
            return false;
        }

        if (zeroHeadRotationAtStartup && !hasHeadPoseReference)
        {
            headPoseReference = sourceRotation;
            hasHeadPoseReference = true;
            Debug.Log($"[ARKitReceiver] Head pose reference captured from {headPoseSource.name}");
        }

        Quaternion relativeRotation = hasHeadPoseReference
            ? Quaternion.Inverse(headPoseReference) * sourceRotation
            : sourceRotation;

        Vector3 eulerDeg = relativeRotation.eulerAngles;
        headRot[0] = NormalizeDeg(eulerDeg.x) * Mathf.Deg2Rad;
        headRot[1] = NormalizeDeg(eulerDeg.y) * Mathf.Deg2Rad;
        headRot[2] = NormalizeDeg(eulerDeg.z) * Mathf.Deg2Rad;
        return true;
    }

    [ContextMenu("Recenter Head Pose Reference")]
    public void RecenterHeadPoseReference()
    {
        if (!TryGetHeadPoseRotation(out Quaternion sourceRotation))
        {
            Debug.LogWarning("[ARKitReceiver] Cannot recenter head pose because no source was found.");
            return;
        }

        headPoseReference = sourceRotation;
        hasHeadPoseReference = true;
        Debug.Log($"[ARKitReceiver] Head pose recentered using {headPoseSource.name}");
    }

    private bool TryGetHeadPoseRotation(out Quaternion rotation)
    {
        rotation = Quaternion.identity;

        if (!ResolveHeadPoseSource(logIfFound: false) || headPoseSource == null)
        {
            return false;
        }

        rotation = useLocalHeadRotation ? headPoseSource.localRotation : headPoseSource.rotation;
        return true;
    }

    private bool ResolveHeadPoseSource(bool logIfFound)
    {
        if (headPoseSource != null)
        {
            return true;
        }

        var centerEye = GameObject.Find("CenterEyeAnchor");
        if (centerEye != null)
        {
            headPoseSource = centerEye.transform;
        }
        else
        {
            var cam = Camera.main;
            if (cam != null)
            {
                headPoseSource = cam.transform;
            }
        }

        if (headPoseSource != null && logIfFound)
        {
            Debug.Log(
                $"[ARKitReceiver] Using head pose source: {headPoseSource.name} " +
                $"(localRotation={useLocalHeadRotation})"
            );
        }

        return headPoseSource != null;
    }

    private void UpdateARKitWeights()
    {
        float W(OVRFaceExpressions.FaceExpression e)
            => faceExp.GetWeight(e);

        // ----- Eyebrows -----
        arkitWeights[(int)ARKitBlendshape.BrowDownLeft] =
            W(OVRFaceExpressions.FaceExpression.BrowLowererL);
        arkitWeights[(int)ARKitBlendshape.BrowDownRight] =
            W(OVRFaceExpressions.FaceExpression.BrowLowererR);
        arkitWeights[(int)ARKitBlendshape.BrowInnerUp] =
            0.5f * (
                W(OVRFaceExpressions.FaceExpression.InnerBrowRaiserL) +
                W(OVRFaceExpressions.FaceExpression.InnerBrowRaiserR)
            );
        arkitWeights[(int)ARKitBlendshape.BrowOuterUpLeft] =
            W(OVRFaceExpressions.FaceExpression.OuterBrowRaiserL);
        arkitWeights[(int)ARKitBlendshape.BrowOuterUpRight] =
            W(OVRFaceExpressions.FaceExpression.OuterBrowRaiserR);

        // ----- Cheeks -----
        arkitWeights[(int)ARKitBlendshape.CheekPuff] =
            0.5f * (
                W(OVRFaceExpressions.FaceExpression.CheekPuffL) +
                W(OVRFaceExpressions.FaceExpression.CheekPuffR)
            );
        arkitWeights[(int)ARKitBlendshape.CheekSquintLeft] =
            W(OVRFaceExpressions.FaceExpression.CheekRaiserL);
        arkitWeights[(int)ARKitBlendshape.CheekSquintRight] =
            W(OVRFaceExpressions.FaceExpression.CheekRaiserR);

        // ----- Eyes -----
        arkitWeights[(int)ARKitBlendshape.EyeBlinkLeft] =
            W(OVRFaceExpressions.FaceExpression.EyesClosedL);
        arkitWeights[(int)ARKitBlendshape.EyeBlinkRight] =
            W(OVRFaceExpressions.FaceExpression.EyesClosedR);

        arkitWeights[(int)ARKitBlendshape.EyeLookDownLeft] =
            W(OVRFaceExpressions.FaceExpression.EyesLookDownL);
        arkitWeights[(int)ARKitBlendshape.EyeLookDownRight] =
            W(OVRFaceExpressions.FaceExpression.EyesLookDownR);

        // In / Out：
        // these signal just causing no effect
        // i dont know why but just no, so thats why using ER signal

        // arkitWeights[(int)ARKitBlendshape.EyeLookInLeft] =
        //     W(OVRFaceExpressions.FaceExpression.EyesLookRightL);
        // arkitWeights[(int)ARKitBlendshape.EyeLookInRight] =
        //     W(OVRFaceExpressions.FaceExpression.EyesLookLeftR);

        // arkitWeights[(int)ARKitBlendshape.EyeLookOutLeft] =
        //     W(OVRFaceExpressions.FaceExpression.EyesLookLeftL);
        // arkitWeights[(int)ARKitBlendshape.EyeLookOutRight] =
        //     W(OVRFaceExpressions.FaceExpression.EyesLookRightR);

        // arkitWeights[(int)ARKitBlendshape.EyeLookUpLeft] =
        //     W(OVRFaceExpressions.FaceExpression.EyesLookUpL);
        // arkitWeights[(int)ARKitBlendshape.EyeLookUpRight] =
        //     W(OVRFaceExpressions.FaceExpression.EyesLookUpR);

        arkitWeights[(int)ARKitBlendshape.EyeLookDownLeft] = 0f;
        arkitWeights[(int)ARKitBlendshape.EyeLookDownRight] = 0f;
        arkitWeights[(int)ARKitBlendshape.EyeLookInLeft] = 0f;
        arkitWeights[(int)ARKitBlendshape.EyeLookInRight] = 0f;
        arkitWeights[(int)ARKitBlendshape.EyeLookOutLeft] = 0f;
        arkitWeights[(int)ARKitBlendshape.EyeLookOutRight] = 0f;
        arkitWeights[(int)ARKitBlendshape.EyeLookUpLeft] = 0f;
        arkitWeights[(int)ARKitBlendshape.EyeLookUpRight] = 0f;

        arkitWeights[(int)ARKitBlendshape.EyeSquintLeft] =
            W(OVRFaceExpressions.FaceExpression.LidTightenerL);
        arkitWeights[(int)ARKitBlendshape.EyeSquintRight] =
            W(OVRFaceExpressions.FaceExpression.LidTightenerR);

        arkitWeights[(int)ARKitBlendshape.EyeWideLeft] =
            W(OVRFaceExpressions.FaceExpression.UpperLidRaiserL);
        arkitWeights[(int)ARKitBlendshape.EyeWideRight] =
            W(OVRFaceExpressions.FaceExpression.UpperLidRaiserR);

        // ----- Jaw -----
        arkitWeights[(int)ARKitBlendshape.JawForward] =
            W(OVRFaceExpressions.FaceExpression.JawThrust);
        arkitWeights[(int)ARKitBlendshape.JawLeft] =
            W(OVRFaceExpressions.FaceExpression.JawSidewaysLeft);
        arkitWeights[(int)ARKitBlendshape.JawOpen] =
            W(OVRFaceExpressions.FaceExpression.JawDrop);
        arkitWeights[(int)ARKitBlendshape.JawRight] =
            W(OVRFaceExpressions.FaceExpression.JawSidewaysRight);

        // ----- Mouth -----
        float lipPressL = W(OVRFaceExpressions.FaceExpression.LipPressorL);
        float lipPressR = W(OVRFaceExpressions.FaceExpression.LipPressorR);
        float lipTightL = W(OVRFaceExpressions.FaceExpression.LipTightenerL);
        float lipTightR = W(OVRFaceExpressions.FaceExpression.LipTightenerR);

        arkitWeights[(int)ARKitBlendshape.MouthClose] = 0.0f;
            // 0.5f * (
            //     Mathf.Max(lipPressL, lipTightL) +
            //     Mathf.Max(lipPressR, lipTightR)
            // );
        
        // arkitWeights[(int)ARKitBlendshape.MouthPressLeft] = 0.0f;
        // arkitWeights[(int)ARKitBlendshape.MouthPressRight] = 0.0f;
        arkitWeights[(int)ARKitBlendshape.MouthPressLeft] = lipPressL;
        arkitWeights[(int)ARKitBlendshape.MouthPressRight] = lipPressR;

        arkitWeights[(int)ARKitBlendshape.MouthDimpleLeft] =
            W(OVRFaceExpressions.FaceExpression.DimplerL);
        arkitWeights[(int)ARKitBlendshape.MouthDimpleRight] =
            W(OVRFaceExpressions.FaceExpression.DimplerR);

        arkitWeights[(int)ARKitBlendshape.MouthFrownLeft] =
            W(OVRFaceExpressions.FaceExpression.LipCornerDepressorL);
        arkitWeights[(int)ARKitBlendshape.MouthFrownRight] =
            W(OVRFaceExpressions.FaceExpression.LipCornerDepressorR);

        arkitWeights[(int)ARKitBlendshape.MouthFunnel] =
            0.25f * (
                W(OVRFaceExpressions.FaceExpression.LipFunnelerLB) +
                W(OVRFaceExpressions.FaceExpression.LipFunnelerLT) +
                W(OVRFaceExpressions.FaceExpression.LipFunnelerRB) +
                W(OVRFaceExpressions.FaceExpression.LipFunnelerRT)
            );

        arkitWeights[(int)ARKitBlendshape.MouthLeft] =
            W(OVRFaceExpressions.FaceExpression.MouthLeft);

        arkitWeights[(int)ARKitBlendshape.MouthLowerDownLeft] =
            W(OVRFaceExpressions.FaceExpression.LowerLipDepressorL);
        arkitWeights[(int)ARKitBlendshape.MouthLowerDownRight] =
            W(OVRFaceExpressions.FaceExpression.LowerLipDepressorR);

        arkitWeights[(int)ARKitBlendshape.MouthPucker] =
            0.5f * (
                W(OVRFaceExpressions.FaceExpression.LipPuckerL) +
                W(OVRFaceExpressions.FaceExpression.LipPuckerR)
            );

        arkitWeights[(int)ARKitBlendshape.MouthRight] =
            W(OVRFaceExpressions.FaceExpression.MouthRight);

        float lipSuckLower =
            0.5f * (
                W(OVRFaceExpressions.FaceExpression.LipSuckLB) +
                W(OVRFaceExpressions.FaceExpression.LipSuckRB)
            );
        float lipSuckUpper =
            0.5f * (
                W(OVRFaceExpressions.FaceExpression.LipSuckLT) +
                W(OVRFaceExpressions.FaceExpression.LipSuckRT)
            );

        arkitWeights[(int)ARKitBlendshape.MouthRollLower] = lipSuckLower;
        arkitWeights[(int)ARKitBlendshape.MouthRollUpper] = lipSuckUpper;

        float lipsToward =
            W(OVRFaceExpressions.FaceExpression.LipsToward);
        float lowerDep =
            0.5f * (
                W(OVRFaceExpressions.FaceExpression.LowerLipDepressorL) +
                W(OVRFaceExpressions.FaceExpression.LowerLipDepressorR)
            );
        float upperRaise =
            0.5f * (
                W(OVRFaceExpressions.FaceExpression.UpperLipRaiserL) +
                W(OVRFaceExpressions.FaceExpression.UpperLipRaiserR)
            );

        arkitWeights[(int)ARKitBlendshape.MouthShrugLower] =
            0.5f * (lipsToward + lowerDep);
        arkitWeights[(int)ARKitBlendshape.MouthShrugUpper] =
            0.5f * (lipsToward + upperRaise);

        arkitWeights[(int)ARKitBlendshape.MouthSmileLeft] =
            W(OVRFaceExpressions.FaceExpression.LipCornerPullerL);
        arkitWeights[(int)ARKitBlendshape.MouthSmileRight] =
            W(OVRFaceExpressions.FaceExpression.LipCornerPullerR);

        arkitWeights[(int)ARKitBlendshape.MouthStretchLeft] =
            W(OVRFaceExpressions.FaceExpression.LipStretcherL);
        arkitWeights[(int)ARKitBlendshape.MouthStretchRight] =
            W(OVRFaceExpressions.FaceExpression.LipStretcherR);

        arkitWeights[(int)ARKitBlendshape.MouthUpperUpLeft] =
            W(OVRFaceExpressions.FaceExpression.UpperLipRaiserL);
        arkitWeights[(int)ARKitBlendshape.MouthUpperUpRight] =
            W(OVRFaceExpressions.FaceExpression.UpperLipRaiserR);

        arkitWeights[(int)ARKitBlendshape.NoseSneerLeft] =
            W(OVRFaceExpressions.FaceExpression.NoseWrinklerL);
        arkitWeights[(int)ARKitBlendshape.NoseSneerRight] =
            W(OVRFaceExpressions.FaceExpression.NoseWrinklerR);

        // arkitWeights[(int)ARKitBlendshape.TongueOut] =
        //     W(OVRFaceExpressions.FaceExpression.TongueOut);
    }

    private void SendARKitWeights()
    {
        if (udpClient == null || remoteEndPoint == null) return;

        const int expectedCount = 52; 
        var sb = new StringBuilder();
        sb.Append("{\"/W\":[");

        for (int i = 0; i < expectedCount; i++)
        {
            float v = (i < arkitWeights.Length) ? arkitWeights[i] : 0f;
            sb.Append(v.ToString("0.######", inv));
            if (i < expectedCount - 1)
                sb.Append(",");
        }

        sb.Append("],\"/ER\":[");

        for (int i = 0; i < 6; i++)
        {
            sb.Append(eyeRot[i].ToString("0.######", inv));
            if (i < 5)
                sb.Append(",");
        }

        sb.Append("],\"/HR\":[");

        for (int i = 0; i < 3; i++)
        {
            sb.Append(headRot[i].ToString("0.######", inv));
            if (i < 2)
                sb.Append(",");
        }

        sb.Append("]}");
        SendJson(sb.ToString());
    }

    [ContextMenu("Send Timeline Control Signal")]
    public void SendTimelineControlSignal()
    {
        SendControlSignal(defaultTimelineEventName, defaultTimelineClipName, "timeline");
    }

    public void SendControlSignal(string eventName)
    {
        SendControlSignal(eventName, defaultTimelineClipName, "manual");
    }

    public void SendControlSignal(string eventName, string clipName, string source)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            Debug.LogWarning("[ARKitReceiver] Control signal skipped because eventName is empty.");
            return;
        }

        var sb = new StringBuilder();
        sb.Append("{\"address\":\"/CTRL\",\"args\":{");
        sb.Append("\"event\":\"").Append(JsonEscape(eventName)).Append("\",");
        sb.Append("\"clip\":\"").Append(JsonEscape(clipName ?? string.Empty)).Append("\",");
        sb.Append("\"source\":\"").Append(JsonEscape(source ?? "manual")).Append("\",");
        sb.Append("\"sentAtMs\":").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        sb.Append("}}");

        SendJson(sb.ToString());
        Debug.Log(
            $"[ARKitReceiver] Sent control signal event='{eventName}' clip='{clipName}' source='{source}'"
        );
    }

    private void SendJson(string json)
    {
        if (udpClient == null || remoteEndPoint == null) return;

        byte[] data = Encoding.UTF8.GetBytes(json);

        try
        {
            udpClient.Send(data, data.Length, remoteEndPoint);
            // Debug.Log($"[ARKitReceiver] Sent: {json}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ARKitReceiver] UDP send failed: {e.Message}");
        }
    }

    private static string JsonEscape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private bool TryUpdateEyeRotation()
    {
    #if UNITY_ANDROID && !UNITY_EDITOR
        bool ok = OVRPlugin.GetEyeGazesState(OVRPlugin.Step.Render, -1, ref gazeState);
        if (!ok)
        {
            Debug.Log("[EyeTracking] GetEyeGazesState failed");
            return false;
        }

        var left  = gazeState.EyeGazes[(int)OVRPlugin.Eye.Left];
        var right = gazeState.EyeGazes[(int)OVRPlugin.Eye.Right];

        if (!left.IsValid || !right.IsValid)
        {
            Debug.Log("[EyeTracking] Eye gazes not valid yet");
            return false;
        }

        var qL = new Quaternion(
            left.Pose.Orientation.x,
            left.Pose.Orientation.y,
            left.Pose.Orientation.z,
            left.Pose.Orientation.w
        );

        var qR = new Quaternion(
            right.Pose.Orientation.x,
            right.Pose.Orientation.y,
            right.Pose.Orientation.z,
            right.Pose.Orientation.w
        );

        Vector3 eulerL = qL.eulerAngles * Mathf.Deg2Rad;
        Vector3 eulerR = qR.eulerAngles * Mathf.Deg2Rad;

        eyeRot[0] = eulerL.x;  // 左眼 X up and down
        eyeRot[1] = eulerL.y;  // 左眼 Y left and right
        eyeRot[2] = 0f;        // 左眼 Z (先 0) rotate clock wise or counter clock wise
        eyeRot[3] = eulerR.x;  // 右眼 X
        eyeRot[4] = eulerR.y;  // 右眼 Y
        eyeRot[5] = 0f;        // 右眼 Z (先 0)

        // Debug.Log(
        //     $"[EyeTracking] L(rad)=({eyeRot[0]:0.000}, {eyeRot[1]:0.000}, {eyeRot[2]:0.000})  " +
        //     $"R(rad)=({eyeRot[3]:0.000}, {eyeRot[4]:0.000}, {eyeRot[5]:0.000})"
        // );

        return true;
    #else
        for (int i = 0; i < 6; i++) eyeRot[i] = 0f;
        return false;
    #endif
    }

}
