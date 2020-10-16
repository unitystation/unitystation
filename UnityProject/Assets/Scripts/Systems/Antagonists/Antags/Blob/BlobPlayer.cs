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

		[SerializeField]
		private GameObject attackEffect = null;

		private GameObject blobCore;
		private Integrity coreHealth;
		private TMP_Text text;

		public int damage = 100;
		public AttackType attackType = AttackType.Melee;
		public DamageType damageType = DamageType.Brute;

		private PlayerSync playerSync;
		private RegisterPlayer registerPlayer;

		public bool clickCoords = true;

		private ConcurrentDictionary<Vector3Int, GameObject> blobTiles = new ConcurrentDictionary<Vector3Int, GameObject>();

		private List<Vector3Int> coords = new List<Vector3Int>
		{
			new Vector3Int(0, 0, 0),
			new Vector3Int(0, 1, 0),
			new Vector3Int(1, 0, 0),
			new Vector3Int(0, -1, 0),
			new Vector3Int(-1, 0, 0)
		};

		[SyncVar(hook = nameof(SyncResources))]
		private int resources = 0;

		public void BlobStart()
		{
			playerSync = GetComponent<PlayerSync>();
			registerPlayer = GetComponent<RegisterPlayer>();

			var result = Spawn.ServerPrefab(blobCorePrefab, playerSync.ServerPosition, gameObject.transform);

			if (!result.Successful)
			{
				Debug.LogError("Failed to spawn blob core for player!");
				return;
			}

			blobCore = result.GameObject;

			blobTiles.TryAdd(blobCore.GetComponent<CustomNetTransform>().ServerPosition, blobCore);

			coreHealth = blobCore.GetComponent<Integrity>();

			var uiBlob = UIManager.Display.hudBottomBlob.GetComponent<UI_Blob>();
			uiBlob.blobPlayer = this;
			text = uiBlob.text;
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
		public void CmdTryPlaceBlobOrAttack(Vector3Int worldPos)
		{
			worldPos.z = 0;

			//TODO only allow attack or build adjacent or on blob structures

			if(!ValidateAction(worldPos)) return;

			if (!clickCoords)
			{
				worldPos = playerSync.ServerPosition;
			}

			if (TryAttack(worldPos)) return;

			if (blobTiles.ContainsKey(worldPos))
			{
				if (blobTiles.TryGetValue(worldPos, out var blob))
				{
					return;
				}

				var replaceResult = Spawn.ServerPrefab(blobNormalPrefab, worldPos, gameObject.transform);

				if (replaceResult.Successful)
				{
					blobTiles[worldPos] = replaceResult.GameObject;
				}

				return;
			}

			var result = Spawn.ServerPrefab(blobNormalPrefab, worldPos, gameObject.transform);

			if (result.Successful)
			{
				blobTiles.TryAdd(worldPos, result.GameObject);
			}

			return;
		}

		private bool TryAttack(Vector3 worldPos)
		{
			var matrix = registerPlayer.Matrix;

			if (matrix == null)
			{
				Debug.LogError("matrix for blob click was null");
				return false;
			}

			var metaTileMap = matrix.MetaTileMap;

			var pos = worldPos.RoundToInt();

			var local = worldPos.ToLocalInt(matrix);

			var players = matrix.Get<LivingHealthBehaviour>(local, ObjectType.Player, true);

			if (players.Any())
			{
				players.First().ApplyDamage(gameObject, damage, attackType, damageType);
				PlayAttackEffect(pos);

				return true;
			}

			var hits = matrix.Get<RegisterTile>(local, ObjectType.Object, true);

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

				//Try damage NPC
				if (toHit.TryGetComponent<LivingHealthBehaviour>(out var npcComponent))
				{
					npcComponent.ApplyDamage(gameObject, damage, attackType, damageType);

					PlayAttackEffect(pos);

					return true;
				}

				//Dont bother destroying passable stuff, eg open door
				if (toHit.IsPassable(true))
				{
					return false;
				}

				if (toHit.TryGetComponent<Integrity>(out var component) && !component.Resistances.Indestructable)
				{
					component.ApplyDamage(damage, attackType, damageType);

					PlayAttackEffect(pos);

					return true;
				}

				return false;
			}

			//Check for walls, windows and grills
			if (metaTileMap != null && !MatrixManager.IsPassableAt(pos, true))
			{
				//Cell pos is unused var
				metaTileMap.ApplyDamage(Vector3Int.zero, damage,
					pos, attackType);

				PlayAttackEffect(pos);

				return true;
			}

			return false;
		}

		private bool ValidateAction(Vector3 worldPos)
		{
			var pos = worldPos.RoundToInt();

			foreach (var offSet in coords)
			{
				if (blobTiles.ContainsKey(pos + offSet))
				{
					return true;
				}
			}

			//No adjacent blobs, therefore cannot attack or expand
			return false;
		}

		private void PlayAttackEffect(Vector3 worldPos)
		{
			var result = Spawn.ServerPrefab(attackEffect, worldPos, gameObject.transform);

			if(!result.Successful) return;

			StartCoroutine(DespawnEffect(result.GameObject));
		}

		private IEnumerator DespawnEffect(GameObject effect)
		{
			yield return WaitFor.Seconds(2f);

			Despawn.ServerSingle(effect);
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

			blobTiles = new ConcurrentDictionary<Vector3Int, GameObject>();
		}

		#endregion
	}
}