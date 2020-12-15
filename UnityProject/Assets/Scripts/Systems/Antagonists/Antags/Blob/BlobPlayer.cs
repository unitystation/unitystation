using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DatabaseAPI;
using Light2D;
using Mirror;
using UnityEngine;
using TMPro;
using AddressableReferences;
using Random = UnityEngine.Random;

namespace Blob
{
	/// <summary>
	/// Class which has the logic and data for the blob player
	/// </summary>
	public class BlobPlayer : NetworkBehaviour
	{
		[SerializeField] private AddressableAudioSource Outbreak5 = null;

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

		public List<BlobStrain> blobStrains = new List<BlobStrain>();

		private BlobStrain currentStrain;

		private GameObject blobCore;
		private Integrity coreHealth;
		private UI_Blob uiBlob;

		public int layerDamage = 50;

		public int adaptStrainCost = 40;
		public int rerollStrainsCost = 20;

		private PlayerSync playerSync;
		private RegisterPlayer registerPlayer;
		private PlayerScript playerScript;
		public int numOfTilesForVictory = 400;
		private int numOfTilesForDetection = 75;

		private bool teleportCheck;
		private bool victory;
		private bool announcedBlob;
		private float announceAfterSeconds = 600f;
		private bool hasDied;
		private bool halfWay;
		private bool nearlyWon;

		[HideInInspector] public bool BlobRemoveMode;

		private float timer = 0f;
		private float econTimer = 0f;
		private float factoryTimer = 0f;
		private float healthTimer = 0f;
		private float rerollTimer = 0f;

		private string overmindName;

		[SerializeField]
		private float econModifier = 1f;

		[SerializeField]
		private float maxBiomass = 100f;

		[SerializeField]
		[Tooltip("If true then there will be announcements when blob is close to destroying station, after the initial biohazard.")]
		private bool isBlobGamemode = true;

		[SerializeField]
		private bool endRoundWhenKilled = false;

		[SerializeField]
		private bool endRoundWhenBlobVictory = true;

		[SerializeField]
		private bool rapidExpand = false;

		[SerializeField]
		private int blobSpreadDistance = 4;

		[SerializeField]
		private int buildDistanceLimit = 4;

		[SerializeField]
		private GameObject overmindLightObject = null;

		[SerializeField]
		private LightSprite overmindLight = null;

		[SerializeField]
		private GameObject overmindSprite = null;

		public bool clickCoords = true;

		private ConcurrentDictionary<Vector3Int, BlobStructure> blobTiles =
			new ConcurrentDictionary<Vector3Int, BlobStructure>();

		private HashSet<GameObject> nonSpaceBlobTiles = new HashSet<GameObject>();

		private HashSet<GameObject> resourceBlobs = new HashSet<GameObject>();

		private ConcurrentDictionary<GameObject, HashSet<GameObject>> factoryBlobs =
			new ConcurrentDictionary<GameObject, HashSet<GameObject>>();

		private HashSet<BlobStructure> nodeBlobs = new HashSet<BlobStructure>();

		private List<Vector3Int> coords = new List<Vector3Int>
		{
			new Vector3Int(0, 0, 0),
			new Vector3Int(0, 1, 0),
			new Vector3Int(1, 0, 0),
			new Vector3Int(0, -1, 0),
			new Vector3Int(-1, 0, 0)
		};

		public float Resources
		{
			get { return resources; }
			set
			{
				resources = value;

				TargetRpcSyncResources(connectionToClient, resources);
			}
		}

		private float resources = 0;

		public float Health
		{
			get { return health; }
			set
			{
				health = value;

				TargetRpcSyncHealth(connectionToClient, health);
			}
		}

		private float health = 400f;

		public int NumOfNonSpaceBlobTiles
		{
			get { return numOfNonSpaceBlobTiles; }
			set
			{
				numOfNonSpaceBlobTiles = value;

				TargetRpcSyncNumOfBlobTiles(connectionToClient, numOfNonSpaceBlobTiles);
			}
		}

		private int numOfNonSpaceBlobTiles = 1;

		//Amount of free strain rerolls
		public int StrainRerolls
		{
			get { return strainRerolls; }
			set
			{
				strainRerolls = value;

				TargetRpcSyncStrainRerolls(connectionToClient, strainRerolls);
			}
		}

		private int strainRerolls = 1;

		//Needed to run the Rpc on client
		public int StrainIndex
		{
			get { return strainIndex; }
			set
			{
				strainIndex = value;

				TargetRpcSyncStrainIndex(connectionToClient, strainIndex);
			}
		}

		private int strainIndex;

		public BlobStrain clientCurrentStrain;

		private int numOfBlobTiles = 1;

		private int maxCount = 0;

		private int maxNonSpaceCount = 0;

		/// <summary>
		/// The start function of the script called from BlobStarter when player turns into blob, sets up core.
		/// </summary>
		public void BlobStart()
		{
			playerSync = GetComponent<PlayerSync>();
			registerPlayer = GetComponent<RegisterPlayer>();
			playerScript = GetComponent<PlayerScript>();

			if (playerScript == null && (!TryGetComponent(out playerScript) || playerScript == null))
			{
				Debug.LogError("Playerscript was null on blob and couldnt be found.");
				return;
			}

			playerScript.mind.ghost = playerScript;
			playerScript.mind.body = playerScript;

			overmindName = $"Overmind {Random.Range(1, 1001)}";

			playerScript.SetPermanentName(overmindName);

			playerScript.IsPlayerSemiGhost = true;

			var result = Spawn.ServerPrefab(blobCorePrefab, playerSync.ServerPosition, gameObject.transform);

			if (!result.Successful)
			{
				Debug.LogError("Failed to spawn blob core for player!");
				return;
			}

			TargetRpcTurnOnClientLight(connectionToClient);

			blobCore = result.GameObject;

			var pos = blobCore.GetComponent<CustomNetTransform>().ServerPosition;

			var structure = blobCore.GetComponent<BlobStructure>();

			blobTiles.TryAdd(pos, structure);
			nonSpaceBlobTiles.Add(blobCore);

			structure.location = pos;
			structure.overmindName = overmindName;

			currentStrain = blobStrains.PickRandom();
			StrainIndex = blobStrains.IndexOf(currentStrain);

			SetStrainData(structure);
			SetLightAndColor(structure);

			//Make core act like node
			structure.expandCoords = GenerateCoords(pos);
			structure.healthPulseCoords = structure.expandCoords;
			nodeBlobs.Add(structure);

			//Set up death detection
			coreHealth = blobCore.GetComponent<Integrity>();
			coreHealth.OnWillDestroyServer.AddListener(Death);
			SubscribeToDamage(structure);

			//Block escape shuttle from leaving station when it arrives
			GameManager.Instance.PrimaryEscapeShuttle.SetHostileEnvironment(true);
		}

		private void OnEnable()
		{
			UpdateManager.Add(PeriodicUpdate, 1f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
		}

		private void Start()
		{
			playerSync = GetComponent<PlayerSync>();
			registerPlayer = GetComponent<RegisterPlayer>();
			playerScript = GetComponent<PlayerScript>();
		}

		private void PeriodicUpdate()
		{
			if (!CustomNetworkManager.IsServer) return;

			timer += 1f;
			econTimer += 1f;
			factoryTimer += 1f;
			healthTimer += 1f;
			rerollTimer += 1f;

			//Force overmind back to blob if camera moves too far
			if (!teleportCheck && !victory && !ValidateAction(playerSync.ServerPosition, true) && blobCore != null)
			{
				teleportCheck = true;

				StartCoroutine(TeleportPlayerBack());
			}

			nonSpaceBlobTiles.Remove(null);

			//Count number of blob tiles
			NumOfNonSpaceBlobTiles = nonSpaceBlobTiles.Count;
			numOfBlobTiles = blobTiles.Count;

			if (numOfBlobTiles > maxCount)
			{
				maxCount = numOfBlobTiles;
			}

			if (NumOfNonSpaceBlobTiles > maxNonSpaceCount)
			{
				maxNonSpaceCount = NumOfNonSpaceBlobTiles;
			}

			if (isBlobGamemode && !halfWay && NumOfNonSpaceBlobTiles >= numOfTilesForVictory / 2)
			{
				halfWay = true;

				Chat.AddSystemMsgToChat(
					string.Format(CentComm.BioHazardReportTemplate,
						"Caution! Biohazard expanding rapidly. Station structural integrity failing."),
					MatrixManager.MainStationMatrix);
				SoundManager.PlayNetworked(SingletonSOSounds.Instance.Notice1);
			}

			if (isBlobGamemode && !nearlyWon && NumOfNonSpaceBlobTiles >= numOfTilesForVictory / 1.25)
			{
				nearlyWon = true;

				Chat.AddSystemMsgToChat(
					string.Format(CentComm.BioHazardReportTemplate,
						"Alert! Station integrity near critical. Biomass sensor levels are off the charts."),
					MatrixManager.MainStationMatrix);
				SoundManager.PlayNetworked(SingletonSOSounds.Instance.Notice1);
			}

			//Blob wins after number of blob tiles reached
			if (!victory && NumOfNonSpaceBlobTiles >= numOfTilesForVictory)
			{
				victory = true;
				BlobWins();
			}

			//Detection check
			if (!announcedBlob && (timer >= announceAfterSeconds || numOfBlobTiles >= numOfTilesForDetection))
			{
				announcedBlob = true;

				Chat.AddSystemMsgToChat(
					string.Format(CentComm.BioHazardReportTemplate,
						"Confirmed outbreak of level 5 biohazard aboard the station. All personnel must contain the outbreak."),
					MatrixManager.MainStationMatrix);
				SoundManager.PlayNetworked(Outbreak5);
			}

			if (rerollTimer > 300f)
			{
				rerollTimer = 0f;

				StrainRerolls += 1;
			}

			BiomassTick();

			TrySpawnSpores();

			AutoExpandBlob();

			HealthPulse();

			if (blobCore == null) return;

			Health = coreHealth.integrity;
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

			Chat.AddExamineMsgFromServer(gameObject, "Your mind gets sucked back to the blob");

			if(blobCore == null) yield break;

			TeleportToNode();

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
			TeleportToNode();
		}

		private void TeleportToNode()
		{
			GameObject node = null;

			var vector = float.PositiveInfinity;

			var pos = playerSync.ServerPosition;

			//Find closet node
			foreach (var blobStructure in nodeBlobs)
			{
				var distance = Vector3.Distance(blobStructure.location, pos);

				if (distance > vector) continue;

				vector = distance;
				node = blobStructure.gameObject;
			}

			//If null go to core instead
			if (node == null)
			{
				//Blob is dead :(
				if(blobCore == null) return;

				playerSync.SetPosition(blobCore.WorldPosServer());
				return;
			}

			playerSync.SetPosition(node.WorldPosServer());
		}

		#endregion

		#region TargetRpc

		[TargetRpc]
		private void TargetRpcTurnOnClientLight(NetworkConnection target)
		{
			TurnOnClientLight();
			playerScript.IsPlayerSemiGhost = true;
			uiBlob = UIManager.Display.hudBottomBlob.GetComponent<UI_Blob>();
			uiBlob.blobPlayer = this;
			uiBlob.controller = GetComponent<BlobMouseInputController>();
		}

		//Client Side
		public void TurnOnClientLight()
		{
			overmindLightObject.SetActive(true);

			var colour = Color.green;

			if (clientCurrentStrain != null)
			{
				colour = clientCurrentStrain.color;
			}

			overmindLight.Color = colour;
			overmindLight.Color.a = 0.2f;
			overmindSprite.layer = 29;
		}

		[TargetRpc]
		private void TargetRpcTurnOffBlob(NetworkConnection target)
		{
			playerScript.IsPlayerSemiGhost = false;
			PlayerManager.LocalPlayerScript.IsPlayerSemiGhost = false;
		}

		[TargetRpc]
		private void TargetRpcSyncResources(NetworkConnection target, float newVar)
		{
			uiBlob.resourceText.text = Mathf.FloorToInt(newVar).ToString();
		}

		[TargetRpc]
		private void TargetRpcSyncHealth(NetworkConnection target, float newVar)
		{
			uiBlob.healthText.text = newVar.ToString();
		}

		[TargetRpc]
		private void TargetRpcSyncNumOfBlobTiles(NetworkConnection target, int newVar)
		{
			uiBlob.numOfBlobTilesText.text = newVar.ToString();
		}

		[TargetRpc]
		private void TargetRpcSyncStrainRerolls(NetworkConnection target, int newVar)
		{
			uiBlob.strainRerollsText.text = newVar.ToString();
		}

		[TargetRpc]
		private void TargetRpcSyncStrainIndex(NetworkConnection target, int newVar)
		{
			clientCurrentStrain = blobStrains[newVar];
			uiBlob.UpdateStrainInfo();
			TurnOnClientLight();
		}

		[TargetRpc]
		private void TargetRpcForceStrainReset(NetworkConnection target)
		{
			var strains = blobStrains.Where(s => s != clientCurrentStrain);
			uiBlob.randomStrains = strains.PickRandom(4).ToList();

			uiBlob.UpdateStrainInfo();
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
			//standing on (or around since validation checks adjacent)
			if (!clickCoords)
			{
				worldPos = playerSync.ServerPosition;
			}

			//Whether player has toggled always remove on in the UI
			if (BlobRemoveMode)
			{
				InternalRemoveBlob(worldPos);
				return;
			}

			PlaceBlobOrAttack(worldPos);
		}

		//return value only used by auto expand, return true if blob is already there so can be removed
		//from the coords that need expanding to. Return false in all other cases
		private bool PlaceBlobOrAttack(Vector3Int worldPos, bool autoExpanding = false)
		{
			if (!ValidateAction(worldPos)) return false;

			if (!autoExpanding && Resources < attackCost)
			{
				Chat.AddExamineMsgFromServer(gameObject,
					$"Not enough biomass to attack, you need {attackCost} biomass");
				return false;
			}

			if (!autoExpanding && Cooldowns.IsOn(playerScript, CooldownID.Asset(CommonCooldowns.Instance.Melee, NetworkSide.Server)))
			{
				//On attack cooldown
				return false;
			}

			if (TryAttack(worldPos, autoExpanding))
			{
				if (autoExpanding)
				{
					return false;
				}

				if (!victory)
				{
					Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Melee);
				}

				Resources -= attackCost;
				return true;
			}

			return TryExpand(worldPos, autoExpanding);
		}

		/// <summary>
		/// Try to expand blob, doesnt do a passable check!
		/// </summary>
		/// <param name="worldPos"></param>
		/// <param name="autoExpanding"></param>
		/// <returns></returns>
		private bool TryExpand(Vector3Int worldPos, bool autoExpanding = false)
		{
			if (currentStrain.strainType == StrainTypes.NetworkedFibers && !ValidateNextToCore(worldPos))
			{
				Chat.AddExamineMsgFromServer(gameObject, "You can only expand next to the core, due to the current strain");
				return false;
			}

			//See if theres blob already there
			if (blobTiles.ContainsKey(worldPos))
			{
				if (blobTiles.TryGetValue(worldPos, out var blob) && blob != null)
				{
					if (currentStrain.strainType == StrainTypes.NetworkedFibers)
					{
						//Move core to normal blob when networked fibers strain
						MoveCoreToNormalBlob(blob);
						return true;
					}

					//Cant place normal blob where theres normal blob
					return true;
				}

				SpawnNormalBlob(worldPos, autoExpanding);
				return false;
			}

			//If blob doesnt exist at that location already try placing
			SpawnNormalBlob(worldPos, autoExpanding, true);

			return false;
		}

		private void SpawnNormalBlob(Vector3Int worldPos, bool autoExpanding, bool newPosition = false)
		{
			if (!autoExpanding && !ValidateCost(normalBlobCost, blobNormalPrefab)) return;

			var result = Spawn.ServerPrefab(blobNormalPrefab, worldPos, gameObject.transform);

			if (!result.Successful) return;

			if (!autoExpanding)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You grow a normal blob.");
			}

			var structure = result.GameObject.GetComponent<BlobStructure>();

			structure.location = worldPos;
			structure.overmindName = overmindName;
			SetStrainData(structure);
			SetLightAndColor(structure);

			AddNonSpaceBlob(result.GameObject);

			SubscribeToDamage(structure);

			if (newPosition)
			{
				blobTiles.TryAdd(worldPos, structure);
				return;
			}

			blobTiles[worldPos] = structure;
		}

		private bool TryAttack(Vector3 worldPos, bool autoExpanding = false)
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

			foreach (var player in players)
			{
				if(player.IsDead) continue;

				foreach (var playerDamage in currentStrain.playerDamages)
				{
					player.ApplyDamage(gameObject, playerDamage.damageDone, AttackType.Melee, playerDamage.damageType);
				}

				Chat.AddAttackMsgToChat(gameObject, player.gameObject, customAttackVerb: "tried to absorb", posOverride: worldPos);

				PlayAttackEffect(pos);

				return true;
			}

			var hits = matrix.Get<RegisterTile>(local, ObjectType.Object, true)
				.Where( hit => hit != null && !hit.IsPassable(true) && (!hit.TryGetComponent<BlobStructure>(out var structure) || structure.overmindName != overmindName));

			foreach (var hit in hits)
			{
				//Try damage NPC
				if (hit.TryGetComponent<LivingHealthBehaviour>(out var npcComponent))
				{
					if(npcComponent.IsDead) continue;

					foreach (var npcDamage in currentStrain.playerDamages)
					{
						npcComponent.ApplyDamage(gameObject, npcDamage.damageDone, AttackType.Melee, npcDamage.damageType);
					}

					Chat.AddAttackMsgToChat(gameObject, hit.gameObject, customAttackVerb: "tried to absorb", posOverride: worldPos);

					PlayAttackEffect(pos);

					return true;
				}

				//Dont bother destroying passable stuff, eg open door
				if (hit.IsPassable(true)) continue;

				if (hit.TryGetComponent<Integrity>(out var component) && !component.Resistances.Indestructable)
				{
					foreach (var objectDamage in currentStrain.objectDamages)
					{
						component.ApplyDamage(objectDamage.damageDone, AttackType.Melee, objectDamage.damageType, true);
					}

					if (!autoExpanding)
					{
						Chat.AddLocalMsgToChat($"The blob attacks the {hit.gameObject.ExpensiveName()}", gameObject);
					}

					PlayAttackEffect(pos);

					return true;
				}
			}

			//Do check to see if the impassable thing is a friendly blob, as it will be the only object left
			var hitsSecond = matrix.Get<RegisterTile>(local, ObjectType.Object, true)
				.Where(hit => hit != null && hit.TryGetComponent<BlobStructure>(out var structure) && structure != null
				&& structure.overmindName == overmindName);

			if (hitsSecond.Any())
			{
				return false;
			}

			//Check for walls, windows and grills
			if (metaTileMap != null && !MatrixManager.IsPassableAtAllMatricesOneTile(pos, true))
			{
				//Cell pos is unused var
				metaTileMap.ApplyDamage(Vector3Int.zero, layerDamage, pos);

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
			if (Resources >= cost)
			{
				Resources -= cost;
				return true;
			}

			if (noMsg) return false;

			Chat.AddExamineMsgFromServer(gameObject,
				$"Unable to place {toSpawn.ExpensiveName()}, {cost - Resources} biomass missing");

			return false;
		}

		/// <summary>
		/// Validate the distance between specialised blobs
		/// </summary>
		/// <param name="worldPos"></param>
		/// <returns></returns>
		private bool ValidateDistance(Vector3Int worldPos, bool excludeNodes = false, bool excludeFactory = false,
			bool excludeResource = false)
		{
			var specialisedBlobs = new List<Vector3Int>();

			if (!excludeNodes)
			{
				foreach (var node in nodeBlobs)
				{
					if(node == null) continue;

					specialisedBlobs.Add(node.location);
				}
			}

			if (!excludeFactory)
			{
				foreach (var factory in factoryBlobs)
				{
					if(factory.Key == null) continue;

					specialisedBlobs.Add(factory.Key.GetComponent<BlobStructure>().location);
				}
			}

			if (!excludeResource)
			{
				foreach (var resourceBlob in resourceBlobs)
				{
					if(resourceBlob == null) continue;

					specialisedBlobs.Add(resourceBlob.GetComponent<BlobStructure>().location);
				}
			}

			foreach (var blob in specialisedBlobs)
			{
				if (Vector3Int.Distance(blob, worldPos) <= buildDistanceLimit)
				{
					//A blob is too close
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Check to see if pos is next to core position
		/// </summary>
		/// <param name="worldPos"></param>
		/// <param name="noMsg"></param>
		/// <returns></returns>
		private bool ValidateNextToCore(Vector3 worldPos)
		{
			var pos = worldPos.RoundToInt();

			var corePos = blobCore.WorldPosServer();

			foreach (var offSet in coords)
			{
				if (pos == offSet + corePos)
				{
					return true;
				}
			}
			return false;
		}

		#endregion

		#region Effects

		private void PlayAttackEffect(Vector3 worldPos)
		{
			var result = Spawn.ServerPrefab(attackEffect, worldPos, gameObject.transform);

			if (!result.Successful) return;

			StartCoroutine(DespawnEffect(result.GameObject));
		}

		private IEnumerator DespawnEffect(GameObject effect)
		{
			yield return WaitFor.Seconds(0.5f);

			Despawn.ServerSingle(effect);
		}

		#endregion

		#region PlaceStrong/Reflective

		[Command]
		public void CmdTryPlaceStrongReflective(Vector3Int worldPos)
		{
			worldPos.z = 0;

			if (!ValidateAction(worldPos)) return;

			if (blobTiles.TryGetValue(worldPos, out var blob) && blob != null)
			{
				//Try place strong
				if (blob != null && blob.isNormal)
				{
					PlaceStrongReflective(blobStrongPrefab, blob, strongBlobCost, worldPos, "You grow a strong blob, you can now mutate to a reflective.");
					return;
				}

				//Try place reflective
				if (blob != null && blob.isStrong)
				{
					PlaceStrongReflective(blobReflectivePrefab, blob, reflectiveBlobCost, worldPos, "You grow a reflective blob.");
					return;
				}
			}

			//No normal blob at tile
			Chat.AddExamineMsgFromServer(gameObject, "You need to place a normal blob first");
		}

		private void PlaceStrongReflective(GameObject prefab, BlobStructure originalBlob, int cost, Vector3Int worldPos, string msg)
		{
			if (!ValidateCost(cost, prefab)) return;

			var result = Spawn.ServerPrefab(prefab, worldPos, gameObject.transform);

			if (!result.Successful) return;

			Chat.AddExamineMsgFromServer(gameObject, msg);

			Despawn.ServerSingle(originalBlob.gameObject);

			var structure = result.GameObject.GetComponent<BlobStructure>();
			structure.overmindName = overmindName;

			SetStrainData(structure);
			SetLightAndColor(structure);

			SubscribeToDamage(structure);

			blobTiles[worldPos] = structure;
			AddNonSpaceBlob(result.GameObject);
		}

		#endregion

		#region PlaceOther

		[Command]
		public void CmdTryPlaceOther(Vector3Int worldPos, BlobConstructs blobConstructs)
		{
			worldPos.z = 0;

			if (!ValidateAction(worldPos)) return;

			if (MatrixManager.IsSpaceAt(worldPos, true))
			{
				Chat.AddExamineMsgFromServer(gameObject, "Cannot place structures on space blob, find a sturdier location");
				return;
			}

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
				default:
					Debug.LogError("Switch has no correct case for blob structure!");
					break;
			}

			if (prefab == null) return;

			if (blobTiles.TryGetValue(worldPos, out var blob) && blob != null)
			{
				if (blob != null && blob.isNormal)
				{
					if (blobConstructs != BlobConstructs.Node && !ValidateDistance(worldPos, true))
					{
						Chat.AddExamineMsgFromServer(gameObject, $"Too close to another factory or resource blob, place at least {buildDistanceLimit}m away");
						return;
					}

					if (blobConstructs == BlobConstructs.Node && !ValidateDistance(worldPos, excludeFactory: true, excludeResource: true))
					{
						Chat.AddExamineMsgFromServer(gameObject, $"Too close to another node, place at least {buildDistanceLimit}m away");
						return;
					}

					if (!ValidateCost(cost, prefab)) return;

					var result = Spawn.ServerPrefab(prefab, worldPos, gameObject.transform);

					if (!result.Successful) return;

					Chat.AddExamineMsgFromServer(gameObject, $"You grow a {blobConstructs} blob.");

					var structure = result.GameObject.GetComponent<BlobStructure>();
					structure.overmindName = overmindName;

					switch (blobConstructs)
					{
						case BlobConstructs.Node:

							structure.expandCoords = GenerateCoords(worldPos);
							structure.healthPulseCoords = structure.expandCoords;
							nodeBlobs.Add(structure);
							break;
						case BlobConstructs.Factory:
							factoryBlobs.TryAdd(result.GameObject, new HashSet<GameObject>());
							break;
						case BlobConstructs.Resource:
							resourceBlobs.Add(result.GameObject);
							break;
						default:
							Debug.LogError("Switch has no correct case for blob structure!");
							break;
					}

					Despawn.ServerSingle(blob.gameObject);

					structure.location = worldPos;
					SetStrainData(structure);
					SetLightAndColor(structure);
					SubscribeToDamage(structure);

					blobTiles[worldPos] = structure;
					AddNonSpaceBlob(result.GameObject);
					return;
				}
			}

			//No normal blob at tile
			Chat.AddExamineMsgFromServer(gameObject, "You need to place a normal blob first");
		}

		#endregion

		#region BlobTileDeath

		private void BlobTileDeath(DestructionInfo info)
		{
			nonSpaceBlobTiles.Remove(info.Destroyed.gameObject);

			if (blobTiles.TryRemove(info.Destroyed.GetComponent<BlobStructure>().location, out var removed))
			{
				nodeBlobs.Remove(removed);
			}

			factoryBlobs.TryRemove(info.Destroyed.gameObject, out var spores);

			info.Destroyed.OnWillDestroyServer.RemoveListener(BlobTileDeath);
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
				if (blob.isNode || blob.isCore)
				{
					Chat.AddExamineMsgFromServer(gameObject, "This is a core or node blob. It cannot be removed");
					return;
				}

				var returnCost = 0;

				if (blob.isNormal)
				{
					returnCost = Mathf.RoundToInt(normalBlobCost * refundPercentage);
				}
				else if (blob.isStrong)
				{
					returnCost = Mathf.RoundToInt(strongBlobCost * refundPercentage);
				}
				else if (blob.isReflective)
				{
					returnCost = Mathf.RoundToInt(reflectiveBlobCost * refundPercentage);
				}
				else if (blob.isResource)
				{
					returnCost = Mathf.RoundToInt(resourceBlobCost * refundPercentage);
				}
				else if (blob.isFactory)
				{
					returnCost = Mathf.RoundToInt(factoryBlobCost * refundPercentage);
				}

				Chat.AddExamineMsgFromServer(gameObject, $"Blob removed, {AddToResources(returnCost)} biomass refunded");

				nonSpaceBlobTiles.Remove(blob.gameObject);
				Despawn.ServerSingle(blob.gameObject);
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
			if(!CustomNetworkManager.IsServer || hasDied) return;

			hasDied = true;

			coreHealth.OnWillDestroyServer.RemoveListener(Death);

			//Destroy all blob tiles
			foreach (var tile in blobTiles)
			{
				if (tile.Value != null)
				{
					Despawn.ServerSingle(tile.Value.gameObject);
				}
			}

			blobTiles = new ConcurrentDictionary<Vector3Int, BlobStructure>();

			GameManager.Instance.PrimaryEscapeShuttle.SetHostileEnvironment(false);

			Chat.AddSystemMsgToChat(
				string.Format(CentComm.BioHazardReportTemplate,
					"The biohazard has been contained."),
				MatrixManager.MainStationMatrix);

			playerScript.IsPlayerSemiGhost = false;
			TargetRpcTurnOffBlob(connectionToClient);

			//Make blob into ghost
			PlayerSpawn.ServerSpawnGhost(playerScript.mind);

			if (endRoundWhenKilled)
			{
				GameManager.Instance.EndRound();
			}

			Destroy(this);
		}

		#endregion

		#region Victory

		private void BlobWins()
		{
			GameManager.Instance.CentComm.ChangeAlertLevel(CentComm.AlertLevel.Delta);

			Chat.AddSystemMsgToChat(
				string.Format(CentComm.BioHazardReportTemplate,
					"Biohazard has reached critical mass. Station integrity critical!"),
				MatrixManager.MainStationMatrix);

			Chat.AddExamineMsgFromServer(gameObject, "Your hunger is unstoppable, you are fully unleashed");

			maxBiomass = 5000;

			econModifier = 10f;

			rapidExpand = true;

			foreach (var objective in playerScript.mind.GetAntag().Objectives)
			{
				objective.SetAsComplete();
			}

			if (endRoundWhenBlobVictory)
			{
				StartCoroutine(EndRound());
			}
		}

		private IEnumerator EndRound()
		{
			yield return WaitFor.Seconds(180f);

			Chat.AddGameWideSystemMsgToChat("The blob has consumed the station, we are all but goo now.");

			Chat.AddGameWideSystemMsgToChat($"At its biggest the blob had {maxCount} tiles controlled" +
			                                $" but only had {maxNonSpaceCount} non-space tiles which counted to victory.");

			GameManager.Instance.EndRound();
		}

		#endregion

		#region SwitchCore

		[Command]
		public void CmdMoveCore(Vector3Int worldPos)
		{
			worldPos.z = 0;

			if (blobTiles.TryGetValue(worldPos, out var blob) && blob != null)
			{
				SwitchCore(blob);
			}
		}

		/// <summary>
		/// Switches a node into a core
		/// </summary>
		/// <param name="oldNode"></param>
		public void SwitchCore(BlobStructure oldNode)
		{
			if (!oldNode.isNode)
			{
				Chat.AddExamineMsgFromServer(gameObject, "Can only move the core to a node");
				return;
			}

			if (!ValidateCost(moveCoreCost, blobCorePrefab, true))
			{
				Chat.AddExamineMsgFromServer(gameObject,
					$"Not enough biomass to move core, {moveCoreCost - Resources} biomass missing");
				return;
			}

			var core = blobCore.GetComponent<CustomNetTransform>();
			var node = oldNode.GetComponent<CustomNetTransform>();

			var coreCache = core.ServerPosition;

			core.SetPosition(node.ServerPosition);
			blobTiles[node.ServerPosition] = blobCore.GetComponent<BlobStructure>();
			blobTiles[coreCache] = oldNode;
			node.SetPosition(coreCache);

			ResetArea(blobCore);
			ResetArea(oldNode.gameObject);
		}

		/// <summary>
		/// Switches a node into a core
		/// </summary>
		/// <param name="oldNode"></param>
		public void MoveCoreToNormalBlob(BlobStructure oldNormal)
		{
			if (!oldNormal.isNormal)
			{
				Chat.AddExamineMsgFromServer(gameObject, "Can only move the core to a normal blob");
				return;
			}

			var core = blobCore.GetComponent<CustomNetTransform>();
			var normal = oldNormal.GetComponent<CustomNetTransform>();

			var coreCache = core.ServerPosition;

			core.SetPosition(normal.ServerPosition);
			blobTiles[normal.ServerPosition] = blobCore.GetComponent<BlobStructure>();
			blobTiles[coreCache] = oldNormal;
			normal.SetPosition(coreCache);

			ResetArea(blobCore);
		}

		#endregion

		#region AutoExpand

		private void AutoExpandBlob()
		{
			if(currentStrain.strainType == StrainTypes.NetworkedFibers) return;

			//Node auto expand logic
			foreach (var node in nodeBlobs.Shuffle())
			{
				if(node == null || node.nodeDepleted) continue;

				var coordsLeft = node.expandCoords;

				foreach (var expandCoord in coordsLeft.Shuffle())
				{
					if (!ValidateAction(expandCoord.To3Int(), true))
					{
						continue;
					}

					if (PlaceBlobOrAttack(expandCoord.To3Int(), true))
					{
						node.expandCoords.Remove(expandCoord);
						continue;
					}

					if(rapidExpand) continue;
					break;
				}

				if (node.expandCoords.Count == 0)
				{
					node.nodeDepleted = true;
				}
			}
		}

		private void ResetArea(GameObject node)
		{
			var pos = node.GetComponent<CustomNetTransform>().ServerPosition;
			var structNode = node.GetComponent<BlobStructure>();

			if(!structNode.isCore && !structNode.isNode) return;

			structNode.expandCoords = GenerateCoords(pos);
			structNode.healthPulseCoords = structNode.expandCoords;
			structNode.location = pos;
			structNode.nodeDepleted = false;
		}

		private List<Vector2Int> GenerateCoords(Vector3Int worldPos)
		{
			int r2 = blobSpreadDistance * blobSpreadDistance;
			int area = r2 << 2;
			int rr = blobSpreadDistance << 1;

			var data = new List<Vector2Int>();

			for (int i = 0; i < area; i++)
			{
				int tx = (i % rr) - blobSpreadDistance;
				int ty = (i / rr) - blobSpreadDistance;

				if (tx * tx + ty * ty <= r2)
					data.Add(new Vector2Int(worldPos.x + tx, worldPos.y + ty));
			}

			return data;
		}

		#endregion

		#region FactoryBlob

		private void TrySpawnSpores()
		{
			if (factoryTimer < 10f) return;

			factoryTimer = 0f;

			foreach (var factoryBlob in factoryBlobs)
			{
				if (factoryBlob.Key == null) continue;

				factoryBlob.Value.Remove(null);

				var spores = factoryBlob.Value;

				foreach (var spore in spores)
				{
					if (spore.GetComponent<LivingHealthBehaviour>().IsDead)
					{
						factoryBlob.Value.Remove(spore);
					}
				}

				//Create max of three spore
				if (factoryBlob.Value.Count >= 3) continue;

				var result = Spawn.ServerPrefab(blobSpore, factoryBlob.Key.WorldPosServer(),
					factoryBlob.Key.transform);

				if (!result.Successful) continue;

				result.GameObject.GetComponent<BlobStructure>().overmindName = overmindName;

				factoryBlob.Value.Add(result.GameObject);
			}
		}

		#endregion

		#region Economy

		private void BiomassTick()
		{
			//Gain biomass every 5 seconds
			if (econTimer >= 5f)
			{
				econTimer = 0f;

				//Remove null if possible
				resourceBlobs.Remove(null);

				float numResource = resourceBlobs.Count;
				var coreIncome = 3;

				if (currentStrain.strainType == StrainTypes.NetworkedFibers)
				{
					//Nodes produce 1.5 resources to make up for no expansion
					numResource += nodeBlobs.Count * 1.5f;
					coreIncome = 4;
				}
				else if (currentStrain.strainType == StrainTypes.RegenerativeMateria)
				{
					coreIncome = 4;
				}

				//One biomass for each resource node
				var newBiomass = (numResource + coreIncome) * econModifier; //Base income of three

				AddToResources(newBiomass);
			}
		}

		private float AddToResources(float newBiomass)
		{
			var used = 0f;

			//Reset to max if over
			if (Resources >= maxBiomass)
			{
				Resources = maxBiomass;
				return maxBiomass;
			}
			//Add only to max if it would go above max
			else if (Resources + newBiomass > maxBiomass)
			{
				used = maxBiomass - Resources;
			}
			else
			{
				used = newBiomass;
			}

			Resources += used;
			return used;
		}

		#endregion

		#region Light/Color

		private void SetLightAndColor(BlobStructure blobStructure)
		{
			if (blobStructure.lightSprite != null)
			{
				//TODO needs networking
				blobStructure.lightSprite.Color = currentStrain.color;
				blobStructure.lightSprite.Color.a = 0.2f;
			}

			blobStructure.spriteHandler.SetColor(currentStrain.color);
		}

		#endregion

		#region CountSpace

		private void AddNonSpaceBlob(GameObject newBlob)
		{
			if(MatrixManager.IsSpaceAt(newBlob.GetComponent<RegisterObject>().WorldPositionServer, true)) return;

			nonSpaceBlobTiles.Remove(null);
			nonSpaceBlobTiles.Add(newBlob);
		}

		#endregion

		#region HealthPulse

		private void HealthPulse()
		{
			if(healthTimer <= 10f) return;

			healthTimer = 0f;

			foreach (var node in nodeBlobs)
			{
				if(node == null) continue;

				var healthToRestore = node.isCore ? 3f : 1f;

				if (currentStrain.strainType == StrainTypes.NetworkedFibers && node.isCore)
				{
					healthToRestore *= 2.5f;
				}
				else if (currentStrain.strainType == StrainTypes.RegenerativeMateria && node.isCore)
				{
					healthToRestore *= 10f;
				}

				foreach (var healthPulseTarget in node.healthPulseCoords)
				{
					if (blobTiles.TryGetValue(healthPulseTarget.To3Int(), out var blob) && blob != null)
					{
						if(blob.integrity == null) continue;

						blob.integrity.RestoreIntegrity(healthToRestore);
					}
				}
			}
		}

		#endregion

		#region Strain

		private void SetStrainData(BlobStructure blobStructure)
		{
			if (blobStructure.integrity == null) return;

			blobStructure.integrity.Armor = blobStructure.initialArmor;
			blobStructure.integrity.Resistances = blobStructure.initialResistances;

			if (currentStrain.customArmor)
			{
				blobStructure.integrity.Armor = currentStrain.armor;
			}

			if (currentStrain.customResistances)
			{
				blobStructure.integrity.Resistances = currentStrain.resistances;
			}
		}

		private void UpdateBlobStrain()
		{
			var blobs = blobTiles;

			foreach (var blob in blobs)
			{
				if(blob.Value == null) continue;

				SetStrainData(blob.Value);
				SetLightAndColor(blob.Value);
			}
		}

		[Command]
		public void CmdChangeStrain(int newStrainIndex)
		{
			if (StrainRerolls > 0)
			{
				StrainRerolls -= 1;
			}
			else
			{
				if (resources < adaptStrainCost)
				{
					Chat.AddExamineMsgFromServer(gameObject, "Not enough resources to readapt");
					return;
				}

				resources -= adaptStrainCost;
			}

			Chat.AddExamineMsgFromServer(gameObject, $"You readapt and mutate into the {blobStrains[newStrainIndex].strainName} strain");

			currentStrain = blobStrains[newStrainIndex];
			StrainIndex = newStrainIndex;
			UpdateBlobStrain();

			TargetRpcForceStrainReset(connectionToClient);
		}

		[Command]
		public void CmdRandomiseStrains()
		{
			if (resources < rerollStrainsCost)
			{
				Chat.AddExamineMsgFromServer(gameObject, "Not enough resources to randomise strains");
				return;
			}

			resources -= rerollStrainsCost;

			TargetRpcForceStrainReset(connectionToClient);
		}

		#endregion

		#region Ondamage

		private void SubscribeToDamage(BlobStructure structure)
		{
			if(!CustomNetworkManager.IsServer) return;

			structure.integrity.OnWillDestroyServer.AddListener(BlobTileDeath);

			structure.integrity.OnApplyDamage.AddListener(OnDamageReceived);
		}

		private void OnDamageReceived(DamageInfo info)
		{
			switch (currentStrain.strainType)
			{
				case StrainTypes.ReactiveSpines:
					// Attacks nearby area when hit with melee attacks
					AttackAllSides(info);
					break;
				case StrainTypes.BlazingOil:
					// Emit burst of flame
					EmitFlame(info);
					break;
				case StrainTypes.PressurizedSlime:
					// Releases water when hit
					SetAllSidesSlippy(info);
					break;
				case StrainTypes.ReplicatingFoam:
					// Expands when burned
					ExpandWhenBurnt(info);
					break;
				case StrainTypes.ShiftingFragments:
					// When damaged always swaps positions with a nearby blob
					SwapPositionWithBlob(info);
					break;
				case StrainTypes.SynchronousMesh:
					// Spreads Damage Between Nearby Blobs
					SpreadDamageOut(info);
					break;
				default:
					return;
			}
		}

		private void AttackAllSides(DamageInfo info)
		{
			var pos = info.AttackedIntegrity.gameObject.WorldPosServer();

			foreach (var offset in coords)
			{
				TryAttack(offset + pos, true);
			}
		}

		private void SetAllSidesSlippy(DamageInfo info)
		{
			var registerObject = info.AttackedIntegrity.gameObject.GetComponent<RegisterObject>();
			var pos = registerObject.WorldPositionServer;

			foreach (var offset in coords)
			{
				registerObject.Matrix.MetaDataLayer.MakeSlipperyAt(offset + pos);
			}
		}

		private void ExpandWhenBurnt(DamageInfo info)
		{
			if(info.DamageType != DamageType.Burn && info.AttackType != AttackType.Fire) return;

			var pos = info.AttackedIntegrity.gameObject.WorldPosServer().RoundToInt();

			foreach (var offset in coords)
			{
				PlaceBlobOrAttack(offset + pos, true);
			}
		}

		private void SwapPositionWithBlob(DamageInfo info)
		{
			var pos = info.AttackedIntegrity.gameObject.WorldPosServer().RoundToInt();

			foreach (var offset in coords)
			{
				//Dont swap with self
				if (offset == Vector3Int.zero || !blobTiles.TryGetValue(offset + pos, out var blobStructure)) continue;

				if(blobStructure == null) continue;

				var first = info.AttackedIntegrity.GetComponent<CustomNetTransform>();
				var second = blobStructure.GetComponent<CustomNetTransform>();

				var posCache = pos + offset;

				first.SetPosition(second.ServerPosition);
				blobTiles[second.ServerPosition] = first.GetComponent<BlobStructure>();
				blobTiles[posCache] = blobStructure;
				second.SetPosition(posCache);

				//If moved to node or core refresh areas
				ResetArea(first.gameObject);

				if(!blobStructure.isCore && !blobStructure.isNode) return;

				ResetArea(second.gameObject);
				return;
			}
		}

		private void SpreadDamageOut(DamageInfo info)
		{
			var pos = info.AttackedIntegrity.gameObject.WorldPosServer().RoundToInt();

			List<Integrity> blobIntegrities = new List<Integrity>();

			//Add self
			blobIntegrities.Add(info.AttackedIntegrity);

			foreach (var offset in coords)
			{
				//Dont swap with self
				if (offset == Vector3Int.zero || !blobTiles.TryGetValue(offset + pos, out var blobStructure)) continue;

				if(blobStructure == null) continue;

				blobIntegrities.Add(blobStructure.integrity);
			}

			if(blobIntegrities.Count == 1) return;

			var damage = info.Damage / blobIntegrities.Count;

			//Distribute damage
			foreach (var blob in blobIntegrities)
			{
				blob.ApplyDamage(damage, info.AttackType, info.DamageType, triggerEvent: false);
			}

			//Heal self back up
			info.AttackedIntegrity.RestoreIntegrity(damage * (blobIntegrities.Count - 1));
		}

		private void EmitFlame(DamageInfo info)
		{
			var registerObject = info.AttackedIntegrity.gameObject.GetComponent<RegisterObject>();
			var pos = registerObject.WorldPositionServer;

			foreach (var offset in coords)
			{
				registerObject.Matrix.ReactionManager.ExposeHotspotWorldPosition((offset + pos).To2Int());
			}
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
}