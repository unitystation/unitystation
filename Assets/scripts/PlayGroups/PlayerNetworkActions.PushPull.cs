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
		if (isPulling)
			return;
		
		ObjectActions pulled = obj.GetComponent<ObjectActions>();
		if(pulled != null)
		pulled.PulledBy = playerMove.netId;
		isPulling = true;
	}

	[Command]
	public void CmdStopPulling(GameObject obj)
	{
		isPulling = false;
		ObjectActions pulled = obj.GetComponent<ObjectActions>();
		if(pulled != null)
		pulled.PulledBy = NetworkInstanceId.Invalid;
	}

	[Command]
	public void CmdPushObject(GameObject obj)
	{
		ObjectActions pushed = obj.GetComponent<ObjectActions>();
		if (pushed != null) {
			pushed.RpcTryToPush(playerMove.transform.position, playerMove.speed);
			if (pushed.PulledBy == playerMove.netId) {
				pushed.PulledBy = NetworkInstanceId.Invalid;
				isPulling = false;
			}
		}
	}
}
