using UnityEngine;
using System.Collections;

namespace TimelineBasicTools{
    public enum FadeMode
	{
		Basic = 0,
		Mode1 = 1,
		Mode2 = 2,
		Mode3 = 3,
		Mode4 = 4
	}

	public enum FuncOrder{
		_BlendTest = 0,
		_BlendT2rd = 1
	}

    public class BasedBlender : MonoBehaviour{
        public float iniBlendTestVal = 0;
        public float iniBlend2ndVal = 0;
        public FadeMode fadeMode = FadeMode.Basic;
		public bool alwaysActive = false;
        
        [SerializeField] Renderer[] _linkedRenderers;
        MaterialPropertyBlock _sheet;

        private void Awake(){
            if (_linkedRenderers == null || _linkedRenderers.Length == 0){
				Debug.LogWarning($"{this.gameObject.name}: Empty _linkedRenderers!");
			}
        }
        // // Start is called before the first frame update
        // void Start()
        // {
            
        // }

        // // Update is called once per frame
        // void Update()
        // {
            
        // }

#region Function For Editor
        
        public void addChildRenderer(bool withInActive = false){
			_linkedRenderers = this.gameObject.GetComponentsInChildren<Renderer>(withInActive);
		}

        public void clearLinkedRenderer(){
            System.Array.Clear(_linkedRenderers, 0, _linkedRenderers.Length);
        }

        public void RemoveInvalidRenderer(){
            var _rds = this.gameObject.GetComponentsInChildren<Renderer>(true);

            for( int i=0 ; i<_rds.Length ; i++ ){
                if( _rds[i].sharedMaterial == null || _rds[i].sharedMaterials.Length == 0 ){
                    Debug.Log("-- Remove Invalid Renderer: "+_rds[i].gameObject.name, _rds[i].gameObject);
                    DestroyImmediate(_rds[i]);
                }
            }
        }

#endregion

#region Status Control Function
        public void activeAllRenders(bool enableState){
			if (_linkedRenderers == null || _linkedRenderers.Length == 0) return;

			if(Application.isPlaying){
				foreach (var renderer in _linkedRenderers){
					renderer.enabled = enableState || alwaysActive;
				}
			}
		}
        public void reset(){
            if (_linkedRenderers == null || _linkedRenderers.Length == 0) return;

            if (_sheet == null) _sheet = new MaterialPropertyBlock();

            if( Application.isEditor && !Application.isPlaying ){
				// Means reset function maybe call from Editor
			}

            try{
                for( int i=0 ; i<_linkedRenderers.Length ; i++ ){
                    _linkedRenderers[i].GetPropertyBlock(_sheet);

                    _sheet.SetFloat("_BlendTest", iniBlendTestVal);
                    _sheet.SetFloat("_BlendT2nd", iniBlend2ndVal);
                    _sheet.SetFloat("_FadeMode", (float)fadeMode);

                    _linkedRenderers[i].SetPropertyBlock(_sheet);
                }
            }catch(System.Exception e){
				Debug.LogError($"{this.gameObject.name}: _linkedRenderers Array Error.");
				Debug.LogException(e, this);
			}
        }

        public void blend_playable(float nTime, float nTime2nd, int fadeMode = 0 ){
            if (_linkedRenderers == null || _linkedRenderers.Length == 0) return;

            if (_sheet == null) _sheet = new MaterialPropertyBlock();

            foreach (var renderer in _linkedRenderers){
				renderer.GetPropertyBlock(_sheet);

				_sheet.SetFloat("_BlendTest", nTime);
				_sheet.SetFloat("_BlendT2nd", nTime2nd);
				_sheet.SetFloat("_FadeMode", fadeMode);
				
				renderer.SetPropertyBlock(_sheet);
			}
        }
#endregion
    
    }
}

