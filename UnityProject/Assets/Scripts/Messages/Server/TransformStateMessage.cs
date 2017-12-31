using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to change world object's transform state ((dis)appear/change pos/start floating)
/// </summary>
public class TransformStateMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.TransformStateMessage;
	public bool ForceRefresh;
	public TransformState State;
	public NetworkInstanceId TransformedObject;

	///To be run on client
	public override IEnumerator Process()
	{
//		Debug.Log("Processed " + ToString());
		if (TransformedObject == NetworkInstanceId.Invalid)
		{
			//Doesn't make any sense
			yield return null;
		}
		else
		{
			yield return WaitFor(TransformedObject);
			if (CustomNetworkManager.Instance._isServer || ForceRefresh)
			{
				//update NetworkObject transform state
				var transform = NetworkObject.GetComponent<CustomNetTransform>();
				transform.UpdateClientState(State);
			}
		}
	}

	public static TransformStateMessage Send(GameObject recipient, GameObject transformedObject, TransformState state, bool forced = true)
	{
		var msg = new TransformStateMessage
		{
			TransformedObject = transformedObject != null ? transformedObject.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			State = state,
			ForceRefresh = forced
		};
		msg.SendTo(recipient);
		return msg;
	}

	/// <param name="transformedObject">object to hide</param>
	/// <param name="state"></param>
	/// <param name="forced">
	///     Used for client simulation, use false if already updated by prediction
	///     (to avoid updating it twice)
	/// </param>
	public static TransformStateMessage SendToAll(GameObject transformedObject, TransformState state, bool forced = true)
	{
		var msg = new TransformStateMessage
		{
			TransformedObject = transformedObject != null ? transformedObject.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			State = state,
			ForceRefresh = forced
		};
		msg.SendToAll();
		return msg;
	}

	public override string ToString()
	{
		return
			$"[TransformStateMessage Parameter={TransformedObject} Active={State.Active} WorldPos={State.position} localPos={State.localPos} " +
			$"Spd={State.Speed} Imp={State.Impulse} Type={MessageType} Forced={ForceRefresh}]";
	}
}