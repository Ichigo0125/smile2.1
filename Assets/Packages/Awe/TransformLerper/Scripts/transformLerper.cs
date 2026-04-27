using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlayerCtr {
	[System.Serializable]
	public class transformInfo{
		// public Vector3 position;
		// public Quaternion rotation;
		// public Vector3 localScale;

		public bool isBzPoint;
		// A Vector
		public Vector3 controlPt;

		[SerializeField]
		public GameObject refObj;

		public bool reverse;
		public AnimationCurve tMap;

		//*** Skip Ensure refObj is Empty ***//
		public Vector3 localPosition{
			get{
				return refObj.transform.localPosition;
			}
		}

		public Quaternion localRotation{
			get{
				return refObj.transform.localRotation;
			}
		}

		public Vector3 localScale{
			get{
				return refObj.transform.localScale;
			}
		}

		// public Vector3 invrCtrPt{
		// 	get{
		// 		if( refObj != null ){
		// 			return refObj.transform.localPosition + (refObj.transform.localPosition - controlPt);
		// 		}
		// 		return new Vector3(0, 0, 0);
		// 	}
		// }

		public static Vector3 BzPoint (Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
			t = Mathf.Clamp01(t);
			float oneMinusT = 1f - t;
			return
				oneMinusT * oneMinusT * oneMinusT * p0 +
				3f * oneMinusT * oneMinusT * t * p1 +
				3f * oneMinusT * t * t * p2 +
				t * t * t * p3;
		}
		public static Vector3 Bz1stTangent (Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
			t = Mathf.Clamp01(t);
			float oneMinusT = 1f - t;
			return
				3f * oneMinusT * oneMinusT * (p1 - p0) +
				6f * oneMinusT * t * (p2 - p1) +
				3f * t * t * (p3 - p2);
		}
	}

	// Attribute Test
	// public class TLStepMenuAttribute : PropertyAttribute {
	// 	public int selected;
	// 	public int maxIndex;

	// 	public TLStepMenuAttribute(int getMax){
	// 		maxIndex = getMax;
	// 	}
	// }

	public class transformLerper : MonoBehaviour {
		// public Vector3 targetPosition { get { return m_TargetPosition; } set { m_TargetPosition = value; } }
		// [SerializeField]
		// private Vector3 m_TargetPosition = new Vector3(1f, 0f, 2f);

		public bool startWithInit = true;
		public int iniInterval = 1;
		public float iniTimeWeight = 0;
		public GameObject controlTarget = null;
		// public List<Transform> getTrans;
		// [HideInInspector]
		[SerializeField]
		public List<transformInfo> steps;

		// [Header("Debug")]
        public bool showDebug = false;
        public int lineSeg = 100;
		public float debug_normalH = 1.0f;
		[Range(0, 1)]
		public float handleSizeScale = 1.0f;


		// [TLStepMenu(10)]
		// public string test;
		private int savedIndex = 0;
		// [Header("Curve Speed Control")]
		public bool staticSpeed = false;
		// [Space(5)]
		public bool autoRotation = false;
		[Range(0, 10)]
		public float bzRotSmooth = 0.01f;

		[Space(5)]
		private Quaternion targetLocalRotation = Quaternion.identity;

		// Use this for initialization
#region Runtime Usage
		void Start () {
			bool validIndex = false;

			iniInterval = indexChecker(iniInterval, out validIndex);

			if( validIndex && startWithInit ){
				Debug.Log("Ini "+this.name);
				playableCtr(iniInterval, iniTimeWeight);
			}
			
		}

		// Update is called once per frame
		public void FixedUpdate () {
			// transform.LookAt(m_TargetPosition);
			//if (autoRotation)
			//	this.transform.localRotation = Quaternion.Lerp( this.transform.localRotation, targetLocalRotation, 0.02f);
		}
#endregion
#region  Editor Functions
		// [Temp] use world space to move the target
		public void addNewStep(){
			if( steps == null ){
				Debug.Log("<color=blue>["+this.gameObject.name+"]</color> Initial transformInfo List", this.gameObject);
				steps = new List<transformInfo>();
			}

			if( steps.Count == 0 ){
				// Add Self
				transformInfo selfInfo = new transformInfo();

				// selfInfo.position 	= this.transform.position;
				// selfInfo.rotation 	= this.transform.rotation;
				// selfInfo.localScale = this.transform.localScale;
				// selfInfo.isBzPoint 	= false;
				// selfInfo.controlPt 	= selfInfo.position + this.transform.up;

				GameObject _self = new GameObject("_TL_"+this.gameObject.GetInstanceID().ToString()+"_0");
				_self.transform.parent = this.transform.parent;
				_self.hideFlags = HideFlags.HideInHierarchy;

				_self.transform.localPosition 	= this.transform.localPosition;
				_self.transform.localRotation 	= this.transform.localRotation;
				_self.transform.localScale 		= this.transform.localScale;
				selfInfo.refObj 	= _self;
				selfInfo.isBzPoint 	= false;
				selfInfo.controlPt 	= this.transform.up;

				steps.Add(selfInfo);
			}

			transformInfo sampleInfo = new transformInfo();

			// if( steps.Count == 0 ){
			// 	sampleInfo.position 	= this.transform.position + this.transform.forward;
			// 	sampleInfo.rotation 	= this.transform.rotation;
			// 	sampleInfo.localScale 	= this.transform.localScale;
			// }else{
			// 	sampleInfo.position 	= steps[steps.Count-1].position + steps[steps.Count-1].rotation * Vector3.forward;
			// 	sampleInfo.rotation 	= steps[steps.Count-1].rotation;
			// 	sampleInfo.localScale 	= steps[steps.Count-1].localScale;
			// }
			// sampleInfo.isBzPoint = false;
			// sampleInfo.controlPt = sampleInfo.position + this.transform.up;
			
			Vector3 newPos = Vector3.zero;
			if( steps.Count-1 == 0 ){
				newPos = steps[steps.Count-1].localPosition 
						+ steps[steps.Count-1].localRotation * Vector3.forward;
			}
			else{
				Vector3 tempDir = steps[steps.Count-1].localPosition - steps[steps.Count-2].localPosition;
				newPos = steps[steps.Count-1].localPosition + tempDir.normalized;
			}

			GameObject _tmpStep = new GameObject("_TL_"+this.gameObject.GetInstanceID().ToString()+"_"+steps.Count);
			_tmpStep.transform.parent = this.transform.parent;
			_tmpStep.hideFlags = HideFlags.HideInHierarchy;
			_tmpStep.transform.localPosition 	= newPos;
			_tmpStep.transform.localRotation 	= steps[steps.Count-1].localRotation;
			_tmpStep.transform.localScale 		= steps[steps.Count-1].localScale;
			sampleInfo.refObj = _tmpStep;
			sampleInfo.isBzPoint = false;
			sampleInfo.controlPt = this.transform.up;

			steps.Add(sampleInfo);
		}

		public void insertStep(int targetIndex){
			targetIndex = Mathf.Max(0, targetIndex);
			transformInfo sampleInfo = new transformInfo();
			
			Vector3 newPos = Vector3.zero;
			Quaternion newRot = steps[targetIndex].localRotation;
			Vector3 newScale = steps[targetIndex].localScale;

			if( targetIndex - 1 < 0){
				newPos = steps[targetIndex].localPosition - steps[targetIndex].refObj.transform.forward;
			}else{
				Vector3 tempDir = steps[targetIndex-1].localPosition - steps[targetIndex].localPosition;
				newPos = steps[targetIndex].localPosition + tempDir.normalized * tempDir.magnitude * 0.5f;
			}
			
			GameObject _tmpStep = new GameObject("_TL_"+this.gameObject.GetInstanceID().ToString()+"_"+steps.Count);
			_tmpStep.transform.parent = this.transform.parent;
			_tmpStep.hideFlags = HideFlags.HideInHierarchy;
			_tmpStep.transform.localPosition 	= newPos;
			_tmpStep.transform.localRotation 	= newRot;
			_tmpStep.transform.localScale 		= newScale;
			sampleInfo.refObj = _tmpStep;
			sampleInfo.isBzPoint = false;
			sampleInfo.controlPt = this.transform.up;

			// steps.Add(sampleInfo);
			steps.Insert(targetIndex, sampleInfo);
		}

		public void toggleStepHideObj(){
			if( steps == null || steps.Count < 1 ){
				return;
			}

			if( steps[0].refObj.hideFlags == HideFlags.HideInHierarchy ){
				Debug.Log("<color=blue>["+this.gameObject.name+"]</color> Show Debug Object.");
				for( int i=0 ; i<steps.Count ; i++ ){
					steps[i].refObj.hideFlags = HideFlags.None;
				}
			}else{
				Debug.Log("<color=blue>["+this.gameObject.name+"]</color> Hide Debug Object.");
				for( int i=0 ; i<steps.Count ; i++ ){
					steps[i].refObj.hideFlags = HideFlags.HideInHierarchy;
				}
			}
		}

		public void followRootObjlScale(){
			if( steps == null || steps.Count < 1 ){
				return;
			}

			for( int i=0 ; i<steps.Count ; i++ ){
				steps[i].refObj.transform.localScale = this.transform.localScale;
			}
		}

		public void trackingFuncObjName(){
			if( steps == null || steps.Count < 1 ){
				return;
			}

			if( this.transform.parent== null ){
				// Do not solve this kind of object now XD
				return;
			}

			Transform[] allChildObjs = this.transform.parent.GetComponentsInChildren<Transform>();

			for( int i=0 ; i<allChildObjs.Length ; i++ ){
				if( allChildObjs[i].gameObject.name.Contains("_TL_") ){
					string[] _postfix = allChildObjs[i].gameObject.name.Split('_');

					allChildObjs[i].gameObject.name = "_TL_" + this.gameObject.GetInstanceID().ToString() + "_" + _postfix[_postfix.Length-1];
				}
			}

		}

		public void shiftAlltoParent(Transform target){
			this.transform.parent = target;

			if( steps != null ){
				for( int i=0 ; i<steps.Count ; i++ ){
					steps[i].refObj.transform.parent = target;
				}
			}
		}

		public void removeStep(int id = -1){
			if( steps.Count < 1 ){
				// Nothing to remove.
				return;
			}

			if( id == -1 ){
				id = steps.Count-1;
			}
			GameObject takeRefObj = steps[id].refObj;
			steps.RemoveAt(id);

			DestroyImmediate(takeRefObj);
		}

		public void removeAllStep(){
			// // P1: clear List
			// steps.Clear();

			// // P2: find Hidden GameObject and Destory
			// GameObject[] allObjs = UnityEngine.Object.FindObjectsOfType<GameObject>() ;
			// for( int i=0 ; i<allObjs.Length ; i++ ){
			// 	if( allObjs[i].name.Contains("_TL_"+this.gameObject.GetInstanceID().ToString()) ){
			// 		// Debug.Log(allObjs[i].name);
			// 		DestroyImmediate(allObjs[i]);
			// 	}
			// }

			for( int i=0 ; i<steps.Count ; i++ ){
				removeStep(i);
			}

			steps.Clear();
		}

		public MeshFilter findSharedMesh(){
			if( controlTarget != null ){
				return controlTarget.GetComponent<MeshFilter>();
			}else{
				return this.gameObject.GetComponent<MeshFilter>();
			}
		}

		public MeshFilter[] findSharedMeshes(){
			if( controlTarget != null ){
				return controlTarget.GetComponentsInChildren<MeshFilter>();
			}else{
				return this.gameObject.GetComponentsInChildren<MeshFilter>();
			}
		}

		public MeshRenderer findMeshRenderer(){
			if( controlTarget != null ){
				return controlTarget.GetComponent<MeshRenderer>();
			}else{
				return this.gameObject.GetComponent<MeshRenderer>();
			}
		}

		public MeshRenderer[] findMeshRenderers(){
			if( controlTarget != null ){
				return controlTarget.GetComponentsInChildren<MeshRenderer>();
			}else{
				return this.gameObject.GetComponentsInChildren<MeshRenderer>();
			}
		}

#endregion
#region Interpolation
		public Vector3 interPos(int index, float interVal){
			if( steps[index-1].isBzPoint || steps[index].isBzPoint ){
				Vector3 p1_ctr = ((steps[index - 1].isBzPoint) ? -steps[index - 1].controlPt : Vector3.zero) + steps[index - 1].localPosition;
				Vector3 p2_ctr = ((steps[index].isBzPoint) ? steps[index].controlPt : Vector3.zero) + steps[index].localPosition;
				if (staticSpeed) {
					interVal = steps[index].tMap.Evaluate(interVal);
					return transformInfo.BzPoint(steps[index - 1].localPosition, p1_ctr, p2_ctr, steps[index].localPosition, interVal);
				}
				else {
					return transformInfo.BzPoint(steps[index - 1].localPosition, p1_ctr, p2_ctr, steps[index].localPosition, interVal);
				}
			}else{
				return Vector3.Lerp(steps[index-1].localPosition, steps[index].localPosition, interVal);
			}
		}

		Vector3 savedFw = Vector3.zero;

		public Quaternion interRot(int index, float interVal) {
			if (autoRotation) {
				bool nonBz = false;
				var forward = Vector3.zero;
				if (steps[index - 1].isBzPoint || steps[index].isBzPoint) {
					Vector3 p1_ctr = ((steps[index - 1].isBzPoint) ? -steps[index - 1].controlPt : Vector3.zero) + steps[index - 1].localPosition;
					Vector3 p2_ctr = ((steps[index].isBzPoint) ? steps[index].controlPt : Vector3.zero) + steps[index].localPosition;
					interVal = steps[index].tMap.Evaluate(interVal);
					forward = transformInfo.Bz1stTangent(steps[index - 1].localPosition, p1_ctr, p2_ctr, steps[index].localPosition, interVal);
					
					if( forward.magnitude == 0 ){
						float sampleSign = (steps[index].isBzPoint) ? 1 : -1;
						forward = sampleSign * transformInfo.BzPoint(steps[index - 1].localPosition, p1_ctr, p2_ctr, steps[index].localPosition, interVal + sampleSign*0.01f)
								- sampleSign * steps[index - 1].localPosition;
						forward = forward.normalized;
					}
				}
				else {
					// if AutoRatation, then from Bz to straight line will have flip issue
					// hense, we use interpolation from now forward to the line froward
					nonBz = true;
					forward = steps[index].localPosition - steps[index - 1].localPosition;
					// forward = steps[index].refObj.transform.position - steps[index - 1].refObj.transform.position;
				}
				if (steps[index].reverse)
					forward = -forward;
				forward.y = 0f;

				if( nonBz ){
					// forward = Vector3.Lerp(savedFw.normalized, forward.normalized, Mathf.Max(interVal * bzRotSmooth, 1));
					return Quaternion.LookRotation(
						Vector3.Lerp(savedFw.normalized, forward.normalized, Mathf.Min(interVal * bzRotSmooth, 1)), 
						Vector3.up);
				}else{
					return Quaternion.LookRotation(forward, Vector3.up);
				}
			}
			else {
				return Quaternion.Lerp(steps[index - 1].localRotation, steps[index].localRotation, interVal);
			} 
		}

		public Vector3 interScale(int index, float interVal){
			return Vector3.Lerp(steps[index-1].localScale, steps[index].localScale, interVal);
		}
		// public Vector3 getNewPos(int index, float interVal){
		// 	bool isValied = false;
		// 	index = indexChecker(index, out isValied);

		// 	if(!isValied){
		// 		return new Vector3(0, 0, 0);
		// 	}

		// 	return Vector3.Lerp(getTrans[index].position, getTrans[(index+1)%getTrans.Count].position, interVal);
		// }

		// public Quaternion getNewRot(int index, float interVal){
		// 	bool isValied = false;
		// 	index = indexChecker(index, out isValied);

		// 	if(!isValied){
		// 		return Quaternion.identity;
		// 	}

		// 	return Quaternion.Lerp(getTrans[index].rotation, getTrans[(index+1)%getTrans.Count].rotation, interVal);
		// }

		// public Vector3 getNewScale(int index, float interVal){
		// 	bool isValied = false;
		// 	index = indexChecker(index, out isValied);

		// 	if(!isValied){
		// 		return new Vector3(0, 0, 0);
		// 	}

		// 	return Vector3.Lerp(getTrans[index].localScale, getTrans[(index+1)%getTrans.Count].localScale, interVal);
		// }

		// private Transform getNewTrans(int index, float interVal){
		// 	bool isValied = false;
		// 	index = indexChecker(index, out isValied);
		// 
		// 	if(!isValied){
		// 		return null;
		// 	}
		// 
		// 	GameObject newObj = new GameObject();
		// 	newObj.transform.position = getNewPos(index, interVal);
		// 	newObj.transform.rotation = getNewRot(index, interVal);
		// 	newObj.transform.localScale = getNewScale(index, interVal);
		// 	return newObj.transform;
		// }
#endregion
#region Playbale Functions
		public void initialIndex(int getID){
			bool isValied = false;
			savedIndex = indexChecker(getID, out isValied);
			savedFw = this.transform.parent.rotation * this.transform.forward;
			if( autoRotation ){
				if( savedIndex - 2 >= 0 ){
					savedFw = steps[savedIndex - 1].localPosition - steps[savedIndex - 2].localPosition;
					savedFw.y = 0;
					savedFw = savedFw.normalized;
				}
			}
		}

		public void playableCtr(int usingIdx, float nTime){
			if(controlTarget == null ){
				this.transform.localPosition 	= interPos(usingIdx, nTime);
				//if (autoRotation)
				//	targetLocalRotation 		= interRot(usingIdx, nTime);
				//else
				this.transform.localRotation = interRot(usingIdx, nTime);
				this.transform.localScale 		= interScale(usingIdx, nTime);
			}else{
				controlTarget.transform.localPosition 	= interPos(usingIdx, nTime);
				controlTarget.transform.localRotation 	= interRot(usingIdx, nTime);
				controlTarget.transform.localScale 		= interScale(usingIdx, nTime);
			}
		}

		public void playableCtr(float nTime){
			// if( controlTarget == null ){
			// 	this.transform.position 	= Vector3.Lerp(getTrans[savedIndex].position, getTrans[(savedIndex+1)%getTrans.Count].position, nTime);
			// 	this.transform.rotation 	= Quaternion.Lerp(getTrans[savedIndex].rotation, getTrans[(savedIndex+1)%getTrans.Count].rotation, nTime);
			// 	this.transform.localScale 	= Vector3.Lerp(getTrans[savedIndex].localScale, getTrans[(savedIndex+1)%getTrans.Count].localScale, nTime);
			// }else{
			// 	controlTarget.transform.position 	= Vector3.Lerp(getTrans[savedIndex].position, getTrans[(savedIndex+1)%getTrans.Count].position, nTime);
			// 	controlTarget.transform.rotation 	= Quaternion.Lerp(getTrans[savedIndex].rotation, getTrans[(savedIndex+1)%getTrans.Count].rotation, nTime);
			// 	controlTarget.transform.localScale 	= Vector3.Lerp(getTrans[savedIndex].localScale, getTrans[(savedIndex+1)%getTrans.Count].localScale, nTime);
			// }
			
		}

		private int indexChecker(int getIndex, out bool isValied){
			if( steps.Count < 2 ){
				isValied = false;
				return 0;
			}
			if( getIndex > steps.Count || getIndex < 0 ){
				getIndex = 0;
				isValied = false;
				return getIndex;
			}

			isValied = true;
			return getIndex;
		}
#endregion

#region SampleBeizerPoints
#if UNITY_EDITOR
		public 	int sampleCount = 100000;
		public float segLength = 1f;
		float totalLength = 0f;
		public void SampleBeizerPoints() {
			for (int i = 1; i < steps.Count; i++) {
				var p0 = steps[i - 1];
				var p1 = steps[i];
				if (p0.isBzPoint || p1.isBzPoint) {
					totalLength = 0f;
					Vector3 p0_ctr = ((p0.isBzPoint) ? -p0.controlPt : Vector3.zero) + p0.localPosition;
					Vector3 p1_ctr = ((p1.isBzPoint) ? p1.controlPt : Vector3.zero) + p1.localPosition;
					var samples = Handles.MakeBezierPoints((p0.localPosition),
											(p1.localPosition),
											(p0_ctr),
											(p1_ctr), sampleCount);

					p1.tMap = new AnimationCurve(new Keyframe(0f , 0.00001f), new Keyframe(1f,0.99999f));

					for (int j = 1; j < sampleCount; j++) {
						totalLength += Vector3.Distance(samples[j], samples[j-1]);
					}
					var nowLength = 0f;
					var nowSegLength = 0f;
					for (int j = 1; j < sampleCount; j++) {
						var dis = Vector3.Distance(samples[j], samples[j-1]);
						nowLength += dis;
						nowSegLength += dis;
						if (nowSegLength > segLength) {
							p1.tMap.AddKey(nowLength / totalLength, (float)j / (sampleCount - 1));
							nowSegLength -= segLength;
						}
					}

					for (int j = 0; j < p1.tMap.length; j++) {
						AnimationUtility.SetKeyLeftTangentMode(p1.tMap, j, AnimationUtility.TangentMode.Linear);
						AnimationUtility.SetKeyRightTangentMode(p1.tMap, j, AnimationUtility.TangentMode.Linear);
					}
				}
			}
		}
#endif
#endregion

#region DebugScope
		void OnDrawGizmos(){
            if(!showDebug){
                return;
            }

			if( steps == null || steps.Count < 2 ){
				return;
			}

			int lineSegPart = Mathf.FloorToInt(lineSeg / (steps.Count-1));

			Matrix4x4 rootMatrix = Matrix4x4.identity;
			if( this.transform.parent != null ){
                rootMatrix = this.transform.parent.localToWorldMatrix;
            }

			float railWidth_pv = 0;
			float railWidth_nt = 0;
			Vector3 rightVec_pv = Vector3.zero;
			Vector3 rightVec_nt = Vector3.zero;

			for( int i=1 ; i<steps.Count ; i++){
				if( steps[i-1].isBzPoint ){
					rightVec_pv = interPos(i, 0.01f) - steps[i-1].refObj.transform.position;
					rightVec_pv = Vector3.Cross(steps[i-1].refObj.transform.up, rightVec_pv.normalized).normalized;
				}else{
					rightVec_pv = steps[i-1].refObj.transform.right;
				}

				if( steps[i].isBzPoint ){
					rightVec_nt = steps[i].refObj.transform.position - interPos(i, 0.99f);
					rightVec_nt = Vector3.Cross(steps[i].refObj.transform.up, rightVec_nt.normalized).normalized;
				}else{
					rightVec_nt = steps[i-1].refObj.transform.right;
				}

				for( int j=1 ; j<=lineSegPart ; j++ ){
					railWidth_pv = (j-1)/(float)lineSegPart;
					railWidth_nt = (j)/(float)lineSegPart;
					Vector3 getTmpPos_pv = interPos(i, railWidth_pv);
					Vector3 getTmpPos_nt = interPos(i, railWidth_nt);

					// Vector3 getTmpScale_nt = getNewScale(i, (j)/(float)lineSegPart);
					// Vector3 getUpVec = getNewRot(i, (j)/(float)lineSegPart) * Vector3.up;

					if( steps[i].isBzPoint || steps[i-1].isBzPoint ){
						Vector3 newRVec_pv = Vector3.Lerp(rightVec_pv, rightVec_nt, railWidth_pv);
						Vector3 newRVec_nt = Vector3.Lerp(rightVec_pv, rightVec_nt, railWidth_nt);
						Vector3 getUpVec = Vector3.Lerp(steps[i-1].refObj.transform.up, steps[i].refObj.transform.up, railWidth_pv);

						if( steps[i].isBzPoint && steps[i-1].isBzPoint ){
							railWidth_pv = 1;
							railWidth_nt = 1;
						}
						else if(steps[i-1].isBzPoint){
							railWidth_pv = 1 - railWidth_pv;
							railWidth_nt = 1 - railWidth_nt;
						}
						railWidth_pv = handleSizeScale * 0.25f * railWidth_pv;
						railWidth_nt = handleSizeScale * 0.25f * railWidth_nt;
						
						newRVec_pv = newRVec_pv * railWidth_pv;
						newRVec_nt = newRVec_nt * railWidth_nt;
						
						Gizmos.color = Color.white;
						Gizmos.DrawLine(rootMatrix.MultiplyPoint3x4(getTmpPos_pv + newRVec_pv), rootMatrix.MultiplyPoint3x4(getTmpPos_nt + newRVec_nt));
						Gizmos.DrawLine(rootMatrix.MultiplyPoint3x4(getTmpPos_pv - newRVec_pv), rootMatrix.MultiplyPoint3x4(getTmpPos_nt - newRVec_nt));
						Gizmos.DrawLine(rootMatrix.MultiplyPoint3x4(getTmpPos_nt + newRVec_nt), rootMatrix.MultiplyPoint3x4(getTmpPos_nt - newRVec_nt));

						Gizmos.color = Color.green;
						Gizmos.DrawLine(rootMatrix.MultiplyPoint3x4(getTmpPos_nt), rootMatrix.MultiplyPoint3x4(getTmpPos_nt) + getUpVec * railWidth_pv);
					}else{
						Gizmos.color = Color.white;
						Gizmos.DrawLine(rootMatrix.MultiplyPoint3x4(getTmpPos_pv), rootMatrix.MultiplyPoint3x4(getTmpPos_nt));
					}
					// float tmpScale = getTmpScale_nt.magnitude/2.0f;
					// Debug.DrawLine(getTmpPos_nt, getTmpPos_nt + getUpVec * debug_normalH * tmpScale, Color.green);
				}
			}
			// End of Draw Debug
        }
#endregion

	}
	
}