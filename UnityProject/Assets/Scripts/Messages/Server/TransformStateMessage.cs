using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to change world object's transform state ((dis)appear/change posistion, rotation/start floating)
/// </summary>
public class TransformStateMessage : ServerMessage
{
	public class TransformStateMessageNetMessage : NetworkMessage
	{
		public bool ForceRefresh;
		public TransformState State;
		public uint TransformedObject;
	}

	///To be run on client
	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as TransformStateMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadNetworkObject(newMsg.TransformedObject);

		if (NetworkObject && (CustomNetworkManager.Instance._isServer || newMsg.ForceRefresh))
		{
			//update NetworkObject transform state
			var transform = NetworkObject.GetComponent<CustomNetTransform>();
//				Logger.Log($"{transform.ClientState} ->\n{State}");
			transform.UpdateClientState(newMsg.State);
		}
	}

	/// <summary>
	/// Send the TransformStateMessage to a specific client.
	/// </summary>
	/// <param name="recipient">Recipient to receive the message</param>
	/// <param name="transformedObject">The object to apply the transformation</param>
	/// <param name="state">The transformation to apply</param>
	/// <param name="forced">
	///     Used for client simulation, use false if already updated by prediction
	///     (to avoid updating it twice)
	/// </param>
	/// <returns>The sent message</returns>
	public static TransformStateMessageNetMessage Send(NetworkConnection recipient, GameObject transformedObject, TransformState state,
		bool forced = true)
	{
		var msg = new TransformStateMessageNetMessage
		{
			TransformedObject = transformedObject != null
				? transformedObject.GetComponent<NetworkIdentity>().netId
				: NetId.Invalid,
			State = state,
			ForceRefresh = forced
		};
		new TransformStateMessage().SendTo(recipient, msg);
		return msg;
	}

	/// <summary>
	/// Send the TransformStateMessage to a all client.
	/// </summary>
	/// <param name="transformedObject">The object to apply the transformation</param>
	/// <param name="state">The transformation to apply</param>
	/// <param name="forced">
	///     Used for client simulation, use false if already updated by prediction
	///     (to avoid updating it twice)
	/// </param>
	/// <returns>The sent message</returns>
	public static TransformStateMessageNetMessage SendToAll(GameObject transformedObject, TransformState state,
		bool forced = true)
	{
		var msg = new TransformStateMessageNetMessage
		{
			TransformedObject = transformedObject != null
				? transformedObject.GetComponent<NetworkIdentity>().netId
				: NetId.Invalid,
			State = state,
			ForceRefresh = forced
		};
		new TransformStateMessage().SendToAll(msg);
		return msg;
	}
}