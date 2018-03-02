using System.Collections;
using PlayGroup;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to 
/// </summary>
public class PlayerMoveMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.PlayerMoveMessage;
	public PlayerState State;
	public NetworkInstanceId Recipient;

	///To be run on client
	public override IEnumerator Process()
	{
//		Debug.Log("Processed " + ToString());
		yield return WaitFor(Recipient);
		NetworkObject.GetComponent<PlayerSync>().UpdateClientState(State);
	}

	public static PlayerMoveMessage Send(GameObject recipient, PlayerState state)
	{
		var msg = new PlayerMoveMessage
		{
			Recipient = recipient != null ? recipient.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			State = state,
		};
		msg.SendTo(recipient);
		return msg;
	}

	public static PlayerMoveMessage SendToAll(GameObject recipient, PlayerState state)
	{
		var msg = new PlayerMoveMessage
		{
			Recipient = recipient != null ? recipient.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			State = state,
		};
		msg.SendToAll();
		return msg;
	}

	public override string ToString()
	{
		return $"[PlayerMoveMessage State={State} Recip={Recipient}]";
	}
}