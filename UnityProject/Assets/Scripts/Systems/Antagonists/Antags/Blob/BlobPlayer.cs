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
		[SerializeField] private GameObject blobCorePrefab = null;
		[SerializeField] private GameObject blobNodePrefab = null;
		[SerializeField] private GameObject blobResourcePrefab = null;
		[SerializeField] private GameObject blobFactoryPrefab = null;
		[SerializeField] private GameObject blobReflectivePrefab = null;
		[SerializeField] private GameObject blobStrongPrefab = null;
		[SerializeField] private GameObject blobNormalPrefab = null;

		[SerializeField] private GameObject attackEffect = null;

		[SerializeField] private GameObject blobSpore = null;

		[SerializeField] private int normalBlobCost = 4;
		[SerializeField] private int strongBlobCost = 15;
		[SerializeField] private int reflectiveBlobCost = 15;
		[SerializeField] private int resourceBlobCost = 40;
		[SerializeField] private int nodeBlobCost = 50;
		[SerializeField] private int factoryBlobCost = 60;
		[SerializeField] private int moveCoreCost = 80;

		private int attackCost = 1;

		private float refundPercentage = 0.4f;

		public BlobVariants blobVariants;

		private GameObject blobCore;
		private Integrity coreHealth;
		private TMP_Text healthText;
		private TMP_Text resourceText;
		private TMP_Text numOfBlobTilesText;

		public int damage = 100;
		public AttackType attackType = AttackType.Melee;
		public DamageType damageType = DamageType.Brute;

		private PlayerSync playerSync;
		private RegisterPlayer registerPlayer;
		private int numOfTilesForVictory = 400;
		private int numOfTilesForDetection = 75;

		private bool teleportCheck;
		private bool victory;
		private bool announcedBlob;
		private float announceAfterSeconds = 600f;

		[HideInInspector] public bool BlobRemoveMode;

		private float timer = 0f;
		private float econTimer = 0f;
		private float factoryTimer = 0f;

		private float econModifier = 1f;

		public bool clickCoords = true;

		private ConcurrentDictionary<Vector3Int, GameObject> blobTiles =
			new ConcurrentDictionary<Vector3Int, GameObject>();

		private HashSet<GameObject> resourceBlobs = new HashSet<GameObject>();

		private ConcurrentDictionary<GameObject, HashSet<GameObject>> factoryBlobs =
			new ConcurrentDictionary<GameObject, HashSet<GameObject>>();

		private HashSet<GameObject> nodeBlobs = new HashSet<GameObject>();

		private List<Vector3Int> coords = new List<Vector3Int>
		{
			new Vector3Int(0, 0, 0),
			new Vector3Int(0, 1, 0),
			new Vector3Int(1, 0, 0),
			new Vector3Int(0, -1, 0),
			new Vector3Int(-1, 0, 0)
		};

		[SyncVar(hook = nameof(SyncResources))]
		private int resources = 20;

		[SyncVar(hook = nameof(SyncHealth))] private float health = 20;

		[SyncVar(hook = nameof(SyncNumOfBlobTiles))]
		private int numOfBlobTiles = 1;

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

			blobCore.GetComponentInChildren<SpriteHandler>().SetColor(new Color(154, 205, 50));

			blobTiles.TryAdd(blobCore.GetComponent<CustomNetTransform>().ServerPosition, blobCore);

			coreHealth = blobCore.GetComponent<Integrity>();

			coreHealth.OnWillDestroyServer.AddListener(Death);

			//Block escape shuttle from being called
			GameManager.Instance.PrimaryEscapeShuttle.blockCall = true;
		}

		private void OnEnable()
		{
			UpdateManager.Add(PeriodicUpdate, 1f);

			var uiBlob = UIManager.Display.hudBottomBlob.GetComponent<UI_Blob>();
			uiBlob.blobPlayer = this;
			uiBlob.controller = GetComponent<BlobMouseInputController>();
			healthText = uiBlob.healthText;
			resourceText = uiBlob.resourceText;
			numOfBlobTilesText = uiBlob.numOfBlobTilesText;
		}

		private void OnDisable()
		{
			Death();
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
		}

		private void PeriodicUpdate()
		{
			if (!CustomNetworkManager.IsServer) return;

			timer += 1f;
			econTimer += 1f;
			factoryTimer += 1f;

			if (!teleportCheck && !ValidateAction(playerSync.ServerPosition, true) && blobCore != null)
			{
				teleportCheck = true;

				StartCoroutine(TeleportPlayerBack());
			}

			numOfBlobTiles = blobTiles.Count;

			//Blob wins after number of blob tiles reached
			if (!victory && numOfBlobTiles >= numOfTilesForVictory)
			{
				victory = true;
				BlobWins();
			}

			if (!announcedBlob && (timer >= announceAfterSeconds || numOfBlobTiles >= numOfTilesForDetection))
			{
				announcedBlob = true;

				Chat.AddSystemMsgToChat(
					string.Format(CentComm.BioHazardReportTemplate,
						"Confirmed outbreak of level 5 biohazard aboard the station. All personnel must contain the outbreak."),
					MatrixManager.MainStationMatrix);
				SoundManager.PlayNetworked("Outbreak5");
			}

			//Gain biomass every 5 seconds
			if (econTimer >= 5f)
			{
				econTimer = 0f;

				//Remove null if possible
				resourceBlobs.Remove(null);

				//One biomass for each resource node
				resources += Mathf.RoundToInt(resourceBlobs.Count * econModifier) + 3; //Base income of three
			}

			if (factoryTimer >= 10f)
			{
				factoryTimer = 0f;

				foreach (var factoryBlob in factoryBlobs)
				{
					if (factoryBlob.Key == null) continue;

					factoryBlob.Value.Remove(null);

					if (factoryBlob.Value.Count >= 3) continue;

					var result = Spawn.ServerPrefab(blobSpore, factoryBlob.Key.WorldPosServer(),
						factoryBlob.Key.transform);

					if (!result.Successful) continue;

					factoryBlob.Value.Add(result.GameObject);
				}
			}

			if (blobCore == null) return;

			health = coreHealth.integrity;
		}

		#region teleport

		private IEnumerator TeleportPlayerBack()
		{
			Chat.AddExamineMsgFromServer(gameObject,
				"You feel lost without blob.<color=#FF151F>Move back to the blob</color>");

			yield return WaitFor.Seconds(1f);

			if (ValidateAction(playerSync.ServerPosition, true))
			{
				teleportCheck = false;
				yield break;
			}

			Chat.AddExamineMsgFromServer(gameObject, "Your mind gets sucked back to the core");

			if(blobCore == null) yield break;

			playerSync.SetPosition(blobCore.WorldPosServer());

			teleportCheck = false;
		}

		[Command]
		public void CmdTeleportToCore()
		{
			if(blobCore == null) return;

			playerSync.SetPosition(blobCore.WorldPosServer());
		}

		[Command]
		public void CmdTeleportToNode()
		{
			var nodes = blobTiles.Where(pair => pair.Value != null && pair.Value.GetComponent<BlobStructure>().isNode);

			GameObject node = null;

			var vector = float.PositiveInfinity;

			var pos = playerSync.ServerPosition;

			//Find closet node
			foreach (var coord in nodes)
			{
				var distance = Vector3.Distance(coord.Key, pos);

				if (distance > vector) continue;

				vector = distance;
				node = coord.Value;
			}

			if (node == null)
			{
				//If null go to core instead
				if(blobCore == null) return;

				playerSync.SetPosition(blobCore.WorldPosServer());
				return;
			}

			playerSync.SetPosition(node.WorldPosServer());
		}

		#endregion

		#region SyncVars

		private void SyncResources(int oldVar, int newVar)
		{
			resources = newVar;
			resourceText.text = newVar.ToString();
		}

		private void SyncHealth(float oldVar, float newVar)
		{
			health = newVar;
			healthText.text = newVar.ToString();
		}

		private void SyncNumOfBlobTiles(int oldVar, int newVar)
		{
			numOfBlobTiles = newVar;
			numOfBlobTilesText.text = newVar.ToString();
		}

		#endregion

		#region Manual Placing and attacking

		/// <summary>
		/// Manually placing or attacking
		/// </summary>
		/// <param name="worldPos"></param>
		[Command]
		public void CmdTryPlaceBlobOrAttack(Vector3Int worldPos)
		{
			worldPos.z = 0;

			//Whether player can click anywhere, or if when they click it treats it as if they clicked the tile they're
			//standing on
			if (!clickCoords)
			{
				worldPos = playerSync.ServerPosition;
			}

			if (BlobRemoveMode)
			{
				InternalRemoveBlob(worldPos);
				return;
			}

			if (!ValidateAction(worldPos)) return;

			if (resources < attackCost)
			{
				Chat.AddExamineMsgFromServer(gameObject,
					$"Not enough biomass to attack, you need {attackCost} biomass");
				return;
			}

			if (TryAttack(worldPos))
			{
				resources -= attackCost;
				return;
			}

			if (blobTiles.ContainsKey(worldPos))
			{
				if (blobTiles.TryGetValue(worldPos, out var blob))
				{
					return;
				}

				if (!ValidateCost(normalBlobCost, blobNormalPrefab)) return;

				var replaceResult = Spawn.ServerPrefab(blobNormalPrefab, worldPos, gameObject.transform);

				if (replaceResult.Successful)
				{
					Chat.AddExamineMsgFromServer(gameObject, "You grow a normal blob.");
					blobTiles[worldPos] = replaceResult.GameObject;
				}

				return;
			}

			if (!ValidateCost(normalBlobCost, blobNormalPrefab)) return;

			var result = Spawn.ServerPrefab(blobNormalPrefab, worldPos, gameObject.transform);

			if (result.Successful)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You grow a normal blob.");
				blobTiles.TryAdd(worldPos, result.GameObject);
			}
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
				var playerHit = players.First();
				playerHit.ApplyDamage(gameObject, damage, attackType, damageType);

				Chat.AddAttackMsgToChat(gameObject, playerHit.gameObject, customAttackVerb: "tried to absorb");

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
					if (toHit.GetComponent<BlobStructure>() != null) return false;

					npcComponent.ApplyDamage(gameObject, damage, attackType, damageType);

					Chat.AddAttackMsgToChat(gameObject, toHit.gameObject, customAttackVerb: "tried to absorb");

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

					Chat.AddLocalMsgToChat($"The blob attacks the {toHit.gameObject.ExpensiveName()}", gameObject);

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

		#endregion

		#region Validation

		/// <summary>
		/// Validate that the action can only happen adjacent to existing blob
		/// </summary>
		/// <param name="worldPos"></param>
		/// <returns></returns>
		private bool ValidateAction(Vector3 worldPos, bool noMsg = false)
		{
			var pos = worldPos.RoundToInt();

			foreach (var offSet in coords)
			{
				if (blobTiles.ContainsKey(pos + offSet))
				{
					return true;
				}
			}

			if (noMsg) return false;

			//No adjacent blobs, therefore cannot attack or expand
			Chat.AddExamineMsgFromServer(gameObject, "Can only place blob on or next to existing blob growth.");

			return false;
		}

		/// <summary>
		/// Validate cost, make sure have enough resources
		/// </summary>
		/// <param name="cost"></param>
		/// <param name="toSpawn"></param>
		/// <returns></returns>
		private bool ValidateCost(int cost, GameObject toSpawn, bool noMsg = false)
		{
			if (resources >= cost)
			{
				resources -= cost;
				return true;
			}

			if (noMsg) return false;

			Chat.AddExamineMsgFromServer(gameObject,
				$"Unable to place {toSpawn.ExpensiveName()}, {cost - resources} biomass missing");

			return false;
		}

		#endregion

		#region Effects

		private void PlayAttackEffect(Vector3 worldPos)
		{
			var result = Spawn.ServerPrefab(attackEffect, worldPos, gameObject.transform.parent);

			if (!result.Successful) return;

			StartCoroutine(DespawnEffect(result.GameObject));
		}

		private IEnumerator DespawnEffect(GameObject effect)
		{
			yield return WaitFor.Seconds(1f);

			Despawn.ServerSingle(effect);
		}

		#endregion

		#region PlaceStrong/Reflective

		[Command]
		public void CmdTryPlaceStrongReflective(Vector3Int worldPos)
		{
			worldPos.z = 0;

			if (!ValidateAction(worldPos)) return;

			if (blobTiles.TryGetValue(worldPos, out var blob))
			{
				if (blob != null && blob.GetComponent<BlobStructure>().isNormal)
				{
					if (!ValidateCost(strongBlobCost, blobStrongPrefab)) return;

					var result = Spawn.ServerPrefab(blobStrongPrefab, worldPos, gameObject.transform);

					if (!result.Successful) return;

					Chat.AddExamineMsgFromServer(gameObject,
						"You grow a strong blob, you can now mutate to a reflective.");

					Despawn.ServerSingle(blob);
					blobTiles[worldPos] = result.GameObject;
					return;
				}
				else if (blob != null && blob.GetComponent<BlobStructure>().isStrong)
				{
					if (!ValidateCost(reflectiveBlobCost, blobReflectivePrefab)) return;

					var result = Spawn.ServerPrefab(blobReflectivePrefab, worldPos, gameObject.transform);

					if (!result.Successful) return;

					Chat.AddExamineMsgFromServer(gameObject, "You grow a reflective blob.");

					Despawn.ServerSingle(blob);
					blobTiles[worldPos] = result.GameObject;
					return;
				}
			}

			//No normal blob at tile
			Chat.AddExamineMsgFromServer(gameObject, "You need to place a normal blob first");
		}

		#endregion

		#region PlaceOther

		[Command]
		public void CmdTryPlaceOther(Vector3Int worldPos, BlobConstructs blobConstructs)
		{
			worldPos.z = 0;

			if (!ValidateAction(worldPos)) return;

			GameObject prefab = null;

			var cost = 0;

			switch (blobConstructs)
			{
				case BlobConstructs.Node:
					prefab = blobNodePrefab;
					cost = nodeBlobCost;
					break;
				case BlobConstructs.Factory:
					prefab = blobFactoryPrefab;
					cost = factoryBlobCost;
					break;
				case BlobConstructs.Resource:
					prefab = blobResourcePrefab;
					cost = resourceBlobCost;
					break;
			}

			if (prefab == null) return;

			if (blobTiles.TryGetValue(worldPos, out var blob))
			{
				if (blob != null && blob.GetComponent<BlobStructure>().isNormal)
				{
					if (!ValidateCost(cost, prefab)) return;

					var result = Spawn.ServerPrefab(prefab, worldPos, gameObject.transform);

					if (!result.Successful) return;

					Chat.AddExamineMsgFromServer(gameObject, $"You grow a {blobConstructs} blob.");

					switch (blobConstructs)
					{
						case BlobConstructs.Node:
							nodeBlobs.Add(result.GameObject);
							break;
						case BlobConstructs.Factory:
							factoryBlobs.TryAdd(result.GameObject, new HashSet<GameObject>());
							break;
						case BlobConstructs.Resource:
							resourceBlobs.Add(result.GameObject);
							break;
					}

					Despawn.ServerSingle(blob);
					blobTiles[worldPos] = result.GameObject;
					return;
				}
			}

			//No normal blob at tile
			Chat.AddExamineMsgFromServer(gameObject, "You need to place a normal blob first");
		}

		#endregion

		#region RemoveBlob

		[Command]
		public void CmdRemoveBlob(Vector3Int worldPos)
		{
			worldPos.z = 0;

			InternalRemoveBlob(worldPos);
		}

		private void InternalRemoveBlob(Vector3Int worldPos)
		{
			if (!blobTiles.TryGetValue(worldPos, out var blob) || blob == null)
			{
				Chat.AddExamineMsgFromServer(gameObject, "No blob to be removed");
			}
			else
			{
				var blobStructure = blob.GetComponent<BlobStructure>();

				if (blobStructure.isNode || blobStructure.isCore)
				{
					Chat.AddExamineMsgFromServer(gameObject, "This is a core or node blob. It cannot be removed");
					return;
				}

				var returnCost = 0;

				if (blobStructure.isNormal)
				{
					returnCost = Mathf.RoundToInt(normalBlobCost * refundPercentage);
				}
				else if (blobStructure.isStrong)
				{
					returnCost = Mathf.RoundToInt(strongBlobCost * refundPercentage);
				}
				else if (blobStructure.isReflective)
				{
					returnCost = Mathf.RoundToInt(reflectiveBlobCost * refundPercentage);
				}
				else if (blobStructure.isResource)
				{
					returnCost = Mathf.RoundToInt(resourceBlobCost * refundPercentage);
				}
				else if (blobStructure.isFactory)
				{
					returnCost = Mathf.RoundToInt(factoryBlobCost * refundPercentage);
				}

				resources += returnCost;

				Chat.AddExamineMsgFromServer(gameObject, $"Blob removed, {returnCost} biomass refunded");

				Despawn.ServerSingle(blob);
				blobTiles.TryRemove(worldPos, out var empty);
			}
		}

		/// <summary>
		/// Toggle Remove when clicking, not just Alt clicking
		/// </summary>
		[Command]
		public void CmdToggleRemove(bool forceOff)
		{
			if (forceOff)
			{
				BlobRemoveMode = false;
				return;
			}

			BlobRemoveMode = !BlobRemoveMode;
		}

		#endregion

		#region Death

		public void Death(DestructionInfo info = null)
		{
			coreHealth.OnWillDestroyServer.RemoveListener(Death);

			foreach (var tile in blobTiles)
			{
				if (tile.Value != null)
				{
					Despawn.ServerSingle(tile.Value);
				}
			}

			blobTiles = new ConcurrentDictionary<Vector3Int, GameObject>();

			GameManager.Instance.PrimaryEscapeShuttle.blockCall = false;

			GameManager.Instance.EndRound();
		}

		#endregion

		#region Victory

		private void BlobWins()
		{
			econModifier = 10f;

			GameManager.Instance.CentComm.ChangeAlertLevel(CentComm.AlertLevel.Delta);

			Chat.AddSystemMsgToChat(
				string.Format(CentComm.BioHazardReportTemplate,
					"The biohazard has overwhelmed the station. Station integrity critical!"),
				MatrixManager.MainStationMatrix);

			StartCoroutine(EndRound());
		}

		private IEnumerator EndRound()
		{
			yield return WaitFor.Seconds(180f);

			GameManager.Instance.EndRound();
		}

		#endregion

		#region SwitchCore

		[Command]
		public void CmdMoveCore(Vector3Int worldPos)
		{
			if (blobTiles.TryGetValue(worldPos, out var blob) && blob.GetComponent<BlobStructure>().isNode)
			{
				SwitchCore(blob);
			}
		}

		/// <summary>
		/// Switches a node into a core
		/// </summary>
		/// <param name="oldNode"></param>
		public void SwitchCore(GameObject oldNode)
		{
			if (!oldNode.TryGetComponent<BlobStructure>(out var blob))
			{
				Chat.AddExamineMsgFromServer(gameObject, "Unable to move core to node");
				return;
			}

			if (!blob.isNode)
			{
				Chat.AddExamineMsgFromServer(gameObject, "Can only move the core to a node");
				return;
			}

			if (!ValidateCost(moveCoreCost, blobCorePrefab, true))
			{
				Chat.AddExamineMsgFromServer(gameObject,
					$"Not enough biomass to move core, {moveCoreCost - resources} biomass missing");
				return;
			}

			var core = blobCore.GetComponent<CustomNetTransform>();
			var node = oldNode.GetComponent<CustomNetTransform>();

			var coreCache = core.ServerPosition;

			core.SetPosition(node.ServerPosition);
			node.SetPosition(coreCache);

			//Todo run any expand stuff again here
		}

		#endregion
	}

	public enum BlobConstructs
	{
		Core,
		Node,
		Resource,
		Factory,
		Strong,
		Reflective
	}

	public class BlobVariants
	{
		public string name;

		public List<Damages> damages = new List<Damages>();

		public List<Resistances> resistanceses = new List<Resistances>();

		public Color color;
	}

	[Serializable]
	public class Damages
	{
		public int damageDone;

		public DamageType damageType;
	}
}