using System;
using Logs;
using UnityEngine;
using Mirror;
using Objects.Atmospherics;
using Object = UnityEngine.Object;


namespace Messages.Client.DevSpawner
{
	/// <summary>
	/// Message allowing a client dev / admin to spawn something, validated server side.
	/// </summary>
	public class DevSpawnMessage : ClientMessage<DevSpawnMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			// asset ID of the prefab to spawn
			public uint PrefabAssetID;
			// position to spawn at.
			public Vector3 LocalPosition;

			public int MatrixID;

			//If a stackable item how many Should be in the stack
			public int SpawnStackAmount;

			public OrientationEnum OrientationEnum;

			public bool HasOrientationEnum;

			public bool Mapped;

			public override string ToString()
			{
				if (HasOrientationEnum)
				{
					return $"[DevSpawnMessage PrefabAssetID={PrefabAssetID} LocalPosition={LocalPosition} MatrixID={MatrixID} Amount={SpawnStackAmount} Orientation={OrientationEnum}]";
				}
				else
				{
					return $"[DevSpawnMessage PrefabAssetID={PrefabAssetID} LocalPosition={LocalPosition} MatrixID={MatrixID} Amount={SpawnStackAmount}]";
				}
			}
		}

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		private void ValidateAdmin(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			//no longer checks impassability, spawn anywhere, go hog wild.
			if (NetworkClient.prefabs.TryGetValue(msg.PrefabAssetID, out var prefab))
			{
				var Matrix = MatrixManager.Get(msg.MatrixID);
				var worldPosition = msg.LocalPosition.ToWorld(Matrix);
				var game = Spawn.ServerPrefab(prefab, worldPosition).GameObject;

				if (msg.Mapped)
				{
					var NonMapped = game.gameObject.GetComponent<RuntimeSpawned>();
					if (NonMapped != null)
					{
						Object.Destroy(NonMapped);
					}
				}

				if (game.TryGetComponent<Stackable>(out var Stackable) && msg.SpawnStackAmount != -1)
				{
					Stackable.ServerSetAmount(msg.SpawnStackAmount);
				}

				if (game.TryGetComponent<Rotatable>(out var Rotatable) && msg.HasOrientationEnum)
				{
					Rotatable.FaceDirection(msg.OrientationEnum);
				}

				UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(
					$"{SentByPlayer.Username} spawned a {prefab.name} at {worldPosition}", SentByPlayer.AccountId);
			}
			else
			{
				Loggy.LogWarningFormat("An admin attempted to spawn prefab with invalid asset ID {0}, which" +
				                        " is not found in Mirror.ClientScene. Spawn will not" +
				                        " occur.", Category.Admin, msg.PrefabAssetID);
			}
		}

		/// <summary>
		/// Ask the server to spawn a specific prefab
		/// </summary>
		/// <param name="prefab">prefab to instantiate, must be networked (have networkidentity)</param>
		/// <param name="worldPosition">world position to spawn it at</param>
		/// <param name="adminId">user id of the admin trying to perform this action</param>
		/// <param name="adminToken">token of the admin trying to perform this action</param>
		/// <returns></returns>
		public static void Send(GameObject prefab, Vector3 worldPosition, int InSpawnStackAmount,OrientationEnum? OrientationEnum, bool Mapped)
		{
			if (prefab.TryGetComponent<NetworkIdentity>(out var networkIdentity))
			{
				NetMessage msg = new NetMessage
				{
					PrefabAssetID = networkIdentity.assetId,
					LocalPosition = worldPosition.ToLocal(),
					MatrixID = worldPosition.GetMatrixAtWorld().Id,
					SpawnStackAmount = InSpawnStackAmount,
					HasOrientationEnum = OrientationEnum != null,
					Mapped = Mapped
				};

				if (msg.HasOrientationEnum)
				{
					msg.OrientationEnum = OrientationEnum.Value;
				}

				Send(msg);
			}
			else
			{
				Loggy.LogWarningFormat(
						"Prefab {0} which you are attempting to spawn has no NetworkIdentity, thus cannot be spawned.",
						Category.Admin, prefab);
			}
		}
	}
}
