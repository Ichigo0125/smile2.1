using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEditor.Timeline;

[CanEditMultipleObjects]
[CustomEditor(typeof(BasedBlenderPlayableAsset))]
public class BasedBlenderPlayableAssetEditor : Editor {
	// private bool needSetBTValue = false;
	// private float blendTestVal = 0;
	public override void OnInspectorGUI(){
		// DrawDefaultInspector();

		serializedObject.Update();
		BasedBlenderPlayableAsset tbFadeAsset = (BasedBlenderPlayableAsset)target;

		var faderObj = serializedObject.FindProperty("m_fader");

		if( faderObj == null || faderObj.exposedReferenceValue == null ){
			EditorGUILayout.HelpBox("Please Assign Fader Script", MessageType.Error);
			EditorGUILayout.PropertyField(faderObj, new GUIContent("Fader Script", "Fader Script"), true);
		}else{
			EditorGUILayout.PropertyField(faderObj, new GUIContent("Fader Script", "Fader Script"), true);

			GUILayout.Space(5);

			EditorGUILayout.LabelField("Misc Setting", EditorStyles.boldLabel);

			EditorGUI.indentLevel++;

			var canDisableRenderer = serializedObject.FindProperty("canDisableRenderer");
			EditorGUILayout.PropertyField(canDisableRenderer, new GUIContent("Can Disable Renderer?", "Used in Clip Start & End"), true);

			var fadeMode = serializedObject.FindProperty("fadeMode");
			EditorGUILayout.PropertyField(fadeMode, new GUIContent("Fade Mode", "Shader: SetFloat(\"_FadeMode\", fadeMode)"), true);

			var _timeMapCurve = serializedObject.FindProperty("_timeMapCurve");
			EditorGUILayout.PropertyField(_timeMapCurve, new GUIContent("Time Curve", "Time Curve Control"), true);

			EditorGUI.indentLevel--;

			GUILayout.Space(5);

			GUILayout.BeginVertical("Box");
				EditorGUILayout.LabelField("Basic Setting", EditorStyles.boldLabel);

				EditorGUI.indentLevel++;
				var isFadeIn = serializedObject.FindProperty("isFadeIn");
				EditorGUILayout.PropertyField(isFadeIn, new GUIContent("Fade Sequence", "True = Fade-in / False = Fade-out"), true);
				
				EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Major BlendTest Control");
					if(GUILayout.Button("0", GUILayout.Width(25))){
						tbFadeAsset.minWeight = 0;
						tbFadeAsset.maxWeight = 0;
					}
					if(GUILayout.Button("-", GUILayout.Width(25))){
						tbFadeAsset.minWeight = 0;
						tbFadeAsset.maxWeight = 1;
					}
					if(GUILayout.Button("1", GUILayout.Width(25))){
						tbFadeAsset.minWeight = 1;
						tbFadeAsset.maxWeight = 1;
					}
				EditorGUILayout.EndVertical();
				minMaxSliderDrawer(ref tbFadeAsset.minWeight, ref tbFadeAsset.maxWeight);

				EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Second Blend2nd Control");
					if(GUILayout.Button("0", GUILayout.Width(25))){
						tbFadeAsset.minWeight2nd = 0;
						tbFadeAsset.maxWeight2nd = 0;
					}
					if(GUILayout.Button("-", GUILayout.Width(25))){
						tbFadeAsset.minWeight2nd = 0;
						tbFadeAsset.maxWeight2nd = 1;
					}
					if(GUILayout.Button("1", GUILayout.Width(25))){
						tbFadeAsset.minWeight2nd = 1;
						tbFadeAsset.maxWeight2nd = 1;
					}
				EditorGUILayout.EndVertical();
				minMaxSliderDrawer(ref tbFadeAsset.minWeight2nd, ref tbFadeAsset.maxWeight2nd);
				EditorGUI.indentLevel--;
			GUILayout.EndVertical();
		}


		serializedObject.ApplyModifiedProperties();
	}

	void minMaxSliderDrawer(ref float targetMin, ref float targetMax){
		GUILayout.BeginHorizontal();
			targetMin = EditorGUILayout.FloatField(targetMin, GUILayout.Width(62));
			EditorGUILayout.MinMaxSlider(ref targetMin, ref targetMax, 0, 1);
			targetMax = EditorGUILayout.FloatField(targetMax, GUILayout.Width(62));
		GUILayout.EndVertical();
	}

	void defaultBlendValDrawer(ref BasedBlenderPlayableAsset tbFadeAsset){
		GUILayout.BeginHorizontal();
			tbFadeAsset.withDefaultOrderVal = EditorGUILayout.Toggle(tbFadeAsset.withDefaultOrderVal, GUILayout.Width(25));
			if( tbFadeAsset.withDefaultOrderVal ){
				var defalutOrderVal = serializedObject.FindProperty("defalutOrderVal");
					EditorGUILayout.PropertyField(defalutOrderVal, new GUIContent("Default Value", "Default Value for BlendTest"), true);
			}
		GUILayout.EndVertical();
	}
}

[CustomTimelineEditor(typeof(BasedBlenderPlayableAsset))]
public class BasedBlenderPlayableAssetClipEditor : ClipEditor
{
	static Color basedColor = new Color(0.35f, 0.0f, 0.9f);
	static Color alertColor = new Color(0.95f, 0.0f, 0.25f);
	// public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom){
	//     base.OnCreate(clip, track, clonedFrom);
 
	//     var otherAsset = clonedFrom?.asset;
 
	//     (clip.asset as BasedBlenderPlayableAsset).Copy(otherAsset);
	// }
 
	// public override void OnClipChanged(TimelineClip clip){
	//     base.OnClipChanged(clip);
	// }

	public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region){
		base.DrawBackground(clip, region);
		
		Color previousGuiColor = GUI.color;	

		BasedBlenderPlayableAsset asset = clip.asset as BasedBlenderPlayableAsset;	
		if (asset == null)
			return;
		
		try{
			var fader = asset.m_fader.Resolve(TimelineEditor.inspectedDirector.playableGraph.GetResolver());
			if( fader == null ){
				clip.displayName = "[Error]";
				EditorGUI.DrawRect(region.position, alertColor);
			}else{
				if( region.position.width < 100 ){
					clip.displayName = System.Enum.GetName(typeof(TimelineBasicTools.FadeMode), asset.fadeMode);
				}else{
					clip.displayName = $"{fader.gameObject.name}: {System.Enum.GetName(typeof(TimelineBasicTools.FadeMode), asset.fadeMode)}";
				}
				
				fadeTypeDrawer(region, 24.0f, asset.isFadeIn == BasedBlenderPlayableAsset.FadeSequence.FadeIn, asset._timeMapCurve);
			}
		
		}catch(System.Exception e){
			Debug.Log(e.Message);
		}
	}

	void fadeTypeDrawer(ClipBackgroundRegion region, float slice, bool isGreater){
		float w = region.position.width / slice;
		float percent = 0;

		var backgroundRegion = new Rect(0, 0, w, 0);
		var gradientColor = basedColor;

		for( int i=0 ; i<slice ; i++ ){
			percent = (float)(i+1) / slice;

			backgroundRegion.x = (isGreater) ? i * w : (slice - i - 1) * w;
			backgroundRegion.height = region.position.height * percent;
			backgroundRegion.y = region.position.height - backgroundRegion.height - 0.1f;

			gradientColor = basedColor * percent;
			gradientColor.a = 1;
			EditorGUI.DrawRect(backgroundRegion, gradientColor);
			// EditorGUI.DrawRect(backgroundRegion, basedColor);
		}
	}

	void fadeTypeDrawer(ClipBackgroundRegion region, float slice, bool isGreater, AnimationCurve _curve){
		float w = region.position.width / slice;
		float percent = 0;

		var backgroundRegion = new Rect(0, 0, w, 0);
		var gradientColor = basedColor;

		for( int i=0 ; i<slice ; i++ ){
			percent = _curve.Evaluate((float)( (isGreater) ? i + 1 : slice - i ) / slice);

			backgroundRegion.x = i * w;
			backgroundRegion.height = region.position.height * percent;
			backgroundRegion.y = region.position.height - backgroundRegion.height - 0.1f;

			gradientColor = basedColor * percent;
			gradientColor.a = 1;
			EditorGUI.DrawRect(backgroundRegion, gradientColor);
			// EditorGUI.DrawRect(backgroundRegion, basedColor);
		}
	}

	public override ClipDrawOptions GetClipOptions(TimelineClip clip){
        var clipOptions = base.GetClipOptions(clip);
        clipOptions.highlightColor = basedColor;
        return clipOptions;
    }
}