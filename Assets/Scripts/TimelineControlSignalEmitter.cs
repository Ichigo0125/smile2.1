using UnityEngine;

public class TimelineControlSignalEmitter : MonoBehaviour
{
    [Header("Signal Target")]
    public ARKitReceiver receiver;

    [Header("Signal Payload")]
    public string eventName = "timeline_trigger";
    public string clipName = "default";
    public string source = "timeline";

    void Reset()
    {
        TryAutoAssignReceiver();
    }

    [ContextMenu("Emit Control Signal")]
    public void EmitControlSignal()
    {
        TryAutoAssignReceiver();

        if (receiver == null)
        {
            Debug.LogWarning("[TimelineControlSignalEmitter] Missing ARKitReceiver reference.");
            return;
        }

        receiver.SendControlSignal(eventName, clipName, source);
    }

    private void TryAutoAssignReceiver()
    {
        if (receiver == null)
        {
            receiver = GetComponent<ARKitReceiver>();
        }
    }
}
