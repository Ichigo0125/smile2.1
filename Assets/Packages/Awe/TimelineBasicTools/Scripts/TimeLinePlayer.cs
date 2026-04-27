using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace GM{
	public enum PlayStatus
	{
		_initial,
		_play,
		_pause,
		_stop
	}

	[System.Serializable]
	public class ChapterSetting{
		public bool crossScene = false;
		public TimelineCtr chapter;
		public PlayableAsset playerPrefab;
		// public Valve.VR.SteamVR_LoadLevel chapterScene;

		private string _chapterInfo = "";

		public string chapterInfo{
			get{
				return _chapterInfo;
			}
		}

		public string chapterName{
			get{
				if( crossScene ){
					if( playerPrefab != null ){
						return playerPrefab.name;
					}
				}else{
					if( chapter != null ){
						return chapter.name;
					}
				}
				return "[Error]";
			}
		}

		public double duration{
			get{
				if( crossScene ){
					if( playerPrefab != null ){
						return playerPrefab.duration;
					}
				}else{
					if( chapter != null ){
						return chapter.player.duration;
					}
				}

				return 9999;
			}
		}

		public ChapterSetting(){
			_chapterInfo = "";
		}

		public void initialChapter(){
			if( !crossScene ){
				chapter.initialTimeline();
			}
		}

		public void finishChapter(){
			if( !crossScene ){
				chapter.finishTimeline();
			}
		}

		public void setInfo(string getStr){
			_chapterInfo = getStr;
		}

		public void play(int passID){
			if( crossScene ){
				// if( chapterScene != null ){
				// 	PlayerPrefs.SetString("sendChapterName", chapterName);
				// 	PlayerPrefs.SetInt("sendChapterID", passID);
				// 	chapterScene.Trigger();
				// }
			}else{
				if( chapter != null ){
					chapter.play();
				}
			}
		}

		public void stop(){
			if( crossScene ){
				// Can NOT stop XD
			}else{
				if( chapter != null ){
					chapter.player.Stop();
				}
			}
		}
	}

	public enum _Lang{
		_ZWT,
		_ENG
	};
	
	public class TimeLinePlayer : MonoBehaviour {
		public static TimeLinePlayer instance = null;
		public _Lang langSet = _Lang._ZWT;
		public List<ChapterSetting> ChapterSets = new List<ChapterSetting>();
		// public TimelineCtr[] Chapters;
		private bool showGUI = true;
		private bool isPlaying = false;
		private int nowID = 0;
		private bool lockEvent = false;
		private double totalTime = 0;
		private double timeCnt = 0;
		private bool autoMode = false;
		// private string[] savedChStr;
		public PlayStatus _nowState = PlayStatus._initial;

		private bool allowChapterCtr = true;
		void Awake() {
        	//Check if instance already exists
			if (instance == null) {
				instance = this;
			}
			//If instance already exists and it's not this:
			else if (instance != this) {
				Destroy(gameObject);
			}
			
			bool hasDefaltLang = false;

			try{
				StreamReader paramReader = new StreamReader(Application.streamingAssetsPath + "/chapterSetting");

				// Fist Line
				string tmp = paramReader.ReadLine();
				Debug.Log("<color=white>[Cfg]</color> Allow Chapter Control: "+tmp);
				if( int.Parse(tmp) == 1 ){
					allowChapterCtr = true;
				}else{
					allowChapterCtr = false;
				}
				// Second Line
				tmp = paramReader.ReadLine();
				Debug.Log("<color=white>[Cfg]</color> AutoMode: "+tmp);
				if( int.Parse(tmp) == 1 ){
					autoMode = true;
				}else{
					autoMode = false;
				}
				// Third Line
				tmp = paramReader.ReadLine();
				if( tmp.Length > 1 ){
					hasDefaltLang = true;
					langSet = (_Lang) System.Enum.Parse(typeof(_Lang), tmp);
					Debug.Log("<color=white>[Cfg]</color> "+langSet+": "+tmp);
				}else{
					hasDefaltLang = false;
				}
				
			}catch(System.Exception e){
				Debug.Log(e);
				allowChapterCtr = false;
			}
			
			if( PlayerPrefs.HasKey("_Lang") && !hasDefaltLang ){
				langSet = (_Lang) PlayerPrefs.GetInt("_Lang");
			}
			// Sets this to not be destroyed when reloading scene
			// DontDestroyOnLoad(gameObject);
		}

		// Use this for initialization
		void Start () {
			_nowState = PlayStatus._initial;
			initialAllChapter();
			_nowState = PlayStatus._stop;
			
			string getPassStr = PlayerPrefs.GetString("sendChapterName");
			// int getPassIdx = ChapterSets.FindIndex(ch => ch.chapterName == getPassStr);
			int getPassIdx = PlayerPrefs.GetInt("sendChapterID");
			Debug.Log(">>"+getPassStr +" | "+getPassIdx);

			for( int i=0 ; i<ChapterSets.Count ; i++ ){
				if( ChapterSets[i].crossScene ){
					timeCnt += ChapterSets[i].duration;
					continue;
				}else{
					nowID = i;
					break;
				}
			}

			if( getPassIdx != -1 ){	// Means AutoPlay
				PlayerPrefs.SetString("sendChapterName", "");
				PlayerPrefs.SetInt("sendChapterID", -1);

				if( nowID != 0 ){
					ChapterSets[nowID].play(nowID);
					_nowState = PlayStatus._play;
					isPlaying = true;
				}
				
			}

			if( autoMode && !isPlaying ){
				ChapterSets[nowID].play(nowID);
				_nowState = PlayStatus._play;
				isPlaying = true;
			}

			if( autoMode ){
				showGUI = false;
			}
		}

		void initialAllChapter(){
			totalTime = 0;
			timeCnt = 0;

			if( ChapterSets != null ){
				for( int i=ChapterSets.Count-1 ; i >= 0 ; i-- ){
					ChapterSets[i].initialChapter();

					if( !ChapterSets[i].crossScene ){
						// Temp
						ChapterSets[i].chapter.player.played  += OnChapterPlayed;
						ChapterSets[i].chapter.player.stopped += OnChapterStopped;
					}

					totalTime += ChapterSets[i].duration;
					ChapterSets[i].setInfo("<b>" + ChapterSets[i].chapterName + "</b> " + getTimeStr(ChapterSets[i].duration));
				}
			}
		}

		public double getTotalDuration(){
			totalTime = 0;
			if( ChapterSets != null ){
				for( int i=ChapterSets.Count-1 ; i >= 0 ; i-- ){
					totalTime += ChapterSets[i].duration;
				}
			}
			return totalTime;
		}
		
		// Update is called once per frame
		void Update () {
			if( Input.GetKeyDown(KeyCode.F1) ){
				showGUI = !showGUI;
			}
			if( isPlaying ){
				timeCnt += Time.deltaTime;
			}
		}

		void OnDisable(){
			if( ChapterSets != null ){
				for( int i=0 ; i<ChapterSets.Count ; i++ ){
					if( !ChapterSets[i].crossScene ){
						// Temp
						ChapterSets[i].chapter.player.played  -= OnChapterPlayed;
						ChapterSets[i].chapter.player.stopped -= OnChapterStopped;
					}
				}
			}
			
			// PlayerPrefs.SetString("sendChapterName", "");
			// PlayerPrefs.SetInt("sendChapterID", -1);
		}

		void OnApplicationQuit()
		{
			PlayerPrefs.SetString("sendChapterName", "");
			PlayerPrefs.SetInt("sendChapterID", -1);
		}

		public string getTimeStr(double getTime){
			System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(getTime);
			return string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
		}

		int timeScaleGrade = 1;

		void OnGUI(){
			if(showGUI){
				string playStr = (isPlaying) ? "Pause" : "Play";
				Vector2 LUpos = new Vector2(10, 20);

				if (GUI.Button(new Rect(LUpos.x, LUpos.y, 100, 100), 
					playStr+"\n<b>" + ChapterSets[nowID].chapterName+"</b>\n"+
					getTimeStr(timeCnt)+" | "+getTimeStr(totalTime))
					){

					isPlaying = !isPlaying;
					if( isPlaying ){
						_nowState = PlayStatus._play;
					}else{
						_nowState = PlayStatus._pause;
					}

					ChapterSets[nowID].play(nowID);
				}
				if( !Application.isEditor && Application.isPlaying ){
					if (GUI.Button(new Rect(LUpos.x + 110, LUpos.y, 25, 25), "<color=red>X</color>")){
						Application.Quit();
					}
					if (GUI.Button(new Rect(LUpos.x + 110, LUpos.y+30, 25, 70), "RE")){
						UnityEngine.SceneManagement.SceneManager.LoadScene(0);
						PlayerPrefs.SetString("sendChapterName", "");
						PlayerPrefs.SetInt("sendChapterID", -1);
					}
				}

				if (GUI.Button(new Rect(LUpos.x, LUpos.y+105, 100, 25), "Lang: "+langSet.ToString())){
					langHandler();
					PlayerPrefs.SetInt("_Lang", (int) langSet);
					OnChapterPlayed(ChapterSets[nowID].chapter.player);
				}

				if (GUI.Button(new Rect(LUpos.x + 105, LUpos.y+105, 35, 25), "x"+Time.timeScale.ToString())){
					timeScaleGrade = Mathf.Max(1, ( timeScaleGrade * 2 ) % 16);
					Time.timeScale = timeScaleGrade;
				}

				Rect chBtnCfg = new Rect(10, 10, 110, 25);
				for( int i=0 ; i<ChapterSets.Count && allowChapterCtr ; i++ ){
					string showChStr = ChapterSets[i].chapterInfo;
					int shiftHint = 0;

					if( nowID == i ){
						showChStr = "<color=cyan>"+showChStr+"</color>";
						shiftHint = 5;
					}
					if (GUI.Button(new Rect(LUpos.x + shiftHint, LUpos.y + 110 + 35 + (chBtnCfg.y + chBtnCfg.height)*i, chBtnCfg.width, chBtnCfg.height), showChStr)){
						isPlaying = false;
						_nowState = PlayStatus._stop;
						timeCnt = 0;
						nowID = i;
						
						lockEvent = true;
						if( ChapterSets[i].crossScene ){
							ChapterSets[i].play(i);
						}else{
							StartCoroutine(finishedAssignChapter(i));
							StartCoroutine(delayChapterPlay( 0.1f * (float)(ChapterSets.Count+1) ));
						}
					}
					// Finish Chapter Buttons
				}
			}
		}

		public void langHandler(){
			int numLangs = System.Enum.GetValues(typeof(_Lang)).Length;

			int nowLangNum = (int)langSet;
			nowLangNum = (nowLangNum + 1) % numLangs;
			langSet = (_Lang) nowLangNum;
		}

		IEnumerator delayChapterPlay(float delayTime){
			yield return new WaitForSeconds(delayTime);

			lockEvent = false;
			isPlaying = true;
			_nowState = PlayStatus._play;
			ChapterSets[nowID].play(nowID);
		}

		IEnumerator finishedAssignChapter(int toAssignID){
			toAssignID = Mathf.Min(ChapterSets.Count - 1, toAssignID);

			for( int i=ChapterSets.Count - 1 ; i >= toAssignID ; i-- ){
				ChapterSets[i].stop();
				ChapterSets[i].initialChapter();
			}

			for( int i=0 ; i<toAssignID ; i++ ){
				ChapterSets[i].finishChapter();
				timeCnt += ChapterSets[i].duration;
				yield return new WaitForSeconds(0.1f);
			}
		}
		

		#region Timeline Events

		void OnChapterPlayed(PlayableDirector aDirector){
			// Parse Track
			var timelineAsset = aDirector.playableAsset as TimelineAsset;
			int x=0;
			foreach (var track in timelineAsset.GetOutputTracks()){
				if( track.name.Contains("_") ){
					// Debug.Log(x+"||"+track.name);
					// Means it would have language preset
					if( track.name.Contains(langSet.ToString()) ){
						track.muted = false;
					}else{
						var langItems = System.Enum.GetNames(typeof(_Lang));

						for( int i=0 ; i<langItems.Length ; i++ ){
							if( langItems[i] == langSet.ToString() ){
								continue;
							}
							else if( track.name.Contains(langItems[i]) ){
								track.muted = true;
								break;
							}
						}
					}
				}
				// Debug.Log(x+"||"+track.name);
				x++;
			}

			// aDirector.Evaluate();
			double nowTime = aDirector.time;
			if(  nowTime < 1 ){
				// Perhaps it is first played
				TimelineCtr tlCtr = aDirector.gameObject.GetComponent<TimelineCtr>();
				if( tlCtr != null ){
					tlCtr.initialTimeline();
				}
			}else{
				aDirector.RebuildGraph();
				aDirector.time = nowTime;
				aDirector.Evaluate();
			}
			
		}
		void OnChapterStopped(PlayableDirector aDirector){
			if(lockEvent)
				return;
			Debug.Log("<color=red> ->> "+aDirector.name+" End. </color>");
			if( aDirector.Equals(ChapterSets[nowID].chapter.player) ){
				if( nowID == ChapterSets.Count - 1 ){	// Mean: Last One
					if( autoMode ){
						Debug.Log("<color=white>[Cfg]<color> Auto Quit.");
						Application.Quit();
					}
				}
				else{
					nowID = Mathf.Min(ChapterSets.Count - 1, nowID + 1);
					ChapterSets[nowID].play(nowID);
					isPlaying = true;
				}
			}
		}

		#endregion

	}
}

public interface IGenPlayable<T>{
    void Play_in_Playable(T nTime, bool isInverse = false, int partID = 0);
	void initialVal(int intervalID = 0);
}
