using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace GM
{
	public class TimelineCtr : MonoBehaviour {

		public PlayableDirector player;
		public bool startWithInitial = false;
		public bool isPlaying {
			get{
				return _isPlaying;
			}
		}
		[Space(5)]

		private bool _isPlaying = false;
		private Dictionary<string, double> clipInitialInfo;
		// Use this for initialization
		void Start () {
			if( startWithInitial ){
				initialTimeline();
			}
		}
		// 
		// // Update is called once per frame
		// void Update () {
		// 	
		// }

		// public void Update(){
		// 	if (Input.GetKeyDown("space")){
		// 		initialTimeline();
		// 		player.Play();
		// 		_isPlaying = true;
		// 		// play();
		// 	}
		// 	if(startPlayBackward){
		// 		double t = player.time - Time.deltaTime;
		// 		if (t < 0)
		// 			t = 0;
		
		// 		player.time = t;
		// 		player.Evaluate();
		
		// 		if (t == 0) {
		// 			player.Stop();
		// 			startPlayBackward = false;
		// 			// enabled = false;
		// 		}
		// 	}
		// }
		public void play(){
			if( _isPlaying ){
				player.Pause();
				_isPlaying = false;
			}
			else{
				player.Play();
				_isPlaying = true;
			}
		}
		
		public void forcePlay(){
			player.Play();
			_isPlaying = true;
		}

		public void pause(){
			player.Pause();
			_isPlaying = false;
		}

		public void initialTimeline(){
			if( player == null ){
				Debug.Log("Please Assigne the Timeline Obj!");
				return;
			}

			player.RebuildGraph();
			player.time = 0;
			player.Evaluate();

			try{
				parseClips(true);
			}catch(System.Exception e){
				Debug.Log("<color=red>["+this.gameObject.name+"]</color>"+e);
			}
		}

		public void finishTimeline(){
			if( player == null ){
				Debug.Log("Please Assigne the Timeline Obj!");
				return;
			}

			// player.RebuildGraph();
			player.Stop();
			player.time = player.playableAsset.duration - 0.01;
        	player.Evaluate();

			try{
				parseClips(false);
			}catch(System.Exception e){
				Debug.Log("<color=red>["+this.gameObject.name+"]</color>"+e);
			}
		}

		public void findSameClipName(){
			Dictionary<string, List<string>> sameClipDic = new Dictionary<string, List<string>>();
			Dictionary<string, GameObject> sameClipDic_GO = new Dictionary<string, GameObject>();

			var playableDirector = player;
	
			// clip access: @see: https://forum.unity3d.com/threads/access-the-animation-clip-i-created-in-a-playable-through-code.487193/
			var timelineAsset = playableDirector.playableAsset as TimelineAsset;
			foreach (var track in timelineAsset.GetOutputTracks())
			{
				var playableTrack = track as PlayableTrack;
				if (playableTrack != null)
				{
					foreach (var clip in playableTrack.GetClips())
					{
						var asset = clip.asset as BasedBlenderPlayableAsset;
						if (asset){
							// string getName = track.name+"."+clip.displayName;
							var m_fader = asset.m_fader.Resolve(playableDirector.playableGraph.GetResolver());
							if(m_fader == null){
								Debug.Log("["+track.name+"]<color=blue>["+(clip.start).ToString("F2")+" @ "+clip.displayName+"]</color>["+asset.name+"]: m_fader is null");
								continue;
							}

							string completeName = clip.displayName + " | " + m_fader.gameObject.name;
							string showClipName = "<color=blue>"+(clip.start).ToString("F2")+"</color>";
							string fadeInfo = System.Enum.GetName(typeof(TimelineBasicTools.FadeMode), asset.fadeMode);
							string finalStr = "[<color=blue>"+track.name+"</color>]["+showClipName+"]["+asset.name+"]: "+ fadeInfo + ".";

							if(sameClipDic.ContainsKey(completeName)){
								sameClipDic[completeName].Add(finalStr);
							}else{
								List<string> logInfo = new List<string>();

								logInfo.Add(finalStr);
								sameClipDic.Add(completeName, logInfo);

								// For GameObject
								sameClipDic_GO.Add(completeName, m_fader.gameObject);
							}
						}
					}
					// End of Find Clips
				}
			}
			// End of Find Tracks
			int distinglishClips = 0;
			foreach(KeyValuePair<string, List<string>> item in sameClipDic){
				if( item.Value.Count > 1 ){
					string titleLog = item.Key.Replace("|", "</b>| GameObject:<color=red>");
					titleLog = "clip: <b>" + titleLog + "</color>";
					Debug.Log(titleLog, sameClipDic_GO[item.Key]);

					for( int i=0 ; i<item.Value.Count ; i++ ){
						Debug.Log(item.Value[i]);
					}
					distinglishClips ++;
				}

				// Release Source
				item.Value.Clear();
			}
			if( distinglishClips == 0){
				Debug.Log("<color=yellow>Perfect!</color>");
			}else{
				Debug.Log("<b> "+distinglishClips+" </b> Results.");
			}

			// Release Source
			sameClipDic.Clear();
			sameClipDic_GO.Clear();
		}
		public void findClipAsset(string searchName, bool isPartial = false){
			var playableDirector = player;
	
			// clip access: @see: https://forum.unity3d.com/threads/access-the-animation-clip-i-created-in-a-playable-through-code.487193/
			int findCounter = 0;
			var timelineAsset = playableDirector.playableAsset as TimelineAsset;
			foreach (var track in timelineAsset.GetOutputTracks())
			{
				var playableTrack = track as PlayableTrack;
				if (playableTrack != null)
				{
					foreach (var clip in playableTrack.GetClips())
					{
						var asset = clip.asset as BasedBlenderPlayableAsset;
						if (asset){
							// string getName = track.name+"."+clip.displayName;
							
							bool showFlag = false;
							string showClipName = clip.displayName;

							if(isPartial){
								if( clip.displayName.Contains(searchName) ){
									showClipName = showClipName.Replace(searchName, "<b>"+searchName+"</b>");
									showClipName = "<color=blue>"+(clip.start).ToString("F2")+" @ "+showClipName+"</color>";
									showFlag = true;
								}
							}else{
								if( clip.displayName == searchName ){
									showClipName = "<color=blue>"+(clip.start).ToString("F2")+" @ "+showClipName+"</color>";
									showFlag = true;
								}
							}

							if(showFlag){
								string isFadeInStr = System.Enum.GetName(typeof(TimelineBasicTools.FadeMode), asset.fadeMode);

								var m_fader = asset.m_fader.Resolve(playableDirector.playableGraph.GetResolver());
								if( m_fader == null ){
									Debug.Log("["+track.name+"]["+showClipName+"]["+asset.name+"]: Hi! "+isFadeInStr + ", can't find original object");
								}else{
									Debug.Log("["+track.name+"]["+showClipName+"]["+asset.name+"]: Hi! "+isFadeInStr, m_fader.gameObject);
								}
								findCounter++;
							}

						}
					}
					// End of Find Clips
				}
			}
			// End of Find Tracks
			if(findCounter == 0){
				Debug.Log("<color=yellow>Nothing Here!</color>");
			}else{
				Debug.Log("<b> "+findCounter+" </b> Results.");
			}
		}
		private struct TL_RtnParam{
			public bool neetSet;
			public float nTime;
			public bool actRnd;
		}

		private TL_RtnParam checkInDic(bool isInit, string getName, TimelineClip clip) {
			TL_RtnParam rtnParam = new TL_RtnParam();
			
			rtnParam.neetSet = false;
			if( clipInitialInfo.ContainsKey(getName) ){
				if(isInit){
					if( clipInitialInfo[getName] > clip.start ){
						clipInitialInfo[getName] = clip.start;

						rtnParam.neetSet 	= true;
						rtnParam.nTime 		= 0;
						rtnParam.actRnd 	= false;
					}
				}else{
					if( clipInitialInfo[getName] < clip.start ){
						clipInitialInfo[getName] = clip.start;

						rtnParam.neetSet 	= true;
						rtnParam.nTime		= 1;
						rtnParam.actRnd 	= true;
					}
				}
			}else{
				clipInitialInfo.Add(getName, clip.start);
				if(isInit){
					rtnParam.neetSet 	= true;
					rtnParam.nTime 		= 0;
					rtnParam.actRnd 	= false;
				}else{
					rtnParam.neetSet 	= true;
					rtnParam.nTime 		= 1;
					rtnParam.actRnd 	= true;
				}
			}

			return rtnParam;
		}

		private void parseClips(bool isInit = true){
			if( clipInitialInfo == null ){
				clipInitialInfo = new Dictionary<string, double>();
			}
			clipInitialInfo.Clear();

			var playableDirector = player;
	
			// clip access: @see: https://forum.unity3d.com/threads/access-the-animation-clip-i-created-in-a-playable-through-code.487193/
			var timelineAsset = playableDirector.playableAsset as TimelineAsset;
			foreach (var track in timelineAsset.GetOutputTracks()){
				var playableTrack = track as PlayableTrack;

				if (playableTrack != null){
					foreach (var clip in playableTrack.GetClips()){
						if( clip.asset.GetType() == typeof(BasedBlenderPlayableAsset) ){
							BasedBlenderPlayableAsset asset = (BasedBlenderPlayableAsset)clip.asset;

							if (asset){
								string getName = clip.displayName + "." + asset.name;
								var m_fader = asset.m_fader.Resolve(playableDirector.playableGraph.GetResolver());

								if(m_fader == null){
									Debug.Log("["+track.name+"]<color=red>["+(clip.start).ToString("F2")+" @ "+clip.displayName+"]</color>["+asset.name+"]: m_fader is null");
									continue;
								}

								// if( m_fader.donotReset ){
								// 	Debug.Log("["+track.name+"]<color=blue>["+(clip.start).ToString("F2")+" @ "+clip.displayName+"]</color>["+asset.name+"]: Set As <color=red>Do Not</color> Reset(initial).");
								// 	continue;
								// }

								TL_RtnParam rtnParam = checkInDic(isInit, getName, clip);

								if( rtnParam.neetSet ){
									// m_fader.fading_playable(rtnParam.nTime, asset.isFadeIn, asset.fadeMode);
									// Fixed Initial Bug Here
									// m_fader.fading_playable(
									// 	asset.findWeight( asset._timeMapCurve.Evaluate(rtnParam.nTime) ), 
									// 	asset.isFadeIn, (int)asset.fadeMode);
									if( asset.isFadeIn == BasedBlenderPlayableAsset.FadeSequence.FadeIn ){
										m_fader.activeAllRenders(rtnParam.actRnd);
									}
								}
								
							}
						}
						// else if( clip.asset.GetType() == typeof(PlayerCtr.TransformLerperPlayableAsset) ){
						// 	PlayerCtr.TransformLerperPlayableAsset asset = (PlayerCtr.TransformLerperPlayableAsset)clip.asset;

						// 	if (asset){
						// 		var m_transL = asset.m_transL.Resolve(playableDirector.playableGraph.GetResolver());

						// 		if(m_transL == null){
						// 			Debug.Log("["+track.name+"]<color=red>["+(clip.start).ToString("F2")+" @ "+clip.displayName+"]</color>["+asset.name+"]: m_fader is null");
						// 			continue;
						// 		}
						// 		string getName = clip.displayName + "." + m_transL.name;

						// 		// if( m_fader.donotReset ){
						// 		// 	Debug.Log("["+track.name+"]<color=blue>["+(clip.start).ToString("F2")+" @ "+clip.displayName+"]</color>["+asset.name+"]: Set As <color=red>Do Not</color> Reset(initial).");
						// 		// 	continue;
						// 		// }
								
						// 		TL_RtnParam rtnParam = checkInDic(isInit, getName, clip);

						// 		if( rtnParam.neetSet ){
						// 			Debug.Log("["+track.name+"]<color=blue>["+(clip.start).ToString("F2")+" @ "+clip.displayName+"]</color>["+asset.name+"]: Initial index = "+asset.playIndex);
						// 			m_transL.playableCtr(
						// 				asset.playIndex,
						// 				asset.findNTime()
						// 			);
						// 		}
						// 	}
						// }
						// else if ( clip.asset.GetType() == typeof(SkyboxMatFadePAsset) ){
						// 	SkyboxMatFadePAsset asset = (SkyboxMatFadePAsset)clip.asset;

						// 	if (asset){
						// 		// This Script Should Only one
						// 		string getName = "SkyboxMatFadePAsset"; //clip.displayName + "." + asset.name;
						// 		var m_fader = asset.m_fader.Resolve(playableDirector.playableGraph.GetResolver());

						// 		if(m_fader == null){
						// 			Debug.Log("["+track.name+"]<color=blue>["+(clip.start).ToString("F2")+" @ "+clip.displayName+"]</color>["+asset.name+"]: m_fader is null");
						// 			continue;
						// 		}

						// 		TL_RtnParam rtnParam = checkInDic(isInit, getName, clip);

						// 		if( rtnParam.neetSet ){
						// 			m_fader.setBlendParam(asset.from, asset.to);
						// 			m_fader.reset();
						// 			m_fader.fading_playable(rtnParam.nTime, asset.isFadeIn, asset.fadeMode);
						// 		}
								
						// 	}
						// }
						// else if ( clip.asset.GetType() == typeof(EnvColorPlayableAsset) ){
						// 	EnvColorPlayableAsset asset = (EnvColorPlayableAsset)clip.asset;

						// 	if (asset){
						// 		// This Script Should Only one
						// 		string getName = "EnvColorPlayableAsset"; //clip.displayName + "." + asset.name;
						// 		var m_fader = asset.m_fader.Resolve(playableDirector.playableGraph.GetResolver());

						// 		if(m_fader == null){
						// 			Debug.Log("["+track.name+"]<color=blue>["+(clip.start).ToString("F2")+" @ "+clip.displayName+"]</color>["+asset.name+"]: m_fader is null");
						// 			continue;
						// 		}

						// 		TL_RtnParam rtnParam = checkInDic(isInit, getName, clip);

						// 		if( rtnParam.neetSet ){
						// 			m_fader.reset(asset.from, asset.to);
						// 			m_fader.fading_playable(rtnParam.nTime, asset.isFadeIn, asset.fadeMode);
						// 		}
								
						// 	}
						// }
						// else if ( clip.asset.GetType() == typeof(SampleLightPlayableAsset) ){
						// 	SampleLightPlayableAsset asset = (SampleLightPlayableAsset)clip.asset;

						// 	if (asset){
						// 		// This Script Should Only one
						// 		string getName = track.name+"."+clip.displayName;
						// 		var m_fader = asset.m_fader.Resolve(playableDirector.playableGraph.GetResolver());

						// 		if(m_fader == null){
						// 			Debug.Log("["+track.name+"]<color=blue>["+(clip.start).ToString("F2")+" @ "+clip.displayName+"]</color>["+asset.name+"]: m_fader is null");
						// 			continue;
						// 		}

						// 		TL_RtnParam rtnParam = checkInDic(isInit, getName, clip);

						// 		if( rtnParam.neetSet ){
						// 			m_fader.initialVal(asset.from, asset.to);
						// 			m_fader.fading_playable(rtnParam.nTime, asset.isFadeIn, asset.fadeMode);
						// 		}
								
						// 	}
						// }
						else{
							// Left will be Curve Words Script
							// Debug.Log("["+track.name+"]<color=blue>["+(clip.start).ToString("F2")+" @ "+clip.displayName+"]</color>["+clip.asset.name+"]: not BasedBlenderPlayableAsset");
						}
						
					}
					// End of Find Clips
				}
			}
			// End of Find Tracks
		}
	}
}

