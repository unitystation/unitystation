using System;
using System.Linq;
using Core.Admin.Logs;
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
			public bool AltInteraction;

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
			if (HasPermission(TAG.MAP_SPAWN) == false) return;

			//no longer checks impassability, spawn anywhere, go hog wild.
			if (NetworkClient.prefabs.TryGetValue(msg.PrefabAssetID, out var prefab))
			{
				bool StackableBool = false;
				if (prefab.TryGetComponent<Stackable>(out var prefabStackable))
				{
					StackableBool = true;
				}
				else
				{
					if (msg.SpawnStackAmount != -1)
					{
						if (msg.SpawnStackAmount > 10) //Stop spawning a lot
						{
							msg.SpawnStackAmount = 10;
						}
					}

					StackableBool = false;
				}

				var Matrix = MatrixManager.Get(msg.MatrixID);
				var Dynamicstorage = Matrix.Matrix.Get<DynamicItemStorage>(msg.LocalPosition.RoundToInt(), true);
				var ItemStorage = Matrix.Matrix.Get<ItemStorage>(msg.LocalPosition.RoundToInt(), true);
				var worldPosition = msg.LocalPosition.ToWorld(Matrix);

				SpawnResult SpawnResult = null;
				if (StackableBool || msg.SpawnStackAmount == -1)
				{
					SpawnResult = Spawn.ServerPrefab(prefab, worldPosition);
				}
				else
				{
					SpawnResult = Spawn.ServerPrefab(prefab, worldPosition, count:msg.SpawnStackAmount);
				}


				foreach (var game in SpawnResult.GameObjects)
				{
					if (msg.Mapped)
					{
						var NonMapped = game.gameObject.GetComponent<RuntimeSpawned>();
						if (NonMapped != null)
						{
							Object.Destroy(NonMapped);
						}
					}

					if (msg.SpawnStackAmount != -1)
					{
						if (game.TryGetComponent<Stackable>(out var Stackable) )
						{
							Stackable.ServerSetAmount(msg.SpawnStackAmount);
						}
					}


					if (game.TryGetComponent<Rotatable>(out var Rotatable) && msg.HasOrientationEnum)
					{
						Rotatable.FaceDirection(msg.OrientationEnum);
					}

					if (msg.AltInteraction)
					{
						if (Dynamicstorage?.Any() == true)
						{
							var Slot = Dynamicstorage.First().GetBestHandOrSlotFor(game);
							if (Slot != null)
							{
								Inventory.ServerAdd(game, Slot);
							}
						}
						else if (ItemStorage?.Any() == true)
						{
							var Slot = ItemStorage.First().GetBestSlotFor(game);
							if (Slot != null)
							{
								Inventory.ServerAdd(game, Slot);
							}
						}
					}
				}


				AdminLogsManager.AddNewLog(SentByPlayer.GameObject, $" spawned a ", prefab ,$" at {worldPosition}  ", LogCategory.Admin);
			}
			else
			{
				Loggy.Warning().Format("An admin attempted to spawn prefab with invalid asset ID {0}, which" +
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
		public static void Send(GameObject prefab, Vector3 worldPosition, int InSpawnStackAmount,OrientationEnum? OrientationEnum, bool Mapped,
			bool altInteraction)
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
					Mapped = Mapped,
					AltInteraction = altInteraction,
				};

				if (msg.HasOrientationEnum)
				{
					msg.OrientationEnum = OrientationEnum.Value;
				}

				Send(msg);
			}
			else
			{
				Loggy.Warning().Format(
						"Prefab {0} which you are attempting to spawn has no NetworkIdentity, thus cannot be spawned.",
						Category.Admin, prefab);
			}
		}
	}
}
