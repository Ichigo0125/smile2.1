using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer))]
public class lineRenderBinder : MonoBehaviour
{
    private LineRenderer selfLR;
    public bool useWorldCoord = true;
    [Range(0, 1)]
    public float showTH = 0.01f;
    [Header("Object Binding")]
    public GameObject startObj;
    public GameObject endObj;
    // Start is called before the first frame update
    void Start()
    {
        selfLR = this.GetComponent<LineRenderer>();
        selfLR.positionCount = 2;
        selfLR.useWorldSpace = useWorldCoord;

        if( startObj == null ){
            startObj = this.gameObject;
        }

        selfLR.SetPosition(0, startObj.transform.position);

        if( endObj == null ){
            Debug.Log("<color=red>[Error]</color> Please Specified endOBj.", this);
            selfLR.SetPosition(1, startObj.transform.position);
        }else{
            selfLR.SetPosition(1, endObj.transform.position);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if( Vector3.Distance(startObj.transform.position, endObj.transform.position) < showTH ){
            selfLR.enabled = false;
        }else{
            if( endObj != null ){
                selfLR.enabled = true;
                selfLR.SetPosition(0, startObj.transform.position);
                selfLR.SetPosition(1, endObj.transform.position);
            }
        }
        // End if
    }
}
