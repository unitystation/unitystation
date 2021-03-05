using System.Collections;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	///     Tells client to change world object's transform state ((dis)appear/change posistion, rotation/start floating)
	/// </summary>
	public class TransformStateMessage : ServerMessage<TransformStateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public bool ForceRefresh;
			public TransformState State;
			public uint TransformedObject;
		}

		///To be run on client
		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.TransformedObject);

			if (NetworkObject && (CustomNetworkManager.Instance._isServer || msg.ForceRefresh))
			{
				//update NetworkObject transform state
				var transform = NetworkObject.GetComponent<CustomNetTransform>();
//				Logger.Log($"{transform.ClientState} ->\n{State}");
				transform.UpdateClientState(msg.State);
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
		public static NetMessage Send(NetworkConnection recipient, GameObject transformedObject, TransformState state,
			bool forced = true)
		{
			var msg = new NetMessage
			{
				TransformedObject = transformedObject != null
					? transformedObject.GetComponent<NetworkIdentity>().netId
					: NetId.Invalid,
				State = state,
				ForceRefresh = forced
			};

			SendTo(recipient, msg);
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
		public static void SendToAll(GameObject transformedObject, TransformState state,
			bool forced = true)
		{
			var id = NetId.Invalid;

			if (transformedObject != null && transformedObject.TryGetComponent<NetworkIdentity>(out var networkIdentity))
			{
				if (networkIdentity.netId == 0)
				{
					//netIds default to 0 when spawned, a new Id is assigned but this happens a bit later
					//this is just to catch multiple 0's
					//An identity could have a valid id of 0, but since this message is only for net transforms and since the
					//identities on the managers will get set first, this shouldn't cause any issues.
					Debug.LogError($"{transformedObject.name} still has netId of 0, even after the wait");
					return;
				}

				id = networkIdentity.netId;
			}

			var msg = new NetMessage
			{
				TransformedObject = id,
				State = state,
				ForceRefresh = forced
			};

			SendToAll(msg);
			return;
		}
	}
}