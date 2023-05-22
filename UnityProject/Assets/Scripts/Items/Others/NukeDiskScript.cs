using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;
using Mirror;
using TileManagement;
using Random = UnityEngine.Random;

namespace Items.Command
{
	public class NukeDiskScript : NetworkBehaviour, IServerSpawn
	{
		[SerializeField]
		private float boundRadius = 600;
		private Pickupable pick;
		private UniversalObjectPhysics ObjectPhysics;
		private RegisterTile registerTile;
		private BetterBoundsInt bound;
		private EscapeShuttle escapeShuttle;

		private float timeCheckDiskLocation = 5.0f;

		private bool boundsConfigured = false;

		/// <summary>
		/// Pinpointers wont find this.
		/// </summary>
		public bool secondaryNukeDisk;

		/// <summary>
		/// Stops the disk from teleporting if moved off station matrix.
		/// </summary>
		public bool stopAutoTeleport;

		private void Awake()
		{
			ObjectPhysics = GetComponent<UniversalObjectPhysics>();
			registerTile = GetComponent<RegisterTile>();
			pick = GetComponent<Pickupable>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			bound = MatrixManager.MainStationMatrix.LocalBounds;
			escapeShuttle = FindObjectOfType<EscapeShuttle>();
			boundsConfigured = true;
		}

		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Add(ServerPeriodicUpdate, timeCheckDiskLocation);
			}
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ServerPeriodicUpdate);
			}
		}

		protected virtual void ServerPeriodicUpdate()
		{
			if (!boundsConfigured) return;
			if (stopAutoTeleport) return;

			if (DiskLost())
			{
				Teleport();
			}
		}

		private bool DiskLost()
		{
			if (((gameObject.AssumedWorldPosServer() - MatrixManager.MainStationMatrix.GameObject.transform.position)
				.magnitude < boundRadius)) return false;

			if (escapeShuttle != null && escapeShuttle.Status != EscapeShuttleStatus.DockedCentcom)
			{
				var matrixInfo = escapeShuttle.MatrixInfo;
				if (matrixInfo == null || matrixInfo.LocalBounds.Contains(registerTile.LocalPosition))
				{
					return false;
				}
			}
			else
			{
				ItemSlot slot = pick.ItemSlot;
				if (slot == null)
				{
					return true;
				}

				RegisterPlayer player = slot.Player;
				if (player == null)
				{
					return true;
				}

				if (player.GetComponent<PlayerHealthV2>().IsDead)
				{
					return true;
				}

				var checkPlayer = PlayerList.Instance.Get(player.gameObject);
				if (checkPlayer.Equals(PlayerInfo.Invalid))
				{
					return true;
				}

				if (PlayerList.Instance.AntagPlayers.Contains(checkPlayer) == false)
				{
					return true;
				}

			}
			return false;
		}

		private void Teleport()
		{
			Vector3 position = new Vector3(Random.Range(bound.xMin, bound.xMax), Random.Range(bound.yMin, bound.yMax), 0);
			while (MatrixManager.IsSpaceAt(Vector3Int.FloorToInt(position), true, registerTile.Matrix.MatrixInfo) || MatrixManager.IsWallAt(Vector3Int.FloorToInt(position), true))
			{
				position = new Vector3(Random.Range(bound.xMin, bound.xMax), Random.Range(bound.yMin, bound.yMax), 0);
			}

			if (pick.ItemSlot?.Player is not null)
			{
				Chat.AddExamineMsg(pick.ItemSlot.Player.PlayerScript.gameObject, "You feel a sudden tingling sensation in your pocket, " +
				                             "and as you reach inside, you realize that the Nuclear Authentication Disk " +
				                             "has vanished into thin air. The unmistakable hum of bluespace technology echoes in your ears, " +
				                             "indicating that it has been teleported away to an unknown location");
			}
			else
			{
				Chat.AddExamineMsg(gameObject,
					"The range-activated bluespace retrieval system triggers, whisking away the Nuclear Authentication Disk!");
			}

			if (pick?.ItemSlot != null)
			{
				Inventory.ServerDrop(pick.ItemSlot);
				pick.RefreshUISlotImage();
			}
			ObjectPhysics.AppearAtWorldPositionServer(position.ToWorld(registerTile.Matrix));
		}
	}
}
