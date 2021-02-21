using System;
using System.Collections;
using UnityEngine;
using Mirror;

namespace Systems.ElectricalArcs
{
	/// <summary>
	/// Sends a message to all clients, informing them to create an electrical arc with the given settings.
	/// </summary>
	public class ElectricalArcMessage : ServerMessage
	{
		public class ElectricalArcMessageNetMessage : NetworkMessage
		{
			public Guid prefabAssetID;
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
		public override void Process<T>(T msg)
		{
			var newMsg = msg as ElectricalArcMessageNetMessage;
			if(newMsg == null) return;

			if (CustomNetworkManager.IsServer) return; // Run extra logic for server, handled in ElectricalArc.
			if (MatrixManager.IsInitialized == false) return;
			if (PlayerManager.LocalPlayer == null) return;

			if (ClientScene.prefabs.TryGetValue(newMsg.prefabAssetID, out var prefab) == false)
			{
				Logger.LogError(
						$"Couldn't spawn {nameof(ElectricalArc)}; client doesn't know about this {nameof(newMsg.prefabAssetID)}: {newMsg.prefabAssetID}.",
						Category.Firearms);
				return;
			}

			var settings = new ElectricalArcSettings(prefab, newMsg.startObject, newMsg.endObject,
				newMsg.startPosition, newMsg.endPosition, newMsg.arcCount,
				newMsg.duration, newMsg.reachCheck, newMsg.addRandomness);
			new ElectricalArc().CreateArcs(settings);
		}

		/// <summary>
		/// Sends a message to all clients, informing them to create an electrical arc with the given settings.
		/// </summary>
		public static ElectricalArcMessageNetMessage SendToAll(ElectricalArcSettings arcSettings)
		{
			if (arcSettings.arcEffectPrefab.TryGetComponent<NetworkIdentity>(out var identity) == false)
			{
				Logger.LogError(
						$"No {nameof(NetworkIdentity)} found on {arcSettings.arcEffectPrefab}!",
						Category.NetMessage);
				return default;
			}

			var msg = new ElectricalArcMessageNetMessage
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
			new ElectricalArcMessage().SendToAll(msg);
			return msg;
		}
	}
}
