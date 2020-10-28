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
		public GameObject arcEffectPrefab;
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

			var settings = new ElectricalArcSettings(arcEffectPrefab, startObject, endObject, startPosition, endPosition, arcCount, duration);
			new ElectricalArc().CreateArcs(settings);
		}

		/// <summary>
		/// Sends a message to all clients, informing them to create an electrical arc with the given settings.
		/// </summary>
		public static ElectricalArcMessage SendToAll(ElectricalArcSettings arcSettings)
		{
			var msg = new ElectricalArcMessage
			{
				arcEffectPrefab = arcSettings.arcEffectPrefab,
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

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);

			arcEffectPrefab = reader.ReadGameObject();
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

			writer.WriteGameObject(arcEffectPrefab);
			writer.WriteGameObject(startObject);
			writer.WriteGameObject(endObject);
			writer.WriteVector3(startPosition);
			writer.WriteVector3(endPosition);
			writer.WriteInt32(arcCount);
			writer.WriteSingle(duration);
		}
	}
}
