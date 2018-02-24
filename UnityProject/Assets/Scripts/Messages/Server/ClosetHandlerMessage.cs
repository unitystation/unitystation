using System.Collections;
using Cupboards;
using PlayGroup;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     This Server to Client message is sent when a player is stored inside a closet or crate.
///     It makes sure the relevant ClosetHandler is created on the client to monitor the players actions while the
///     player is being hidden inside
/// </summary>
public class ClosetHandlerMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.ClosetHandlerMessage;
	public GameObject Closet;

	public NetworkInstanceId Recipient;

	public override IEnumerator Process()
	{
		yield return WaitFor(Recipient);
		//Add the local player ClosetPlayerHandler (controls camera and interaction when closet is moving)
		//Also monitors players movement inside the cupboard and tries to break them out
		ObjectBehaviour playerBehaviour = PlayerManager.LocalPlayer.GetComponent<ObjectBehaviour>();
		playerBehaviour.closetHandlerCache = PlayerManager.LocalPlayer.AddComponent<ClosetPlayerHandler>();
		ClosetControl closetCtrl = Closet.GetComponent<ClosetControl>();
		playerBehaviour.closetHandlerCache.Init(closetCtrl);
	}

	public static ClosetHandlerMessage Send(GameObject recipient, GameObject closet)
	{
		ClosetHandlerMessage msg = new ClosetHandlerMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId,
			Closet = closet
		};
		msg.SendTo(recipient);
		return msg;
	}

	public override string ToString()
	{
		return string.Format("[ClosetHandlerMessage Recipient={0} Closet={1}]", Recipient, Closet);
	}
}