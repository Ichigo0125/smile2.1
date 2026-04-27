using UnityEngine;
using System.Collections;
using UnityEditor;  
using UnityEditorInternal;

[CustomEditor(typeof(GM.TimeLinePlayer))]
public class TimeLinePlayerEditor : Editor {
	private ReorderableList chapterList;
	private double totalTime = 0;
	
	private void OnEnable() {
		var chapterSets = serializedObject.FindProperty("ChapterSets");
		chapterList = new ReorderableList(chapterSets.serializedObject, 
			chapterSets,
			true, true, true, true);
		
		chapterList.drawElementCallback = this.DrawElementCallback;
	}

	public override void OnInspectorGUI(){
		// DrawDefaultInspector();
		GM.TimeLinePlayer TLPScript = (GM.TimeLinePlayer)target;

		EditorGUILayout.Space();
		
		if(GUILayout.Button("Language: "+TLPScript.langSet.ToString(), GUILayout.MaxWidth(175))){
			// TLPScript
			TLPScript.langHandler();
		}

		EditorGUILayout.Space();
		serializedObject.Update();
        chapterList.DoLayoutList();

		EditorGUILayout.Space();

		totalTime = TLPScript.getTotalDuration();
		EditorGUILayout.LabelField("Number of Chapter: "+TLPScript.ChapterSets.Count+"\nDuration: "+TLPScript.getTimeStr(totalTime), 
			EditorStyles.helpBox);

        serializedObject.ApplyModifiedProperties();
	}

	private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused){
		GM.TimeLinePlayer TLPScript = (GM.TimeLinePlayer)target;

		var element = chapterList.serializedProperty.GetArrayElementAtIndex(index);
		rect.y += 2;
		float _width = rect.width / 10.0f;

		EditorGUI.LabelField(
			new Rect(rect.x, rect.y, _width * .5f, EditorGUIUtility.singleLineHeight),
			"CS"
		);

		TLPScript.ChapterSets[index].crossScene = EditorGUI.Toggle(
			new Rect(rect.x + _width * 0.5f, rect.y, _width * .5f, EditorGUIUtility.singleLineHeight), 
			TLPScript.ChapterSets[index].crossScene
		);

		if(TLPScript.ChapterSets[index].crossScene){
			EditorGUI.PropertyField(
				new Rect(rect.x + _width * 1, rect.y, _width * 4, EditorGUIUtility.singleLineHeight),
				element.FindPropertyRelative("chapterScene"),
				GUIContent.none
			);

			EditorGUI.PropertyField(
				new Rect(rect.x + _width * 5, rect.y, _width * 2, EditorGUIUtility.singleLineHeight),
				element.FindPropertyRelative("playerPrefab"),
				GUIContent.none
			);

			// if(TLPScript.ChapterSets[index].chapterScene == null){
			// 	EditorGUI.LabelField(
			// 		new Rect(rect.x + _width * 7, rect.y, _width * 3, EditorGUIUtility.singleLineHeight),
			// 		"Please Fill Scene Loader", EditorStyles.miniButtonMid
			// 	);
			// }else{
			// 	if(TLPScript.ChapterSets[index].playerPrefab != null){
			// 		EditorGUI.LabelField(
			// 			new Rect(rect.x + _width * 7, rect.y, _width * 3, EditorGUIUtility.singleLineHeight),
			// 			"["+TLPScript.getTimeStr(TLPScript.ChapterSets[index].playerPrefab.duration)+"] @ "+TLPScript.ChapterSets[index].playerPrefab.name, 
			// 			EditorStyles.boldLabel
			// 		);
			// 	}
			// }
			EditorGUI.LabelField(
					new Rect(rect.x + _width * 7, rect.y, _width * 3, EditorGUIUtility.singleLineHeight),
					"["+TLPScript.getTimeStr(TLPScript.ChapterSets[index].playerPrefab.duration)+"] @ "+TLPScript.ChapterSets[index].playerPrefab.name, 
					EditorStyles.boldLabel
				);
			
		}
		else{
			EditorGUI.PropertyField(
				new Rect(rect.x + _width * 1, rect.y, _width * 3, EditorGUIUtility.singleLineHeight),
				element.FindPropertyRelative("chapter"),
				GUIContent.none
			);

			if(TLPScript.ChapterSets[index].chapter == null){
				EditorGUI.LabelField(
					new Rect(rect.x + _width * 4, rect.y, _width * 6, EditorGUIUtility.singleLineHeight),
					"Please Fill in [TimelineCtr]", EditorStyles.miniButtonMid
				);
			}else{
				EditorGUI.LabelField(
					new Rect(rect.x + _width * 4, rect.y, _width * 6, EditorGUIUtility.singleLineHeight),
					"["+TLPScript.getTimeStr(TLPScript.ChapterSets[index].chapter.player.duration)+"] @ "+TLPScript.ChapterSets[index].chapter.name, 
					EditorStyles.boldLabel
				);
			}
		}
		
		
	}
}
