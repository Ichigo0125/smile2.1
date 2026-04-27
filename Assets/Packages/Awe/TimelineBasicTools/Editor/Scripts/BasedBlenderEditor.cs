using UnityEngine;
using System.Collections;
using UnityEditor;

namespace TimelineBasicTools
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(BasedBlender))]
	public class BasedBlenderEditor : Editor {
		public float blendTestSimulation = 0;
		public float blend2ndSimulation = 0;
		public FadeMode fadeModeSimulation = FadeMode.Basic;
		public bool isFadeIn = true;

		private EditorWindow viewWindow;
		void OnEnable()
		{
			BasedBlender tbFadeScript = (BasedBlender)target;

			blendTestSimulation 	= tbFadeScript.iniBlendTestVal;
			blend2ndSimulation		= tbFadeScript.iniBlend2ndVal;
			fadeModeSimulation		= tbFadeScript.fadeMode;
			
			viewWindow = EditorWindow.GetWindow<SceneView>();
		}

		public override void OnInspectorGUI(){
			// DrawDefaultInspector();
			serializedObject.Update();
			BasedBlender tbFadeScript = (BasedBlender)target;

			EditorGUILayout.LabelField("Selected Object number = "+targets.Length, EditorStyles.helpBox);

			GUILayout.Space(5);

			GUILayout.BeginHorizontal();
				GUILayout.BeginVertical("Box");
					EditorGUILayout.LabelField("Initial Setting", EditorStyles.boldLabel);
					EditorGUI.indentLevel++;
					var iniBlendTestVal = serializedObject.FindProperty("iniBlendTestVal");
					EditorGUILayout.PropertyField(iniBlendTestVal, new GUIContent("_BlendTest", "Initial _BlendTest"), true);
					var iniBlend2ndVal = serializedObject.FindProperty("iniBlend2ndVal");
					EditorGUILayout.PropertyField(iniBlend2ndVal, new GUIContent("_Blend2nd", "Initial _Blend2nd"), true);
					var fadeMode = serializedObject.FindProperty("fadeMode");
					EditorGUILayout.PropertyField(fadeMode, new GUIContent("_FadeMode", "Initial FadeMode"), true);
					EditorGUI.indentLevel--;
				GUILayout.EndVertical();
				
				GUILayout.BeginVertical("Box");
					EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);
					EditorGUI.BeginChangeCheck();
					blendTestSimulation = EditorGUILayout.Slider(blendTestSimulation, 0, 1);
					blend2ndSimulation 	= EditorGUILayout.Slider(blend2ndSimulation, 0, 1);
					fadeModeSimulation 	= (FadeMode)EditorGUILayout.EnumPopup(fadeModeSimulation);
					// isFadeIn			= EditorGUILayout.Toggle("Is Fade In/Out", isFadeIn);
					isFadeIn			= (BasedBlenderPlayableAsset.FadeSequence)EditorGUILayout.EnumPopup((isFadeIn) ? BasedBlenderPlayableAsset.FadeSequence.FadeIn : BasedBlenderPlayableAsset.FadeSequence.FadeOut) == BasedBlenderPlayableAsset.FadeSequence.FadeIn ? true : false;
				GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			if (EditorGUI.EndChangeCheck()){
				tbFadeScript.blend_playable(
					(isFadeIn) ? blendTestSimulation : 1 - blendTestSimulation, 
					(isFadeIn) ? blend2ndSimulation : 1 - blend2ndSimulation, 
					(int) fadeModeSimulation);
				viewWindow.Repaint();
			}

			// GUILayout.Space(5);
			EditorGUILayout.LabelField("Major Setting", EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			var _linkedRenderers = serializedObject.FindProperty("_linkedRenderers");
			EditorGUILayout.PropertyField(_linkedRenderers, new GUIContent("Linked Renderer List:", "Linked Mesh Renderers"), true);
			EditorGUI.indentLevel--;
			
			// if( needSetBTValue ){
			// 	if( targets.Length == 1 ){
			// 		Undo.RecordObject(tbFadeScript, "SetBlend "+blendTestVal);
			// 		tbFadeScript.setMaterialBlend(blendTestVal);
			// 	}
			// 	else{
			// 		Undo.RecordObjects(targets, "("+targets.Length+")SetBlend "+blendTestVal);
			// 		for( int i=0 ; i<targets.Length ; i++ ){
			// 			TimelineBasicTools.BasedBlender tmpTBFade = (TimelineBasicTools.BasedBlender)targets[i];
			// 			tmpTBFade.setMaterialBlend(blendTestVal);
			// 		}
			// 	}
			// 	needSetBTValue = false;
			// }
			// GUILayout.Space(5);
			
			EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

			GUILayout.BeginHorizontal("Box");

			if(GUILayout.Button("Add Child Renderer")){
				if( targets.Length == 1 ){
					Undo.RecordObject(tbFadeScript, "re-Link Obj");
					tbFadeScript.addChildRenderer(false);
					EditorUtility.SetDirty(tbFadeScript);
				}
				else{
					Undo.RecordObjects(targets, "("+targets.Length+") re-Link Obj ");
					for( int i=0 ; i<targets.Length ; i++ ){
						BasedBlender tmpTBFade = (BasedBlender)targets[i];
						tmpTBFade.addChildRenderer(false);
						EditorUtility.SetDirty(tmpTBFade);
					}
				}
			}

			if(GUILayout.Button("Add Child with Inactive")){
				if( targets.Length == 1 ){
					Undo.RecordObject(tbFadeScript, "re-Link Obj with Inactive");
					tbFadeScript.addChildRenderer(true);
					EditorUtility.SetDirty(tbFadeScript);
				}
				else{
					Undo.RecordObjects(targets, "("+targets.Length+") re-Link Obj with Inactive");
					for( int i=0 ; i<targets.Length ; i++ ){
						BasedBlender tmpTBFade = (BasedBlender)targets[i];
						tmpTBFade.addChildRenderer(true);
						EditorUtility.SetDirty(tmpTBFade);
					}
				}
			}

			if(GUILayout.Button("Clear", GUILayout.Width(50))){
				if( targets.Length == 1 ){
					Undo.RecordObject(tbFadeScript, "Clear LinkedRenderer");
					tbFadeScript.clearLinkedRenderer();
					EditorUtility.SetDirty(tbFadeScript);
				}
				else{
					Undo.RecordObjects(targets, "("+targets.Length+") Clear LinkedRenderer");
					for( int i=0 ; i<targets.Length ; i++ ){
						BasedBlender tmpTBFade = (BasedBlender)targets[i];
						tmpTBFade.clearLinkedRenderer();
						EditorUtility.SetDirty(tmpTBFade);
					}
				}
			}

			GUILayout.EndHorizontal();

			EditorGUILayout.Space();

			if(GUILayout.Button("Remove Invalid Meshrenderer", GUILayout.Width(150))){
				tbFadeScript.RemoveInvalidRenderer();
				EditorUtility.SetDirty(tbFadeScript);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}


}
