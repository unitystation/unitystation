using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DatabaseAPI;
using Mirror;
using UnityEngine;
using TMPro;

namespace Blob
{
	/// <summary>
	/// Class which has the logic and data for the blob player
	/// </summary>
	public class BlobPlayer : NetworkBehaviour
	{
		[SerializeField]
		private GameObject blobCorePrefab = null;
		[SerializeField]
		private GameObject blobNodePrefab = null;
		[SerializeField]
		private GameObject blobResourcePrefab = null;
		[SerializeField]
		private GameObject blobFactoryPrefab = null;
		[SerializeField]
		private GameObject blobReflectivePrefab = null;
		[SerializeField]
		private GameObject blobStrongPrefab = null;
		[SerializeField]
		private GameObject blobNormalPrefab = null;

		private GameObject blobCore;
		private Integrity coreHealth;
		private TMP_Text text;

		public int damage = 100;
		public AttackType attackType = AttackType.Melee;
		public DamageType damageType = DamageType.Brute;

		private LayerMask destroyable;
		private LayerMask walls;

		private PlayerSync playerSync;

		private ConcurrentDictionary<Vector3, GameObject> blobTiles = new ConcurrentDictionary<Vector3, GameObject>();

		[SyncVar(hook = nameof(SyncResources))]
		private int resources = 0;

		public void BlobStart()
		{
			playerSync = GetComponent<PlayerSync>();

			var result = Spawn.ServerPrefab(blobCorePrefab, playerSync.ServerPosition, gameObject.transform);

			if (!result.Successful)
			{
				Debug.LogError("Failed to spawn blob core for player!");
				return;
			}

			blobCore = result.GameObject;

			coreHealth = blobCore.GetComponent<Integrity>();

			var uiBlob = UIManager.Display.hudBottomBlob.GetComponent<UI_Blob>();
			uiBlob.blobPlayer = this;
			text = uiBlob.text;

			destroyable = LayerMask.GetMask("Furniture", "Machines", "Unshootable Machines", "Items",
				"Objects");

			walls = LayerMask.GetMask("Walls");
		}

		private void OnEnable()
		{
			UpdateManager.Add(PeriodicUpdate, 1f);
		}

		private void OnDisable()
		{
			Death();
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
		}

		private void PeriodicUpdate()
		{
			if(blobCore == null) return;

			text.text = coreHealth.integrity.ToString();
		}

		private void SyncResources(int oldVar, int newVar)
		{
			resources = newVar;
		}

		[Command]
		public bool CmdTryPlaceBlobOrAttack(Vector3 worldpos)
		{
			//TODO only allow attack or build ajacent or on blob structures

			if (TryAttack(worldpos)) return true;

			if (blobTiles.ContainsKey(worldpos))
			{
				if (blobTiles.TryGetValue(worldpos, out var blob))
				{
					return false;
				}

				var replaceResult = Spawn.ServerPrefab(blobNormalPrefab, playerSync.ServerPosition, gameObject.transform);

				if (replaceResult.Successful)
				{
					blobTiles[worldpos] = replaceResult.GameObject;
				}

				return true;
			}

			var result = Spawn.ServerPrefab(blobNormalPrefab, playerSync.ServerPosition, gameObject.transform);

			if (result.Successful)
			{
				blobTiles.TryAdd(worldpos, result.GameObject);
			}

			return true;
		}

		private bool TryAttack(Vector3 worldpos)
		{
			var hits = MouseUtils.GetOrderedObjectsAtPoint(worldpos, destroyable,
				go => go.GetComponent<CustomNetTransform>() != null && go.GetComponent<Integrity>() != null);

			if (hits.Any())
			{
				var toHit = hits.First();

				var isBlob = toHit.GetComponent<BlobStructure>();

				if (isBlob != null && hits.Count() > 1)
				{
					toHit = hits.Last();
				}
				else if (isBlob != null)
				{
					return false;
				}

				toHit.GetComponent<Integrity>().ApplyDamage(damage, attackType, damageType);
				return true;
			}

			//todo check for walls

			return false;
		}

		#region Death

		public void Death()
		{
			//todo call from core game object when it dies

			foreach (var tile in blobTiles)
			{
				if (tile.Value != null)
				{
					Despawn.ServerSingle(tile.Value);
				}
			}

			blobTiles = new ConcurrentDictionary<Vector3, GameObject>();
		}

		#endregion
	}
}