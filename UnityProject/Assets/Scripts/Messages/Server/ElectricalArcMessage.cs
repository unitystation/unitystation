using System;
using Logs;
using Systems.ElectricalArcs;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	/// Sends a message to all clients, informing them to create an electrical arc with the given settings.
	/// </summary>
	public class ElectricalArcMessage : ServerMessage<ElectricalArcMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint prefabAssetID;
			public GameObject startObject;
			public GameObject endObject;
			public Vector3 startPosition;
			public Vector3 endPosition;
			public int arcCount;
			public float duration;
			public bool reachCheck;
			public bool addRandomness;
		}

		// To be run on client
		public override void Process(NetMessage msg)
		{
			if (CustomNetworkManager.IsServer) return; // Run extra logic for server, handled in ElectricalArc.
			if (MatrixManager.IsInitialized == false) return;
			if (PlayerManager.LocalPlayerObject == null) return;

			if (NetworkClient.prefabs.TryGetValue(msg.prefabAssetID, out var prefab) == false)
			{
				Loggy.LogError(
						$"Couldn't spawn {nameof(ElectricalArc)}; client doesn't know about this {nameof(msg.prefabAssetID)}: {msg.prefabAssetID}.",
						Category.Firearms);
				return;
			}

			var settings = new ElectricalArcSettings(prefab, msg.startObject, msg.endObject,
				msg.startPosition, msg.endPosition, msg.arcCount,
				msg.duration, msg.reachCheck, msg.addRandomness);
			new ElectricalArc().CreateArcs(settings);
		}

		/// <summary>
		/// Sends a message to all clients, informing them to create an electrical arc with the given settings.
		/// </summary>
		public static NetMessage SendToAll(ElectricalArcSettings arcSettings)
		{
			if (arcSettings.arcEffectPrefab.TryGetComponent<NetworkIdentity>(out var identity) == false)
			{
				Loggy.LogError(
						$"No {nameof(NetworkIdentity)} found on {arcSettings.arcEffectPrefab}!",
						Category.Electrical);
				return default;
			}

			var msg = new NetMessage
			{
				prefabAssetID = identity.assetId,
				startObject = arcSettings.startObject,
				endObject = arcSettings.endObject,
				startPosition = arcSettings.startPosition,
				endPosition = arcSettings.endPosition,
				arcCount = arcSettings.arcCount,
				duration = arcSettings.duration,
				reachCheck = arcSettings.reachCheck,
				addRandomness = arcSettings.addRandomness
			};

			SendToAll(msg);
			return msg;
		}
	}
}
