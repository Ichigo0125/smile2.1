using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(GM.TimelineCtr))]
public class TimelinCtrEditor : Editor {
	private string searchStr = "";
	private bool partialSearch = false;
	public override void OnInspectorGUI(){
		DrawDefaultInspector();

		GM.TimelineCtr TLScript = (GM.TimelineCtr)target;
		GUILayout.BeginVertical("Box");
			GUILayout.Label("Search Clips");
			GUILayout.BeginHorizontal("Box");
				partialSearch = GUILayout.Toggle(partialSearch, "Partial", GUILayout.MaxWidth(55));
				searchStr = GUILayout.TextField(searchStr, 25);
				if(GUILayout.Button("Search", GUILayout.MaxWidth(75))){
					TLScript.findClipAsset(searchStr, partialSearch);
				}
			GUILayout.EndHorizontal();

			if(GUILayout.Button("Find Same Clip Name")){
				TLScript.findSameClipName();
			}
		GUILayout.EndVertical();

		GUILayout.Space(5);

		GUILayout.BeginHorizontal("Box");
		if(GUILayout.Button("Initial")){
			TLScript.initialTimeline();
		}
		if(GUILayout.Button("Testing Parse Clips")){
			// Debug.Log("Nothing Happen XD");
		}
		if(GUILayout.Button("Finished")){
			TLScript.finishTimeline();
		}
		GUILayout.EndHorizontal();
		if( Application.isPlaying ){
			string playStr = (TLScript.isPlaying) ? "Pause" : "Play";
			if(GUILayout.Button(playStr)){
				TLScript.play();
			}
		}
		
	}
}
