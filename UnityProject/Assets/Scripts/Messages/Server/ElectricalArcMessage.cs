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
		public Guid prefabAssetID;
		public GameObject startObject;
		public GameObject endObject;
		public Vector3 startPosition;
		public Vector3 endPosition;
		public int arcCount;
		public float duration;

		// To be run on client
		public override void Process()
		{
			if (CustomNetworkManager.IsServer) return; // Run extra logic for server, handled in ElectricalArc.

			if (!MatrixManager.IsInitialized) return;

			if (CustomNetworkManager.IsServer == false && PlayerManager.LocalPlayer == null) return;

			if (ClientScene.prefabs.TryGetValue(prefabAssetID, out var prefab))
			{
				var settings = new ElectricalArcSettings(prefab, startObject, endObject, startPosition, endPosition, arcCount, duration);
				new ElectricalArc().CreateArcs(settings);
			}
		}

		/// <summary>
		/// Sends a message to all clients, informing them to create an electrical arc with the given settings.
		/// </summary>
		public static ElectricalArcMessage SendToAll(ElectricalArcSettings arcSettings)
		{
			if (arcSettings.arcEffectPrefab.TryGetComponent<NetworkIdentity>(out var identity))
			{
				var msg = new ElectricalArcMessage
				{
					prefabAssetID = identity.assetId,
					startObject = arcSettings.startObject,
					endObject = arcSettings.endObject,
					startPosition = arcSettings.startPosition,
					endPosition = arcSettings.endPosition,
					arcCount = arcSettings.arcCount,
					duration = arcSettings.duration,
				};
				msg.SendToAll();
				return msg;
			}
			else
			{
				Logger.LogError($"No {nameof(NetworkIdentity)} found on {arcSettings.arcEffectPrefab}!", Category.NetMessage);
				return default;
			}
		}

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);

			prefabAssetID = reader.ReadGuid();
			startObject = reader.ReadGameObject();
			endObject = reader.ReadGameObject();
			startPosition = reader.ReadVector3();
			endPosition = reader.ReadVector3();
			arcCount = reader.ReadInt32();
			duration = reader.ReadSingle();
		}

		public override void Serialize(NetworkWriter writer)
		{
			base.Serialize(writer);

			writer.WriteGuid(prefabAssetID);
			writer.WriteGameObject(startObject);
			writer.WriteGameObject(endObject);
			writer.WriteVector3(startPosition);
			writer.WriteVector3(endPosition);
			writer.WriteInt32(arcCount);
			writer.WriteSingle(duration);
		}
	}
}
