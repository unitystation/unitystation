using System.Collections;
using UI;
using UnityEngine;
using UnityEngine.Networking;
/// <summary>
/// Tells client to make world object disappear or appear at some position
/// </summary>
public class TransformStateMessage : ServerMessage<TransformStateMessage>
{
	public NetworkInstanceId TransformedObject;
	public TransformState State;
	public bool ForceRefresh;

	///To be run on client
	public override IEnumerator Process()
	{
		//        Debug.Log("Processed " + ToString());
		if (TransformedObject == NetworkInstanceId.Invalid) {
			//Doesn't make any sense
			yield return null;
		} else {
			yield return WaitFor(TransformedObject);
			if (CustomNetworkManager.Instance._isServer || ForceRefresh)
			{
				//update NetworkObject transformedObject state
				var transform = NetworkObject.GetComponent<CustomNetTransform>();
				if ( State.Active )
				{
					transform.AppearAtPosition(State.Position);
				}
				else
				{
					transform.DisappearFromWorld();
				}
			}
		}
	}
	
		public static TransformStateMessage Send(GameObject recipient, GameObject transformedObject, TransformState state, bool forced = true)
	{
		var msg = new TransformStateMessage {
			TransformedObject = (transformedObject != null) ?
				transformedObject.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			State = state,
			ForceRefresh = forced
		};
		msg.SendTo(recipient);
		return msg;
	}

	/// <param name="transformedObject">object to hide</param>
	/// <param name="state"></param>
	/// <param name="forced">Used for client simulation, use false if already updated by prediction
	///     (to avoid updating it twice)
	/// </param>
	public static TransformStateMessage SendToAll(GameObject transformedObject, TransformState state, bool forced = true)
	{
		var msg = new TransformStateMessage {
			TransformedObject = (transformedObject != null) ?
				transformedObject.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			State = state,
			ForceRefresh = forced
		};
		msg.SendToAll();
		return msg;
	}

	public override string ToString()
	{
		return
			$"[TransformStateMessage Parameter={TransformedObject} Pos={State.Position} Active={State.Active} Type={MessageType} Forced={ForceRefresh}]";
	}
}
