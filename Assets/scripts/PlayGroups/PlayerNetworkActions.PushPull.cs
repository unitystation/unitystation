using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;

public partial class PlayerNetworkActions : NetworkBehaviour
{
	[HideInInspector]
	public bool isPulling = false;
	[Command]
	public void CmdPullObject(GameObject obj)
	{
		if (isPulling) {
            GameObject cObj = gameObject.GetComponent<PlayerSync>().pullingObject;
            cObj.GetComponent<PushPull>().pulledBy = null;
            gameObject.GetComponent<PlayerSync>().pullObjectID = NetworkInstanceId.Invalid;
		}
		
		PushPull pulled = obj.GetComponent<PushPull>();
        //Other player is pulling object, send stop on that player
		if (pulled.pulledBy != null) {
            if (pulled.pulledBy != gameObject)
            {
                pulled.GetComponent<PlayerNetworkActions>().CmdStopPulling(obj);
            }
		}

        if (pulled != null)
        {
            PlayerSync pS = GetComponent<PlayerSync>();
            pS.pullObjectID = pulled.netId;
            isPulling = true;
        }
	}

    //if two people try to pull the same object
    [Command]
    public void CmdStopOtherPulling(GameObject obj){
        PushPull objA = obj.GetComponent<PushPull>();
        if (objA.pulledBy != null)
        {
            objA.pulledBy.GetComponent<PlayerNetworkActions>().CmdStopPulling(obj);
        }
    }

	[Command]
	public void CmdStopPulling(GameObject obj)
	{
		if (!isPulling)
			return;
		
		isPulling = false;
		PushPull pulled = obj.GetComponent<PushPull>();
        if (pulled != null)
        {
//			//this triggers currentPos syncvar hook to make sure registertile is been completed on all clients
//			pulled.currentPos = pulled.transform.position;

			PlayerSync pS = gameObject.GetComponent<PlayerSync>();
            pS.pullObjectID = NetworkInstanceId.Invalid;
			pulled.pulledBy = null;
        }
	}

	[Command]
	public void CmdTryPush(GameObject obj, Vector3 pos){
		PushPull pushed = obj.GetComponent<PushPull>();
		if (pushed != null) {
			pushed.serverPos = pos;
		}
	}
}
