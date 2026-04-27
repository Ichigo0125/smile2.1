#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PlayerCtr {
    [CustomEditor(typeof(transformLerper))]
    public class transformLerperInspector : Editor {
        // public Mesh defaltMesh;
        public GameObject assignedParnet;
        // public Material handleMat;
        transformLerper TLScript = null;
        int pickID = -1;
        private int copyID = -1;
        private bool showDiff = false;

        // For Preview Mesh 
        bool previewMesh = false;
        List<Matrix4x4> allTrs;

        // Vec3Popup vec3pop;
        Rect popRect;

        GUIStyle style_richBtn = new GUIStyle();
        GUIStyle style_richLabel = new GUIStyle();

        private MeshRenderer[] _targetMR;
        private MeshFilter[] _targetMF;

        // bool stackShowDebug = false;

        void OnEnable(){
            TLScript = (transformLerper) target;
            // vec3pop = new Vec3Popup();

            computeAllTrs();
            // MeshFilter targetMeshF = TLScript.findSharedMesh();
            // if( targetMeshF != null ){
            //     defaltMesh = targetMeshF.sharedMesh;
            // }
            // MeshRenderer targetMeshR = TLScript.findMeshRenderer();
            // if( targetMeshR != null ){
            //     handleMat = targetMeshR.sharedMaterial;
            // }

            _targetMR = TLScript.findMeshRenderers();
            _targetMF = TLScript.findSharedMeshes();

            // stackShowDebug = TLScript.showDebug;
            // TLScript.showDebug = false;

            TLScript.trackingFuncObjName();
        }

        private void OnDisable() {
            // TLScript.showDebug = stackShowDebug;
        }

        void computeAllTrs(){
            if( allTrs == null ){
                allTrs = new List<Matrix4x4>();
            }
            else{
                allTrs.Clear();
            }

            if( TLScript.steps != null ){
                for( int i=0 ; i<TLScript.steps.Count ; i++ ){
                    Matrix4x4 trs = Matrix4x4.TRS(
                        TLScript.steps[i].refObj.transform.position, 
                        TLScript.steps[i].refObj.transform.rotation, 
                        TLScript.steps[i].refObj.transform.lossyScale);

                    allTrs.Add(trs);
                }
            }
        }
        void updateTrs(int index){
            if( index >= allTrs.Count ){
                Debug.Log("<color=red>[Error] </color> Out of index Range.");
                return;
            }
            Matrix4x4 trs = Matrix4x4.TRS(
                        TLScript.steps[index].refObj.transform.position, 
                        TLScript.steps[index].refObj.transform.rotation, 
                        TLScript.steps[index].refObj.transform.lossyScale);
            allTrs[index] = trs;
        }

        public override void OnInspectorGUI(){
            // DrawDefaultInspector();

            serializedObject.Update();
            transformLerper TLScript = (transformLerper)target;

            // Rich Style Setting
            style_richBtn = GUI.skin.button;
            style_richBtn.richText = true;
            style_richLabel = GUI.skin.label;
            style_richLabel.richText = true;

            if( TLScript.steps == null || TLScript.steps.Count < 2 ){
                EditorGUILayout.LabelField("Initial Steps First", EditorStyles.boldLabel);
                if (GUILayout.Button("Add Steps")){
                    TLScript.addNewStep();
                    computeAllTrs();
                    EditorUtility.SetDirty(TLScript);
                }
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Parent Setting", EditorStyles.boldLabel);
                if( TLScript.gameObject.transform.parent != null ){ // Not On the Top
                    if (GUILayout.Button("Moving Out", GUILayout.Width(100))){
                        TLScript.shiftAlltoParent(TLScript.gameObject.transform.parent.parent);
                        EditorUtility.SetDirty(TLScript);
                    }
                }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("<color=cyan>New Parent</color>", style_richBtn, GUILayout.Width(100))){
                    GameObject _tmpStep = new GameObject(TLScript.gameObject.name+"Root");
                    if( TLScript.gameObject.transform.parent != null ){
                        _tmpStep.transform.parent = TLScript.gameObject.transform.parent;
                        _tmpStep.name = TLScript.gameObject.transform.parent.name+"Root";
                    }
                    TLScript.shiftAlltoParent(_tmpStep.transform);
                    EditorUtility.SetDirty(TLScript);
                }
                EditorGUILayout.LabelField(" Add New Parent to <b>"+TLScript.gameObject.name+"</b>", style_richLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup((assignedParnet != null) ? false : true);
                    if (GUILayout.Button("<color=cyan>Assign Parent</color>", style_richBtn, GUILayout.Width(100))){
                        TLScript.shiftAlltoParent(assignedParnet.transform);
                        EditorUtility.SetDirty(TLScript);
                    }
                EditorGUI.EndDisabledGroup();
                assignedParnet = (GameObject) EditorGUILayout.ObjectField(assignedParnet, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Initial Setting", EditorStyles.boldLabel);
            EditorGUI.indentLevel += 1;
            var enableInit = serializedObject.FindProperty("startWithInit");
            EditorGUILayout.PropertyField(enableInit, new GUIContent("Start Initial", "Start with Initialization"), true);
            if( enableInit.boolValue ){
                string[] _stepsList = new string[TLScript.steps.Count];
                // float lineLength = 0;
                for( int i=0 ; i<_stepsList.Length ; i++ ){
                    // lineLength = Vector3.Distance(TLScript.steps[i].refObj.transform.position, TLScript.steps[i+1].refObj.transform.position);
                    // _stepsList[i] = "["+(i+1).ToString() + "] Length = " + lineLength.ToString("F2");
                    _stepsList[i] = "TL: "+(i).ToString();
                }
                int tempSelect = EditorGUILayout.Popup("Init. Segment", Mathf.Max(0, TLScript.iniInterval - 1 + (int)TLScript.iniTimeWeight), _stepsList) + 1;
                if( tempSelect != (TLScript.iniInterval + (int)TLScript.iniTimeWeight) ){
                    TLScript.iniInterval = tempSelect;
                    if( tempSelect == _stepsList.Length ){
                        TLScript.iniInterval -= 1;
                        TLScript.iniTimeWeight = 1;
                    }else{
                        TLScript.iniTimeWeight = 0;
                    }
                    EditorUtility.SetDirty(target);
                }
            }
            EditorGUI.indentLevel -= 1;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Control", EditorStyles.boldLabel);
            var targetObj = serializedObject.FindProperty("controlTarget");
            EditorGUI.indentLevel += 1;
            EditorGUILayout.PropertyField(targetObj, new GUIContent("Control Target", "if null, it would use SELF in run-time."), true);
            var staticSpeed = serializedObject.FindProperty("staticSpeed");
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(staticSpeed, new GUIContent("Static Speed", "Static Speed Through Whole Line"), true);
                if( staticSpeed.boolValue ){
                    if (GUILayout.Button("SampleBeizerPoint")) {
                        TLScript.SampleBeizerPoints();
                    }
                }
            EditorGUILayout.EndHorizontal();
            if( staticSpeed.boolValue ){
                EditorGUI.indentLevel += 1;
                var sampleCount = serializedObject.FindProperty("sampleCount");
                EditorGUILayout.PropertyField(sampleCount, new GUIContent("Sample Count", "Samples for Curve Point"), true);
                var segLength = serializedObject.FindProperty("segLength");
                EditorGUILayout.PropertyField(segLength, new GUIContent("Segment Length", "Segmantation Length"), true);
                EditorGUI.indentLevel -= 1;
            }

            var autoRotation = serializedObject.FindProperty("autoRotation");
            EditorGUILayout.PropertyField(autoRotation, new GUIContent("Auto Rotation", "Enable Auto Rotation"), true);
            if( autoRotation.boolValue ){
                EditorGUI.indentLevel += 1;
                var bzRotSmooth = serializedObject.FindProperty("bzRotSmooth");
                EditorGUILayout.PropertyField(bzRotSmooth, new GUIContent("Power", "Rotation Smooth Interpolation Speed"), true);
                EditorGUI.indentLevel -= 1;
            }
            EditorGUI.indentLevel -= 1;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            var showDebug = serializedObject.FindProperty("showDebug");
            EditorGUI.indentLevel += 1;
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(showDebug, new GUIContent("Show Debug Line", "The Debug line would be RED line in the Scene"), true);
                if (GUILayout.Button("Open/Close Hide Obj")) {
                    TLScript.toggleStepHideObj();
                    EditorApplication.RepaintHierarchyWindow();
                    try { EditorApplication.DirtyHierarchyWindowSorting(); } catch { }
                }
            EditorGUILayout.EndHorizontal();
            if( showDebug.boolValue ){
                EditorGUILayout.BeginHorizontal();
                var lineSeg = serializedObject.FindProperty("lineSeg");
                EditorGUILayout.PropertyField(lineSeg, new GUIContent("Total Segment", "If Higher, it will have performace issue in Editor Mode."), true);
                var debug_normalH = serializedObject.FindProperty("debug_normalH");
                EditorGUILayout.PropertyField(debug_normalH, new GUIContent("Normal Height", "Height for Noraml Line"), true);
                EditorGUILayout.EndHorizontal();
                // // ---- Temp Show Array
                // var steps = serializedObject.FindProperty("steps");
                // EditorGUILayout.PropertyField(steps, new GUIContent("Steps List", "Steps List"), true);
            }
            EditorGUI.indentLevel -= 1;

            EditorGUILayout.Space();

            string tempName = (pickID == -1) ? "None" : pickID.ToString();
            EditorGUILayout.BeginHorizontal("Box");
            EditorGUILayout.LabelField("Select ID: "+tempName, EditorStyles.boldLabel);
            if( pickID != -1 ){
                if( pickID != copyID ){
                    if (GUILayout.Button("<color=blue>Copy</color>", style_richBtn, GUILayout.Width(50))){
                        copyID = pickID;
                    }
                }

                if (GUILayout.Button("<", GUILayout.Width(25))){
                    pickID = Mathf.Max(0, pickID-1);
                    return;
                }
                if (GUILayout.Button(">", GUILayout.Width(25))){
                    pickID = Mathf.Min(TLScript.steps.Count-1, pickID+1);
                    return;
                }
            } else{
                if (GUILayout.Button("Pickup TL:0", GUILayout.Width(100))){
                    pickID = 0;
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();

            if( pickID != -1 ){
                EditorGUILayout.BeginVertical("helpBox");
                EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Handle Transform:");
                    if( copyID != pickID && copyID != -1 ){
                        if (GUILayout.Button("<color=blue>Paste</color>", style_richBtn, GUILayout.Width(50))){
                            Undo.RecordObject(TLScript.steps[pickID].refObj.transform, "Copy TL: "+copyID.ToString()+" to TL:"+pickID.ToString());
                            TLScript.steps[pickID].refObj.transform.position    = TLScript.steps[copyID].refObj.transform.position;
                            TLScript.steps[pickID].refObj.transform.rotation    = TLScript.steps[copyID].refObj.transform.rotation;
                            TLScript.steps[pickID].refObj.transform.localScale  = TLScript.steps[copyID].refObj.transform.localScale;
                            EditorUtility.SetDirty(TLScript);
                        }
                    }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel += 1;
                CreateEditor(TLScript.steps[pickID].refObj.transform).OnInspectorGUI();
                EditorGUI.indentLevel -= 1;
                if( copyID != -1 ){
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Copied [TL:"+copyID+"]");
                        if( showDiff ){
                            if (GUILayout.Button("Ori.", GUILayout.Width(50))){
                                showDiff = false;
                            }
                        }else{
                            if (GUILayout.Button("Diff.", GUILayout.Width(50))){
                                showDiff = true;
                            }
                        }
                        
                    EditorGUILayout.EndHorizontal();
                    if( copyID != pickID ){
                        EditorGUI.indentLevel += 1;
                        EditorGUI.BeginDisabledGroup(true);
                        if( showDiff ){
                            EditorGUILayout.Vector3Field("Position", TLScript.steps[pickID].refObj.transform.localPosition - TLScript.steps[copyID].refObj.transform.localPosition);
                            EditorGUILayout.Vector3Field("Rotation", TLScript.steps[pickID].refObj.transform.localEulerAngles - TLScript.steps[copyID].refObj.transform.localEulerAngles);
                            EditorGUILayout.Vector3Field("Scale", TLScript.steps[pickID].refObj.transform.localScale - TLScript.steps[copyID].refObj.transform.localScale);
                        }else{
                            CreateEditor(TLScript.steps[copyID].refObj.transform).OnInspectorGUI();
                        }
                        EditorGUI.EndDisabledGroup();
                        EditorGUI.indentLevel -= 1;
                    }
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("helpBox");
                TLScript.steps[pickID].isBzPoint = EditorGUILayout.Toggle("Is Curve", TLScript.steps[pickID].isBzPoint);
                if( TLScript.steps[pickID].isBzPoint ){
                    EditorGUI.indentLevel += 1;
                    TLScript.steps[pickID].controlPt = EditorGUILayout.Vector3Field("Control Point", TLScript.steps[pickID].controlPt);
                    EditorGUI.indentLevel -= 1;
                }
                EditorGUILayout.EndVertical();

                if( TLScript.autoRotation ){
                    EditorGUILayout.BeginVertical("helpBox");
                    TLScript.steps[pickID].reverse = EditorGUILayout.Toggle("Reverse Forward Vec", TLScript.steps[pickID].reverse);
                    EditorGUILayout.EndVertical();
                }
            }

            if(GUI.changed)
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI() {
            // transformLerper example = (transformLerper)target;

            // float size = HandleUtility.GetHandleSize(example.targetPosition) * 0.5f;
            // Vector3 snap = Vector3.one * 0.5f;

            // EditorGUI.BeginChangeCheck();
            // Vector3 newTargetPosition = Handles.FreeMoveHandle(example.targetPosition, Quaternion.identity, size, snap, Handles.RectangleHandleCap);
            // if (EditorGUI.EndChangeCheck())
            // {
            //     Undo.RecordObject(example, "Change Look At Target Position");
            //     example.targetPosition = newTargetPosition;
            //     example.Update();
            // }

            // On Scene GUI
            Handles.BeginGUI();
            // ========== Handles
            if( TLScript.steps != null ){
                if( previewMesh ){
                    // This Will Repaint Issue, so just not use this time.
                    // Graphics.DrawMeshInstanced(defaltMesh, 0, handleMat, allTrs);

                    // handleMat.SetPass(0);
                    // for( int i=0 ; i<allTrs.Count ; i++ ){
                    //     Graphics.DrawMeshNow(defaltMesh, allTrs[i]);
                    // }
                    for( int i=0 ; i<TLScript.steps.Count ; i++ ){
                        for( int j=0 ; j<_targetMR.Length ; j++ ){
                            _targetMR[j].sharedMaterial.SetPass(0);
                            // Graphics.DrawMeshNow(_targetMF[j].sharedMesh, TLScript.steps[i].refObj.transform.localToWorldMatrix);
                            Graphics.DrawMeshNow(_targetMF[j].sharedMesh, allTrs[i]);
                        }
                    }
                }

                float size3D = 0.5f * TLScript.handleSizeScale;

                Matrix4x4 rootMatrix = Matrix4x4.identity;
                if( TLScript.gameObject.transform.parent != null ){
                    rootMatrix = TLScript.gameObject.transform.parent.localToWorldMatrix;
                }

                for( int i=0 ; i<TLScript.steps.Count ; i++ ){
                    // Handles.Label(TLScript.steps[i].localPosition + Vector3.up * size3D, "TL: "+i.ToString());
                    Handles.Label(TLScript.steps[i].refObj.transform.position + Vector3.up * size3D, "TL: "+i.ToString());

                    if (Handles.Button(
                            TLScript.steps[i].refObj.transform.position, 
                            TLScript.steps[i].refObj.transform.rotation, 
                            size3D, size3D * 1.5f, Handles.CubeHandleCap
                            )){
                        if( pickID == i ){
                            pickID = -1;
                        }else{
                            pickID = i;
                        }
                        Repaint();
                    }

                    if( pickID == i ){
                        Vector3 posBuffer = Vector3.zero;
                        Quaternion rotBuffer = Quaternion.identity;
                        Vector3 lscale  = TLScript.steps[i].localScale;
                        Vector3 ctrPos  = TLScript.steps[i].controlPt;

                        // // Popup Control
                        // Handles.color = Color.cyan;
                        // if (Handles.Button(TLScript.steps[i].refObj.transform.position+ (Vector3.up + Vector3.right) * size3D/2.0f, 
                        //                     TLScript.steps[i].refObj.transform.rotation, size3D/5.0f, size3D, Handles.DotHandleCap)){
                        //     popRect.x = Event.current.mousePosition.x;
                        //     popRect.y = Event.current.mousePosition.y;

                        //     vec3pop.setID(pickID);
                        //     vec3pop.setInfo(TLScript.steps[i]);
                        //     PopupWindow.Show(popRect, vec3pop);
                        // }
                        // Handles.color = Color.white;

                        // [1] Edit Step Transform First
                        EditorGUI.BeginChangeCheck();
                        if( Tools.current == Tool.Move ){
                            posBuffer = Handles.PositionHandle(
                                        TLScript.steps[i].refObj.transform.position, 
                                        TLScript.steps[i].refObj.transform.rotation);
                            // TLScript.steps[i].refObj.transform.position = Handles.PositionHandle(
                            //                                                     TLScript.steps[i].refObj.transform.position, 
                            //                                                     TLScript.steps[i].refObj.transform.rotation);
                        }
                        else if( Tools.current == Tool.Rotate ){
                            rotBuffer = Handles.RotationHandle(
                                    TLScript.steps[i].refObj.transform.rotation, 
                                    TLScript.steps[i].refObj.transform.position);
                            // TLScript.steps[i].refObj.transform.rotation = Handles.RotationHandle(
                            //                                                     TLScript.steps[i].refObj.transform.rotation, 
                            //                                                     TLScript.steps[i].refObj.transform.position);
                        }
                        else if( Tools.current == Tool.Scale ){
                            lscale = Handles.ScaleHandle(lscale, 
                                        TLScript.steps[i].refObj.transform.position, 
                                        TLScript.steps[i].refObj.transform.rotation, 
                                        1.0f);
                        }
                        
                        if (EditorGUI.EndChangeCheck()){
                            if( Tools.current == Tool.Move ){
                                // Undo.RegisterCompleteObjectUndo(TLScript.steps[i].refObj, "Move TL: "+i.ToString());
                                // // Compute CtrPoint First
                                // if( TLScript.steps[i].isBzPoint ){
                                //     ctrPos = pos + TLScript.steps[i].controlPt - TLScript.steps[i].position;
                                //     TLScript.steps[i].controlPt = ctrPos;
                                // }
                                Undo.RecordObject(TLScript.steps[i].refObj.transform, "Move TL: "+i.ToString());
                                TLScript.steps[i].refObj.transform.position = posBuffer;
                            }
                            else if( Tools.current == Tool.Rotate ){
                                Undo.RecordObject(TLScript.steps[i].refObj.transform, "Rotate TL: "+i.ToString());
                                TLScript.steps[i].refObj.transform.rotation = rotBuffer;
                            }
                            else if( Tools.current == Tool.Scale ){
                                Undo.RecordObject(TLScript.steps[i].refObj.transform, "Scale TL: "+i.ToString());
                                TLScript.steps[i].refObj.transform.localScale = lscale;
                                // TLScript.steps[i].localScale = lscale;
                            }
                            updateTrs(i);

                            EditorUtility.SetDirty(TLScript);
                        }

                        // // [2] Bz Ctr Check
                        EditorGUI.BeginChangeCheck();
                        if( TLScript.steps[i].isBzPoint && Tools.current == Tool.Move ){
                            Handles.DrawLine(TLScript.steps[i].refObj.transform.position, ctrPos + TLScript.steps[i].refObj.transform.position);
                            Handles.DrawLine(TLScript.steps[i].refObj.transform.position, -ctrPos + TLScript.steps[i].refObj.transform.position);
                            Handles.DotHandleCap(i*10+1, ctrPos + TLScript.steps[i].refObj.transform.position, TLScript.steps[i].refObj.transform.rotation, size3D/4.0f, EventType.Repaint);
                            Handles.DotHandleCap(i*10+2, -ctrPos + TLScript.steps[i].refObj.transform.position, TLScript.steps[i].refObj.transform.rotation, size3D/4.0f, EventType.Repaint);

                            ctrPos = Handles.PositionHandle(ctrPos + TLScript.steps[i].refObj.transform.position, TLScript.steps[i].refObj.transform.rotation);
                        }
                        if (EditorGUI.EndChangeCheck()){
                            // Undo.RecordObject(TLScript, "BzCtrPos TL: "+i.ToString());
                            Undo.RegisterCompleteObjectUndo(TLScript, "BzCtrPos TL: "+i.ToString());
                            TLScript.steps[i].controlPt = ctrPos - TLScript.steps[i].refObj.transform.position;
                            updateTrs(i);

                            EditorUtility.SetDirty(TLScript);
                        }

                    }

                    GUIStyle lineLengthStyle = new GUIStyle() { normal = new GUIStyleState() { textColor = Color.blue } };
                    
                    // Draw Line Part
                    if( i > 0 ){
                        // Will be the Same to OnDrawGizmos in transformLerper.cs
                        // Handles.DrawDottedLine(TLScript.steps[i-1].position, TLScript.steps[i].position, dashSize);
                        Vector3 pos1 = TLScript.steps[i-1].localPosition;
                        Vector3 pos1_ctr = ((TLScript.steps[i-1].isBzPoint) ? -TLScript.steps[i-1].controlPt : Vector3.zero) + TLScript.steps[i-1].localPosition;
                        Vector3 pos2 = TLScript.steps[i].localPosition;
                        Vector3 pos2_ctr = ((TLScript.steps[i].isBzPoint) ? TLScript.steps[i].controlPt : Vector3.zero) + TLScript.steps[i].localPosition;
                        
                        var lineLength = 0f;
                        var lineLengthPosition = Vector3.zero;

                        // get world position
                        var wpos1 = rootMatrix.MultiplyPoint3x4(pos1);
                        var wpos2 = rootMatrix.MultiplyPoint3x4(pos2);
                        if ( TLScript.steps[i-1].isBzPoint || TLScript.steps[i].isBzPoint ){
                            var wpos1_ctr = rootMatrix.MultiplyPoint3x4(pos1_ctr);
                            var wpos2_ctr = rootMatrix.MultiplyPoint3x4(pos2_ctr);

                            Handles.DrawBezier(wpos1, wpos2, 
                                            wpos1_ctr, wpos2_ctr, 
                                            Color.white, null, 1.0f);
                            var lineSeg = 2000;
                            var points = Handles.MakeBezierPoints(wpos1, wpos2, wpos1_ctr, wpos2_ctr, lineSeg);
                            var p0 = points[0];
                            
                            foreach (var p in points) {
                                lineLength += Vector3.Distance(p0, p);
                                p0 = p;
                            }
                            lineLengthPosition = points[lineSeg / 2];
                        }else{
                            Handles.DrawLine(wpos1, wpos2);
                            lineLength = Vector3.Distance(wpos1, wpos2);
                            lineLengthPosition = Vector3.Lerp(wpos1, wpos2, 0.5f);
                        }

                        Handles.Label(lineLengthPosition, lineLength.ToString("f2"), lineLengthStyle);
                    }
                }
                
                // Repaint Issue
                // if( repaintFlag ){
                //     // EditorWindow view = EditorWindow.GetWindow<SceneView>();
                //     // view.Repaint();
                //     HandleUtility.Repaint();
                // }
                // if (Event.current.type == EventType.Repaint) popRect = GUILayoutUtility.GetLastRect();
            }
            // End Transform Handles
            
            // ========== On Scene Panel
            float tmpWidth = 50;
            float perStepPanelH = (pickID == -1) ? 0 : 175;
            GUILayout.BeginArea(new Rect(10, 10, 230, 125+perStepPanelH));
            
            var rect = EditorGUILayout.BeginVertical();
            GUI.color = Color.gray*1.5f;
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;
            
            GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("[TL] "+TLScript.gameObject.name);
                GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
                if (GUILayout.Button("Preview: "+previewMesh.ToString())) {
                    // // Temp Disable This
                    // previewMesh = !previewMesh;

                    if( previewMesh ){
                        computeAllTrs();
                    }
                }
                // if (GUILayout.Button("Debug: "+stackShowDebug.ToString())) {
                //     stackShowDebug = !stackShowDebug;
                // }
                if (GUILayout.Button("Debug: "+TLScript.showDebug.ToString())) {
                    TLScript.showDebug = !TLScript.showDebug;
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("dObj")) {
                    TLScript.toggleStepHideObj();
                    EditorApplication.RepaintHierarchyWindow();
                    try { EditorApplication.DirtyHierarchyWindowSorting(); } catch { }
                }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("Add Step")) {
                    // Undo.RecordObject(TLScript, "Add New Step");
                    Undo.RegisterCompleteObjectUndo(TLScript, "Add New Step");
                    TLScript.addNewStep();
                    computeAllTrs();
                    EditorUtility.SetDirty(TLScript);
                }
                GUI.backgroundColor = Color.red;
                // if (GUILayout.Button("Remove Last One")) {
                //     // Undo.RecordObject(TLScript, "Remove Last");
                //     TLScript.removeStep();
                //     computeAllTrs();
                //     EditorUtility.SetDirty(TLScript);
                // }
                if (GUILayout.Button("Remove All", GUILayout.Width(75))) {
                    // Undo.RecordObject(TLScript, "Remove All");
                    TLScript.removeAllStep();
                    EditorUtility.SetDirty(TLScript);
                }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUI.backgroundColor = Color.white;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Handle", GUILayout.Width(50));
            TLScript.handleSizeScale = GUILayout.HorizontalSlider(TLScript.handleSizeScale, 0.01f, 1);
            GUILayout.Label(TLScript.handleSizeScale.ToString("F2"), GUILayout.Width(30));
            GUILayout.EndHorizontal();

            // ============================== ??? TEST ??? ============================== //
            // GUILayout.BeginHorizontal();
            // GUI.backgroundColor = Color.white;
            // if (GUILayout.Button("Follow Root localScale")) {
            //         // Undo.RecordObject(TLScript, "followRootObjlScale");
            //         TLScript.followRootObjlScale();
            //         EditorUtility.SetDirty(TLScript);
            //     }
            // GUILayout.EndHorizontal();
            // ============================== ??? ---- ??? ============================== //

            if( pickID != -1 ){
                GUILayout.Space(5);
                // ---------------- Insert/Delete Method
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("Insert Prev.")){
                    Undo.RegisterCompleteObjectUndo(TLScript, "Insert Prev.");
                    TLScript.insertStep(pickID);
                    computeAllTrs();
                    EditorUtility.SetDirty(TLScript);
                }
                if (GUILayout.Button("Insert Next.")){
                    if( pickID + 1 >= TLScript.steps.Count ){
                        Undo.RegisterCompleteObjectUndo(TLScript, "Add New Step");
                        TLScript.addNewStep();
                    }else{
                        Undo.RegisterCompleteObjectUndo(TLScript, "Insert Next.");
                        TLScript.insertStep(pickID+1);
                    }
                    computeAllTrs();
                    EditorUtility.SetDirty(TLScript);
                }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Delete", GUILayout.Width(50))) {
                    // --- Remove Undo Still Keep Error
                    // Undo.RecordObject(TLScript, "Remove Last Step");
                    TLScript.removeStep(pickID);
                    computeAllTrs();
                    pickID = -1;
                    EditorUtility.SetDirty(TLScript);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                // ---------------- Aligment Method
                GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Aligment Method");
                    GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;
                
                if( pickID == 0 ){
                    if (GUILayout.Button("Align to Object")) {
                        // Undo.RecordObject(TLScript, "Align to Object");
                        Undo.RecordObject(TLScript.steps[pickID].refObj.transform, "Align to Object");
                        TLScript.steps[pickID].refObj.transform.position = TLScript.transform.position;
                        computeAllTrs();
                        EditorUtility.SetDirty(TLScript);
                    }
                    if (GUILayout.Button("Align All to Object")) {
                        // Undo.RecordObject(TLScript, "Align to Object");
                        Vector3 posGap = TLScript.transform.position - TLScript.steps[pickID].refObj.transform.position;
                        for( int i=0 ; i<TLScript.steps.Count ; i++){
                            TLScript.steps[i].refObj.transform.position += posGap;
                        }
                        computeAllTrs();
                        EditorUtility.SetDirty(TLScript);
                    }
                }else{
                    Vector3 alignVec = Vector3.zero;
                    bool alignChange = false;
                    tmpWidth = 75;
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Forward", GUILayout.Width(tmpWidth))) {
                        alignVec = TLScript.steps[pickID-1].refObj.transform.forward;
                        alignChange = true;
                    }
                    if (GUILayout.Button("Up")) {
                        alignVec = TLScript.steps[pickID-1].refObj.transform.up;
                        alignChange = true;
                    }
                    if (GUILayout.Button("Right", GUILayout.Width(tmpWidth))) {
                        alignVec = TLScript.steps[pickID-1].refObj.transform.right;
                        alignChange = true;
                    }
                    GUILayout.EndHorizontal();
                    GUI.backgroundColor = Color.white * 0.85f;
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Backward", GUILayout.Width(tmpWidth))) {
                        alignVec = -TLScript.steps[pickID-1].refObj.transform.forward;
                        alignChange = true;
                    }
                    if (GUILayout.Button("Down")) {
                        alignVec = -TLScript.steps[pickID-1].refObj.transform.up;
                        alignChange = true;
                    }
                    if (GUILayout.Button("Left", GUILayout.Width(tmpWidth))) {
                        alignVec = -TLScript.steps[pickID-1].refObj.transform.right;
                        alignChange = true;
                    }
                    GUILayout.EndHorizontal();
                    
                    GUILayout.Space(2);
                    GUILayout.BeginHorizontal();
                    tmpWidth = 50;
                    GUI.backgroundColor = Color.white;
                    GUILayout.Label("Fit:", GUILayout.Width(tmpWidth));
                    if (GUILayout.Button("Prev", GUILayout.Width(tmpWidth))){
                        alignVec = Vector3.zero;
                        alignChange = true;
                    }
                    if( pickID+1 != TLScript.steps.Count ){ // && pickID-1 != 0
                        if (GUILayout.Button("Center")) {
                            alignVec = (TLScript.steps[pickID+1].refObj.transform.position - TLScript.steps[pickID-1].refObj.transform.position)*0.5f;
                            alignChange = true;
                        }
                        if (GUILayout.Button("Next", GUILayout.Width(tmpWidth))) {
                            alignVec = (TLScript.steps[pickID+1].refObj.transform.position - TLScript.steps[pickID-1].refObj.transform.position);
                            alignChange = true;
                        }
                    }else{
                        GUILayout.Label("Center");
                        GUILayout.Label("Next", GUILayout.Width(tmpWidth));
                    }
                    
                    GUILayout.EndHorizontal();

                    if( alignChange == true ){
                        // Undo.RecordObject(TLScript, "Align Method");
                        Undo.RecordObject(TLScript.steps[pickID].refObj.transform, "Align Steps: "+pickID.ToString());
                        TLScript.steps[pickID].refObj.transform.position = TLScript.steps[pickID-1].refObj.transform.position + alignVec;
                        computeAllTrs();
                        EditorUtility.SetDirty(TLScript);
                    }
                }

                GUILayout.Space(5);
                // ---------------- Curve Control Method
                GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Curve Control");
                    GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUI.backgroundColor = Color.cyan;
                GUILayout.BeginHorizontal();
                string bzStr = (TLScript.steps[pickID].isBzPoint) ? "Rmv Curve" : "Set Curve";
                if (GUILayout.Button(bzStr)) {
                    // Undo.RecordObject(TLScript, "Add New Step");
                    Undo.RegisterCompleteObjectUndo(TLScript, "Set BzCurve");
                    TLScript.steps[pickID].isBzPoint = !TLScript.steps[pickID].isBzPoint;
                    EditorUtility.SetDirty(TLScript);
                }
                if( TLScript.steps[pickID].isBzPoint ){
                    GUI.backgroundColor = Color.white;
                    if (GUILayout.Button("Inverse", GUILayout.Width(75))) {
                        Undo.RegisterCompleteObjectUndo(TLScript, "BzCtrPos Inverse: "+pickID.ToString());
                        TLScript.steps[pickID].controlPt = -TLScript.steps[pickID].controlPt;
                    }
                }
                GUILayout.EndHorizontal();

                GUI.backgroundColor = Color.white;
                if( TLScript.steps[pickID].isBzPoint ){
                    Vector3 alignVec = Vector3.zero;
                    bool alignChange = false;
                    tmpWidth = 75;
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Forward", GUILayout.Width(tmpWidth))) {
                        alignVec = TLScript.steps[pickID].refObj.transform.forward;
                        alignChange = true;
                    }
                    if (GUILayout.Button("Up")) {
                        alignVec = TLScript.steps[pickID].refObj.transform.up;
                        alignChange = true;
                    }
                    if (GUILayout.Button("Right", GUILayout.Width(tmpWidth))) {
                        alignVec = TLScript.steps[pickID].refObj.transform.right;
                        alignChange = true;
                    }
                    EditorGUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Center Prev")) {
                        if( pickID == 0 )
                            alignVec = (TLScript.steps[pickID+1].refObj.transform.position - TLScript.steps[pickID].refObj.transform.position) * 0.5f;
                        else
                            alignVec = (TLScript.steps[pickID-1].refObj.transform.position - TLScript.steps[pickID].refObj.transform.position) * 0.5f;
                        alignChange = true;
                    }
                    
                    if (GUILayout.Button("Center Next")) {
                        if( pickID + 1 == TLScript.steps.Count )
                            alignVec = -(TLScript.steps[pickID-1].refObj.transform.position - TLScript.steps[pickID].refObj.transform.position) * 0.5f;   
                        else
                            alignVec = -(TLScript.steps[pickID+1].refObj.transform.position - TLScript.steps[pickID].refObj.transform.position) * 0.5f;
                        alignChange = true;
                    }
                    EditorGUILayout.EndHorizontal();

                    if( alignChange ){
                        Undo.RegisterCompleteObjectUndo(TLScript, "BzCtrPos Manipulation:"+pickID.ToString());
                        TLScript.steps[pickID].controlPt = alignVec;
                    }
                }
            }
            EditorGUILayout.EndVertical();
            
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        // public void MeshHandleCap(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType)
        // {
        //     if (eventType == EventType.Repaint)
        //     {
        //         handleMat.SetPass(0);
        //         Graphics.DrawMeshNow(defaltMesh, position, Quaternion.identity);
        //     }
        //     else if (eventType == EventType.Layout)
        //     {
        //         // Ray mouseRay = HandleUtility.GUIPointToWorldRay(e.mousePosition); 
        //         Vector3 mousePos = Event.current.mousePosition;
        //         mousePos.y = SceneView.lastActiveSceneView.camera.pixelHeight - mousePos.y;
        //         Ray mouseRay = SceneView.lastActiveSceneView.camera.ScreenPointToRay(mousePos);
        //         bool intersect = defaltMesh.bounds.IntersectRay(mouseRay);
        //         if (intersect)
        //             HandleUtility.AddControl(controlId, 0);
        //         else
        //             HandleUtility.AddControl(controlId, 1e20f);
        //     }
        // }
    }

    public class Vec3Popup : PopupWindowContent
    {
        int usedID = -1;
        // Vector3 savedVec = Vector3.zero;
        transformInfo stepInfo;
        // Transform transformBuffer;

        public void setID(int id){
            usedID = id;
        }

        public void setInfo(transformInfo getInfo){
            // class call by ref
            stepInfo = getInfo;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(250, 25*5);
        }

        public override void OnGUI(Rect rect)
        {
            // GUILayout.Label("Popup Options Example", EditorStyles.boldLabel);
            if( stepInfo == null ){
                GUI.color = Color.red;
                GUILayout.Label("[Popup Error] Please Assign transformInfo", EditorStyles.boldLabel);
            }else{
                // transformBuffer = EditorGUILayout.ObjectField("root", stepInfo.refqObj.transform, typeof(Transform), true) as Transform;

                stepInfo.refObj.transform.localPosition = EditorGUILayout.Vector3Field("lPosition", stepInfo.localPosition);
                // stepInfo.refObj.transform.localEulerAngles = EditorGUILayout.Vector3Field("Rotation", UnityEditor.TransformUtils.GetInspectorRotation(stepInfo.refObj.transform));
                stepInfo.refObj.transform.localEulerAngles = EditorGUILayout.Vector3Field("lRotation", stepInfo.refObj.transform.localEulerAngles);
                stepInfo.refObj.transform.localScale = EditorGUILayout.Vector3Field("lScale", stepInfo.localScale);

            }
        }

        public override void OnOpen()
        {
            if( stepInfo == null ){
                Debug.Log("<color=red>[Popup Error]</color> Please Assign transformInfo here.");
            }
        }

        public override void OnClose()
        {
            // Debug.Log("Popup closed: " + this);
        }
    }

    // Attribute Test
	// [CustomPropertyDrawer(typeof(TLStepMenuAttribute))]
	// public class TLStepMenuDrawer : PropertyDrawer {

	// 	private const string SCENE_EXTENSION = ".unity";
	// 	private const string NOSCENE_TIP = "Scene is Empty";

	// 	public override void OnGUI (UnityEngine.Rect position, SerializedProperty property, UnityEngine.GUIContent label)
	// 	{
	// 		string sceneFile;
	// 		TLStepMenuAttribute attribute = (TLStepMenuAttribute)base.attribute;
	// 		List<string> sceneNames = new List<string>();

	// 		for(int i = 0 ; i < EditorBuildSettings.scenes.Length ; i++){

	// 			if(EditorBuildSettings.scenes[i].enabled){

	// 				sceneFile = EditorBuildSettings.scenes[i].path.Substring(EditorBuildSettings.scenes[i].path.LastIndexOf("/") + 1);
	// 				sceneNames.Add(sceneFile.Replace(SCENE_EXTENSION, string.Empty));
	// 			}
	// 		}

	// 		if(sceneNames.Count == 0){

	// 			EditorGUI.LabelField(position, label.text, NOSCENE_TIP);

	// 		}else{

	// 			for(int i = 0 ; i < sceneNames.Count ; i++){

	// 				if(sceneNames[i] == property.stringValue){
	// 					attribute.selected = i;
	// 					break;
	// 				}
	// 			}

	// 			attribute.selected = EditorGUI.Popup(position , label.text , attribute.selected , sceneNames.ToArray());
	// 			property.stringValue = sceneNames[attribute.selected];
	// 		}
	// 	}
	// }
}
#endif
