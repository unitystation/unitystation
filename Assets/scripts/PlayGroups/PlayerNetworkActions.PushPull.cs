using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;

public partial class PlayerNetworkActions : NetworkBehaviour
{
	[Command]
	public void CmdPullObject(GameObject obj)
	{
		var pulled = obj.GetComponent<ObjectActions>();
		pulled.PulledBy = playerMove.netId;
	}

	[Command]
	public void CmdStopPulling(GameObject obj)
	{
		var pulled = obj.GetComponent<ObjectActions>();
		pulled.PulledBy = NetworkInstanceId.Invalid;
	}

	[Command]
	public void CmdPushObject(GameObject obj)
	{
		var pushed = obj.GetComponent<ObjectActions>();
		pushed.TryToPush(playerMove);
	}
}
