using PlayGroup;
using UnityEngine;
using UnityEngine.Networking;

public partial class PlayerNetworkActions : NetworkBehaviour
{
	[HideInInspector] public bool isPulling;

	[Command]
	public void CmdPullObject(GameObject obj)
	{
		if (isPulling)
		{
			GameObject cObj = gameObject.GetComponent<IPlayerSync>().PullingObject;
			cObj.GetComponent<PushPull>().pulledBy = null;
			gameObject.GetComponent<IPlayerSync>().PullObjectID = NetworkInstanceId.Invalid;
		}

		PushPull pulled = obj.GetComponent<PushPull>();
		//Stop CNT as the transform of the pulled obj is now handled by PlayerSync
		pulled.RpcToggleCnt(false);
		//Cache value for new players
		pulled.custNetActiveState = false;
		//check if the object you want to pull is another player
		if (pulled.isPlayer)
		{
			IPlayerSync playerS = obj.GetComponent<IPlayerSync>();
			//Anything that the other player is pulling should be stopped
			if (playerS.PullingObject != null)
			{
				PlayerNetworkActions otherPNA = obj.GetComponent<PlayerNetworkActions>();
				otherPNA.CmdStopOtherPulling(playerS.PullingObject);
			}
		}
		//Other player is pulling object, send stop on that player
		if (pulled.pulledBy != null)
		{
			if (pulled.pulledBy != gameObject)
			{
				pulled.GetComponent<PlayerNetworkActions>().CmdStopPulling(obj);
			}
		}

		if (pulled != null)
		{
			IPlayerSync pS = GetComponent<IPlayerSync>();
			pS.PullObjectID = pulled.netId;
			isPulling = true;
		}
	}

	//if two people try to pull the same object
	[Command]
	public void CmdStopOtherPulling(GameObject obj)
	{
		PushPull objA = obj.GetComponent<PushPull>();
		objA.custNetActiveState = true;
		if (objA.pulledBy != null)
		{
			objA.pulledBy.GetComponent<PlayerNetworkActions>().CmdStopPulling(obj);
		}
		var netTransform = obj.GetComponent<CustomNetTransform>();
		if (netTransform != null) {
			netTransform.SetPosition(obj.transform.localPosition);
		}
	}

	[Command]
	public void CmdStopPulling(GameObject obj)
	{
		if (!isPulling)
		{
			return;
		}

		isPulling = false;
		PushPull pulled = obj.GetComponent<PushPull>();
		pulled.RpcToggleCnt(true);
		//Cache value for new players
		pulled.custNetActiveState = true;
		if (pulled != null)
		{
			//			//this triggers currentPos syncvar hook to make sure registertile is been completed on all clients
			//			pulled.currentPos = pulled.transform.position;

			IPlayerSync pS = gameObject.GetComponent<IPlayerSync>();
			pS.PullObjectID = NetworkInstanceId.Invalid;
			pulled.pulledBy = null;
		}
		var netTransform = obj.GetComponent<CustomNetTransform>();
		if (netTransform != null) {
			netTransform.SetPosition(obj.transform.localPosition);
		}
	}

	[Command]
	public void CmdTryPush(GameObject obj, Vector3 startLocalPos, Vector3 targetPos, float speed)
	{
		PushPull pushed = obj.GetComponent<PushPull>();
		if (pushed != null)
		{
			var netTransform = obj.GetComponent<CustomNetTransform>();
			netTransform.PushTo(targetPos, playerSprites.currentDirection.Vector, true, speed, true);
		}
	}
}