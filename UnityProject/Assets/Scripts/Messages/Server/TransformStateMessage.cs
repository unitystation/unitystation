using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to change world object's transform state ((dis)appear/change posistion, rotation/start floating)
/// </summary>
public class TransformStateMessage : ServerMessage
{
	public bool ForceRefresh;
	public TransformState State;
	public uint TransformedObject;

	///To be run on client
	public override void Process()
	{
		LoadNetworkObject(TransformedObject);

		if (NetworkObject && (CustomNetworkManager.Instance._isServer || ForceRefresh))
		{
			//update NetworkObject transform state
			var transform = NetworkObject.GetComponent<CustomNetTransform>();
//				Logger.Log($"{transform.ClientState} ->\n{State}");
			transform.UpdateClientState(State);
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
	public static TransformStateMessage Send(NetworkConnection recipient, GameObject transformedObject, TransformState state,
		bool forced = true)
	{
		var msg = new TransformStateMessage
		{
			TransformedObject = transformedObject != null
				? transformedObject.GetComponent<NetworkIdentity>().netId
				: NetId.Invalid,
			State = state,
			ForceRefresh = forced
		};
		msg.SendTo(recipient);
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
	public static TransformStateMessage SendToAll(GameObject transformedObject, TransformState state,
		bool forced = true)
	{
		var msg = new TransformStateMessage
		{
			TransformedObject = transformedObject != null
				? transformedObject.GetComponent<NetworkIdentity>().netId
				: NetId.Invalid,
			State = state,
			ForceRefresh = forced
		};
		msg.SendToAll();
		return msg;
	}
}