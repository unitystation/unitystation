using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
/// Message that allows the server to broadcast an event to the client
/// </summary>
public class TriggerEventMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.TriggerEvent;

	public EVENT EventType;
	//mirrorworkaround: Temporary workaround for Mirror issue https://github.com/vis2k/Mirror/issues/962
	//please remove when mirror release next version with this fix to asset store and we upgrade.
	public uint SetLocal;
	public uint UnsetLocal;

	public override IEnumerator Process()
	{
		yield return WaitFor(UnsetLocal, SetLocal);
		if (NetworkObjects[0] != null)
		{
			var netId = NetworkObjects[0].GetComponent<NetworkIdentity>();
			netId.UnsetLocal();
		}
		if (NetworkObjects[1] != null)
		{
			var netId = NetworkObjects[1].GetComponent<NetworkIdentity>();
			netId.SetLocal();
		}

		TriggerEvent();
	}

	/// Raise the specified event
	private void TriggerEvent()
	{
		EventManager.Broadcast(EventType);
	}
	/// <summary>
	/// Sends the event message to the player.
	/// </summary>
	/// <param name="recipient"></param>
	/// <param name="eventType"></param>
	/// /// <param name="unsetLocal">if specified, client will call UnsetLocal on this object so they think they are no longer in control of it.</param>
	/// <param name="setLocal">if specified, client will call SetLocal on this object so they think they are in control of it.</param>
	/// <returns></returns>
	public static TriggerEventMessage Send(GameObject recipient, EVENT eventType, GameObject unsetLocal = null, GameObject setLocal =  null)
	{
		TriggerEventMessage msg = new TriggerEventMessage();
		msg.EventType = eventType;
		msg.SetLocal = setLocal == null ?  NetId.Invalid : setLocal.NetId();
		msg.UnsetLocal = unsetLocal == null ?  NetId.Invalid : unsetLocal.NetId();
		msg.SendTo(recipient);
		return msg;
	}
}