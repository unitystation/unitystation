using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Light2D;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;
using EpPathFinding.cs;
using HealthV2;
using Logs;
using Managers;
using Strings;
using Systems.MobAIs;
using UnityEngine.Profiling;

namespace Blob
{
	/// <summary>
	/// Class which has the logic and data for the blob player
	/// </summary>
	public class BlobPlayer : NetworkBehaviour, IAdminInfo, IGib
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

		public List<BlobStrain> blobStrains = new List<BlobStrain>();

		private BlobStrain currentStrain;

		private BlobStructure blobCore;
		private Integrity coreHealth;
		private UI_Blob uiBlob;

		public int layerDamage = 50;

		public int adaptStrainCost = 40;
		public int rerollStrainsCost = 20;

		private MovementSynchronisation playerSync;
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

		private HashSet<BlobStructure> resourceBlobs = new HashSet<BlobStructure>();

		private ConcurrentDictionary<BlobStructure, HashSet<GameObject>> factoryBlobs =
			new ConcurrentDictionary<BlobStructure, HashSet<GameObject>>();

		private HashSet<BlobStructure> nodeBlobs = new HashSet<BlobStructure>();

		private List<Vector3Int> coords = new List<Vector3Int>
		{
			new Vector3Int(0, 0, 0),
			new Vector3Int(0, 1, 0),
			new Vector3Int(1, 0, 0),
			new Vector3Int(0, -1, 0),
			new Vector3Int(-1, 0, 0)
		};

		private BaseGrid searchGrid;
		private int bottomXCoord;
		private int bottomYCoord;

		[SyncVar(hook = nameof(SyncResources))]
		private float resources = 0;

		[SyncVar(hook = nameof(SyncHealth))]
		private float health = 400f;

		[SyncVar(hook = nameof(SyncNumOfBlobTiles))]
		private int numOfNonSpaceBlobTiles = 1;

		//Amount of free strain rerolls
		[SyncVar(hook = nameof(SyncStrainRerolls))]
		private int strainRerolls = 1;

		[SyncVar(hook = nameof(SyncStrainIndex))]
		private int strainIndex;

		[SyncVar(hook = nameof(SyncTurnOnClientLight))]
		private bool clientLight;

		private BlobStrain clientCurrentStrain;
		public BlobStrain ClientCurrentStrain => clientCurrentStrain;

		private int numOfBlobTiles = 1;

		private int maxCount = 0;

		private int maxNonSpaceCount = 0;

		private bool pathSearch;

		private LayerMask layerMask;
		private LayerMask sporeLayerMask;

		//stores the client linerenderer gameobjects
		private HashSet<GameObject> clientLinerenderers = new HashSet<GameObject>();

		private Collider2D[] sporesArray = new Collider2D[40];

		/// <summary>
		/// The start function of the script called from BlobStarter when player turns into blob, sets up core.
		/// </summary>
		public void BlobStart(Mind mind)
		{
			playerSync = GetComponent<MovementSynchronisation>();
			registerPlayer = GetComponent<RegisterPlayer>();
			playerScript = GetComponent<PlayerScript>();

			if (playerScript == null && (!TryGetComponent(out playerScript) || playerScript == null))
			{
				Loggy.LogError("Playerscript was null on blob and couldnt be found.", Category.Blob);
				return;
			}

			if (mind == null)
			{
				Loggy.LogError("Mind was null on blob and couldnt be found.", Category.Blob);
				return;
			}

			mind.SetPossessingObject(playerScript.gameObject);
			mind.StopGhosting();

			overmindName = $"Overmind {Random.Range(1, 1001)}";

			mind.SetPermanentName(overmindName);

			var result = Spawn.ServerPrefab(blobCorePrefab, registerPlayer.WorldPositionServer, gameObject.transform.parent);

			if (!result.Successful)
			{
				Loggy.LogError("Failed to spawn blob core for player!", Category.Blob);
				return;
			}

			clientLight = true;

			blobCore = result.GameObject.GetComponent<BlobStructure>();

			var pos = blobCore.GetComponent<UniversalObjectPhysics>().transform.position.RoundToInt();

			blobTiles.TryAdd(pos, blobCore);
			nonSpaceBlobTiles.Add(blobCore.gameObject);

			blobCore.location = pos;
			blobCore.overmindName = overmindName;

			currentStrain = blobStrains.PickRandom();
			strainIndex = blobStrains.IndexOf(currentStrain);

			SetStrainData(blobCore);
			SetLightAndColor(blobCore);

			//Make core act like node
			blobCore.expandCoords = GenerateCoords(pos);
			blobCore.healthPulseCoords = blobCore.expandCoords;
			blobCore.connectedToBlobNet = true;
			nodeBlobs.Add(blobCore);

			//Set up death detection
			coreHealth = blobCore.GetComponent<Integrity>();
			coreHealth.OnWillDestroyServer.AddListener(Death);
			SubscribeToDamage(blobCore);

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

		private void Awake()
		{
			playerSync = GetComponent<MovementSynchronisation>();
			registerPlayer = GetComponent<RegisterPlayer>();
			playerScript = GetComponent<PlayerScript>();

			layerMask = LayerMask.GetMask("Objects", "Players", "NPC", "Machines", "Windows", "Door Closed");
			sporeLayerMask = LayerMask.GetMask("NPC");
		}

		private void PeriodicUpdate()
		{
			if (CustomNetworkManager.IsServer == false) return;

			timer += 1f;
			econTimer += 1f;
			factoryTimer += 1f;
			healthTimer += 1f;
			rerollTimer += 1f;

			// Force overmind back to blob if camera moves too far
			if (!teleportCheck && !victory && !ValidateAction(playerSync.registerTile.WorldPosition, true) && blobCore != null)
			{
				teleportCheck = true;

				StartCoroutine(TeleportPlayerBack());
			}

			nonSpaceBlobTiles.Remove(null);

			// Count number of blob tiles
			numOfNonSpaceBlobTiles = nonSpaceBlobTiles.Count;
			numOfBlobTiles = blobTiles.Count;

			if (numOfBlobTiles > maxCount)
			{
				maxCount = numOfBlobTiles;
			}

			if (numOfNonSpaceBlobTiles > maxNonSpaceCount)
			{
				maxNonSpaceCount = numOfNonSpaceBlobTiles;
			}

			if (isBlobGamemode && !halfWay && numOfNonSpaceBlobTiles >= numOfTilesForVictory / 2)
			{
				halfWay = true;

				Chat.AddSystemMsgToChat(
					string.Format(ReportTemplates.BioHazard,
						"Caution! Biohazard expanding rapidly. Station structural integrity failing."),
					MatrixManager.MainStationMatrix);
				_ = SoundManager.PlayNetworked(CommonSounds.Instance.Notice1);
			}

			if (isBlobGamemode && !nearlyWon && numOfNonSpaceBlobTiles >= numOfTilesForVictory / 1.25)
			{
				nearlyWon = true;

				Chat.AddSystemMsgToChat(
					string.Format(ReportTemplates.BioHazard,
						"Alert! Station integrity near critical. Biomass sensor levels are off the charts."),
					MatrixManager.MainStationMatrix);
				_ = SoundManager.PlayNetworked(CommonSounds.Instance.Notice1);
			}

			// Blob wins after number of blob tiles reached
			if (!victory && numOfNonSpaceBlobTiles >= numOfTilesForVictory)
			{
				victory = true;
				BlobWins();
			}

			// Detection check
			if (!announcedBlob && (timer >= announceAfterSeconds || numOfBlobTiles >= numOfTilesForDetection))
			{
				announcedBlob = true;

				Chat.AddSystemMsgToChat(
					string.Format(ReportTemplates.BioHazard,
						"Confirmed outbreak of level 5 biohazard aboard the station. All personnel must contain the outbreak."),
					MatrixManager.MainStationMatrix);
				_ = SoundManager.PlayNetworked(CommonSounds.Instance.Outbreak5Announcement);
			}

			if (rerollTimer > 300f)
			{
				rerollTimer = 0f;

				strainRerolls += 1;
			}

			CheckConnections();

			BiomassTick();

			TrySpawnSpores();

			AutoExpandBlob();

			HealthPulse();

			if (blobCore == null) return;

			health = coreHealth.integrity;
		}

		#region teleport

		private IEnumerator TeleportPlayerBack()
		{
			Chat.AddExamineMsgFromServer(gameObject,
				"You feel lost without blob.<color=#FF151F>Move back to the blob</color>");

			yield return WaitFor.Seconds(1f);

			if (ValidateAction(playerSync.registerTile.WorldPosition, true))
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

			playerSync.AppearAtWorldPositionServer(blobCore.location);
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

			var pos = playerSync.registerTile.WorldPosition;

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

				playerSync.AppearAtWorldPositionServer(blobCore.location);
				return;
			}

			playerSync.AppearAtWorldPositionServer(node.AssumedWorldPosServer());
		}

		#endregion

		#region SyncVar Hooks

		[Client]
		private void SyncTurnOnClientLight(bool oldVar, bool newVar)
		{
			clientLight = newVar;
			if (newVar == false) return;

			SetBlobUI();
			TurnOnClientLight();
		}

		[Client]
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

		[Client]
		private void SyncResources(float oldVar, float newVar)
		{
			SetBlobUI();

			resources = newVar;
			uiBlob.resourceText.text = Mathf.FloorToInt(newVar).ToString();
		}

		[Client]
		private void SyncHealth(float oldVar, float newVar)
		{
			SetBlobUI();

			health = newVar;
			uiBlob.healthText.text = newVar.ToString();
		}

		[Client]
		private void SyncNumOfBlobTiles(int oldVar, int newVar)
		{
			SetBlobUI();

			numOfBlobTiles = newVar;
			uiBlob.numOfBlobTilesText.text = newVar.ToString();
		}

		[Client]
		private void SyncStrainRerolls(int oldVar, int newVar)
		{
			SetBlobUI();

			strainRerolls = newVar;
			uiBlob.strainRerollsText.text = newVar.ToString();
		}

		[Client]
		private void SyncStrainIndex(int oldVar, int newVar)
		{
			SetBlobUI();

			strainIndex = newVar;
			clientCurrentStrain = blobStrains[newVar];
			uiBlob.UpdateStrainInfo();
			TurnOnClientLight();
		}

		private void SetBlobUI()
		{
			if (uiBlob == null)
			{
				uiBlob = UIManager.Display.hudBottomBlob.GetComponent<UI_Blob>();
				uiBlob.blobPlayer = this;
				uiBlob.controller = GetComponent<BlobMouseInputController>();
			}
		}

		#endregion

		#region TargetRPCs

		[TargetRpc]
		private void TargetRpcForceStrainReset(NetworkConnection target)
		{
			var strains = blobStrains.Where(s => s != clientCurrentStrain);
			uiBlob.randomStrains = strains.PickRandom(4).ToList();

			uiBlob.UpdateStrainInfo();
		}

		[TargetRpc]
		private void TargetRpcSetLineRender(NetworkConnection target, Vector3 worldPos, Vector3[] positions)
		{
			var blobs = MatrixManager.GetAt<BlobStructure>(worldPos.RoundToInt(), false);

			foreach (var blob in blobs)
			{
				if(blob.lineRenderer == null) continue;

				blob.lineRenderer.positionCount = positions.Length;
				blob.lineRenderer.SetPositions(positions);
				blob.lineRenderer.enabled = uiBlob.blobnet;

				clientLinerenderers.Add(blob.gameObject);
			}
		}

		[TargetRpc]
		private void TargetRpcTurnOffLight(NetworkConnection target)
		{
			overmindLightObject.SetActive(false);
			overmindSprite.layer = 31;
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
			//Whether player can click anywhere, or if when they click it treats it as if they clicked the tile they're
			//standing on (or around since validation checks adjacent)
			if (!clickCoords)
			{
				worldPos = playerSync.registerTile.WorldPosition;
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

			if (!autoExpanding && (resources < attackCost || resources < normalBlobCost))
			{
				Chat.AddExamineMsgFromServer(gameObject,
					$"Not enough biomass to attack or grow, you need {attackCost} biomass to attack and {normalBlobCost} biomass to grow");
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

				resources -= attackCost;
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

			var result = Spawn.ServerPrefab(blobNormalPrefab, worldPos, gameObject.transform.parent);

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
				Loggy.LogError("matrix for blob click was null", Category.Blob);
				return false;
			}

			var metaTileMap = matrix.MetaTileMap;

			var pos = worldPos.RoundToInt();

			var objects = Physics2D.OverlapBoxAll(pos.To2Int(), new Vector2(0.8f, 0.8f), 0, layerMask);

			foreach (var hitObject in objects)
			{
				if (hitObject.TryGetComponent<LivingHealthMasterBase>(out var npcPlayerComponent))
				{
					if(npcPlayerComponent.IsDead) continue;

					foreach (var playerDamage in currentStrain.playerDamages)
					{
						npcPlayerComponent.ApplyDamageToBodyPart(gameObject, playerDamage.damageDone, AttackType.Melee, playerDamage.damageType);
					}

					PlayAttackEffect(pos);

					return true;
				}

				//If you're are passable, dont need to damage
				if (hitObject.TryGetComponent<RegisterTile>(out var registerTile) && registerTile.IsPassable(true)) continue;

				//If you're are allied blob dont damage
				if(hitObject.TryGetComponent<BlobStructure>(out var structure) && structure.overmindName == overmindName) continue;

				if (hitObject.TryGetComponent<Integrity>(out var integrity) && !integrity.Resistances.Indestructable)
				{
					foreach (var objectDamage in currentStrain.objectDamages)
					{
						integrity.ApplyDamage(objectDamage.damageDone, AttackType.Melee, objectDamage.damageType, true);
					}

					PlayAttackEffect(pos);

					return true;
				}
			}

			//Check for walls, windows and grills if we didnt attack anything else
			if (metaTileMap != null && !MatrixManager.IsPassableAtAllMatricesOneTile(pos, true, ignoreObjects: true))
			{
				metaTileMap.ApplyDamage(worldPos.ToLocal().RoundToInt(), layerDamage, pos);

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

					specialisedBlobs.Add(resourceBlob.location);
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

			var corePos = blobCore.location;

			foreach (var offSet in coords)
			{
				if (pos == offSet + corePos)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Check to see if theres any blob next to it
		/// </summary>
		/// <param name="worldPos"></param>
		/// <param name="noMsg"></param>
		/// <returns></returns>
		private bool ValidateNextToBlob(Vector3 worldPos)
		{
			var pos = worldPos.RoundToInt();

			foreach (var offSet in coords)
			{
				if (pos + offSet != pos && blobTiles.ContainsKey(pos + offSet))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Check to see if all blobs are still in main dictionary
		/// </summary>
		/// <param name="worldPos"></param>
		/// <param name="noMsg"></param>
		/// <returns></returns>
		private bool ValidateBlobPath(List<Vector3Int> fullPath)
		{
			if (fullPath.Count == 0) return false;

			foreach (var position in fullPath)
			{
				if (blobTiles.ContainsKey(position)) continue;

				return false;
			}

			return true;
		}

		#endregion

		#region Effects

		private void PlayAttackEffect(Vector3 worldPos)
		{
			RpcPlayEffect(worldPos);

			if (CustomNetworkManager.IsHeadless) return;
			PlayEffect(worldPos);
		}

		[ClientRpc]
		private void RpcPlayEffect(Vector3 worldPos)
		{
			PlayEffect(worldPos);
		}

		private void PlayEffect(Vector3 worldPos)
		{
			Spawn.ClientPrefab(attackEffect, worldPos, gameObject.transform.parent);
		}

		#endregion

		#region PlaceStrong/Reflective

		[Command]
		public void CmdTryPlaceStrongReflective(Vector3Int worldPos)
		{
			if (!ValidateAction(worldPos)) return;

			if (blobTiles.TryGetValue(worldPos, out var blob) && blob != null)
			{
				//Try place strong
				if (blob != null && blob.blobType == BlobConstructs.Normal)
				{
					PlaceStrongReflective(blobStrongPrefab, blob, strongBlobCost, worldPos, "You grow a strong blob, you can now mutate to a reflective.");
					return;
				}

				//Try place reflective
				if (blob != null && blob.blobType == BlobConstructs.Strong)
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
			if (ValidateCost(cost, prefab) == false) return;

			var result = Spawn.ServerPrefab(prefab, worldPos, gameObject.transform.parent);

			if (result.Successful == false) return;

			Chat.AddExamineMsgFromServer(gameObject, msg);

			_ = Despawn.ServerSingle(originalBlob.gameObject);

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
			if (!ValidateAction(worldPos)) return;

			if (MatrixManager.IsSpaceAt(worldPos, true, registerPlayer.Matrix.MatrixInfo))
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
					Loggy.LogError("Switch has no correct case for blob structure!", Category.Blob);
					break;
			}

			if (prefab == null) return;

			if (blobTiles.TryGetValue(worldPos, out var blob) && blob != null)
			{
				if (blob != null && blob.blobType == BlobConstructs.Normal)
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

					var result = Spawn.ServerPrefab(prefab, worldPos, gameObject.transform.parent);

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
							factoryBlobs.TryAdd(structure, new HashSet<GameObject>());
							break;
						case BlobConstructs.Resource:
							resourceBlobs.Add(structure);
							break;
						default:
							Loggy.LogError("Switch has no correct case for blob structure!", Category.Blob);
							break;
					}

					_ = Despawn.ServerSingle(blob.gameObject);

					structure.location = worldPos;
					SetStrainData(structure);
					SetLightAndColor(structure);
					SubscribeToDamage(structure);

					blobTiles[worldPos] = structure;
					AddNonSpaceBlob(result.GameObject);
					return;
				}
			}

			// No normal blob at tile
			Chat.AddExamineMsgFromServer(gameObject, "You need to place a normal blob first");
		}

		#endregion

		#region BlobTileDeath

		private void BlobTileDeath(DestructionInfo info)
		{
			nonSpaceBlobTiles.Remove(info.Destroyed.gameObject);

			if (blobTiles.TryRemove(info.Destroyed.GetComponent<BlobStructure>().location, out var removed))
			{
				removed.connectedToBlobNet = false;
				nodeBlobs.Remove(removed);
				resourceBlobs.Remove(removed);
			}

			factoryBlobs.TryRemove(info.Destroyed.gameObject.GetComponent<BlobStructure>(), out var spores);

			info.Destroyed.OnWillDestroyServer.RemoveListener(BlobTileDeath);
		}

		#endregion

		#region RemoveBlob

		[Command]
		public void CmdRemoveBlob(Vector3Int worldPos)
		{
			InternalRemoveBlob(worldPos);
		}

		private void InternalRemoveBlob(Vector3Int worldPos)
		{
			if (blobTiles.TryGetValue(worldPos, out var blob) == false || blob == null)
			{
				Chat.AddExamineMsgFromServer(gameObject, "No blob to be removed");
			}
			else
			{
				var returnCost = 0;

				switch (blob.blobType)
				{
					case BlobConstructs.Normal:
						returnCost = Mathf.RoundToInt(normalBlobCost * refundPercentage);
						break;
					case BlobConstructs.Factory:
						returnCost = Mathf.RoundToInt(factoryBlobCost * refundPercentage);
						break;
					case BlobConstructs.Resource:
						returnCost = Mathf.RoundToInt(resourceBlobCost * refundPercentage);
						break;
					case BlobConstructs.Strong:
						returnCost = Mathf.RoundToInt(strongBlobCost * refundPercentage);
						break;
					case BlobConstructs.Reflective:
						returnCost = Mathf.RoundToInt(reflectiveBlobCost * refundPercentage);
						break;
					case BlobConstructs.Core:
						Chat.AddExamineMsgFromServer(gameObject, "This is a blob core. It cannot be removed");
						return;
					case BlobConstructs.Node:
						Chat.AddExamineMsgFromServer(gameObject, "This is a blob node. It cannot be removed");
						return;
					default:
						Loggy.LogError("Switch has no correct case for blob structure!", Category.Blob);
						break;
				}

				Chat.AddExamineMsgFromServer(gameObject, $"Blob removed, {AddToResources(returnCost)} biomass refunded");

				nonSpaceBlobTiles.Remove(blob.gameObject);
				_ = Despawn.ServerSingle(blob.gameObject);
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
			if (CustomNetworkManager.IsServer == false || hasDied) return;

			hasDied = true;

			coreHealth.OnWillDestroyServer.RemoveListener(Death);

			// Destroy all blob tiles
			foreach (var tile in blobTiles)
			{
				if (tile.Value != null)
				{
					_ = Despawn.ServerSingle(tile.Value.gameObject);
				}
			}

			blobTiles = new ConcurrentDictionary<Vector3Int, BlobStructure>();

			GameManager.Instance.PrimaryEscapeShuttle.SetHostileEnvironment(false);

			Chat.AddSystemMsgToChat(
				string.Format(ReportTemplates.BioHazard,
					"The biohazard has been contained."),
				MatrixManager.MainStationMatrix);

			clientLight = false;

			if (connectionToClient != null)
			{
				TargetRpcTurnOffLight(connectionToClient);
			}

			//Make blob into ghost
			playerScript.Mind.Ghost();

			if (endRoundWhenKilled)
			{
				GameManager.Instance.RoundEndTime = 60;
				GameManager.Instance.EndRound();
			}

			_ = Despawn.ServerSingle(gameObject);
		}

		public void OnGib()
		{
			Death();
		}

		#endregion

		#region Victory

		private void BlobWins()
		{
			GameManager.Instance.CentComm.ChangeAlertLevel(CentComm.AlertLevel.Delta);

			Chat.AddSystemMsgToChat(
				string.Format(ReportTemplates.BioHazard,
					"Biohazard has reached critical mass. Station integrity critical!"),
				MatrixManager.MainStationMatrix);

			Chat.AddExamineMsgFromServer(gameObject, "Your hunger is unstoppable, you are fully unleashed");

			maxBiomass = 5000;

			econModifier = 10f;

			rapidExpand = true;

			foreach (var objective in playerScript.Mind.GetAntag().Objectives)
			{
				objective.SetAsComplete();
			}

			if (endRoundWhenBlobVictory)
			{
				Chat.AddGameWideSystemMsgToChat("The blob has consumed the station, we are all but goo now.");

				Chat.AddGameWideSystemMsgToChat($"At its biggest the blob had {maxCount} tiles controlled" +
				                                $" but only had {maxNonSpaceCount} non-space tiles which counted to victory.");

				GameManager.Instance.RoundEndTime = 60;
				GameManager.Instance.EndRound();
			}
		}

		#endregion

		#region SwitchCore

		[Command]
		public void CmdMoveCore(Vector3Int worldPos)
		{
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
			if (oldNode.blobType != BlobConstructs.Node)
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

			var core = blobCore.GetComponent<UniversalObjectPhysics>();
			var node = oldNode.GetComponent<UniversalObjectPhysics>();

			var coreCache = core.transform.position.RoundToInt();

			var position = node.transform.position;
			core.AppearAtWorldPositionServer(position);
			blobTiles[position.RoundToInt()] = blobCore;
			blobCore.location = node.transform.position.RoundToInt();

			blobTiles[coreCache] = oldNode;
			oldNode.location = coreCache;
			node.AppearAtWorldPositionServer(coreCache);

			ResetArea(blobCore.gameObject);
			ResetArea(oldNode.gameObject);
		}

		/// <summary>
		/// Moves a core to a normal blob
		/// </summary>
		/// <param name="oldNode"></param>
		public void MoveCoreToNormalBlob(BlobStructure oldNormal)
		{
			if (oldNormal.blobType != BlobConstructs.Normal)
			{
				Chat.AddExamineMsgFromServer(gameObject, "Can only move the core to a normal blob");
				return;
			}

			var core = blobCore.GetComponent<UniversalObjectPhysics>();
			var normal = oldNormal.GetComponent<UniversalObjectPhysics>();

			var coreCache = core.transform.position.RoundToInt();

			core.AppearAtWorldPositionServer(normal.transform.position);
			blobTiles[normal.transform.position.RoundToInt()] = blobCore;
			blobCore.location = normal.transform.position.RoundToInt();

			blobTiles[coreCache] = oldNormal;
			oldNormal.location = coreCache;
			normal.AppearAtWorldPositionServer(coreCache);

			ResetArea(blobCore.gameObject);
		}

		#endregion

		#region AutoExpand

		private void AutoExpandBlob()
		{
			if(currentStrain.strainType == StrainTypes.NetworkedFibers) return;

			//Node auto expand logic
			foreach (var node in nodeBlobs.Shuffle())
			{
				if(node == null || node.nodeDepleted || !node.connectedToBlobNet) continue;

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
			var pos = node.GetComponent<UniversalObjectPhysics>().transform.position;
			var structNode = node.GetComponent<BlobStructure>();

			if(structNode.blobType != BlobConstructs.Core && structNode.blobType != BlobConstructs.Node) return;

			structNode.expandCoords = GenerateCoords(pos.RoundToInt());
			structNode.healthPulseCoords = structNode.expandCoords;
			structNode.location = pos.RoundToInt();
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
				factoryBlob.Value.RemoveWhere(spore => spore.GetComponent<LivingHealthBehaviour>().IsDead);

				//Dont produce spores unless connected
				if(!factoryBlob.Key.connectedToBlobNet) continue;

				//Create max of three spore
				if (factoryBlob.Value.Count >= 3) continue;

				var result = Spawn.ServerPrefab(blobSpore, factoryBlob.Key.location,
					factoryBlob.Key.transform.parent);

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

				//Base income
				var coreIncome = 3;

				resourceBlobs.Remove(null);

				//Connected resource income
				float numResource = resourceBlobs.Count(r => r.connectedToBlobNet);

				if (currentStrain.strainType == StrainTypes.NetworkedFibers)
				{
					//Connected nodes produce 1.5 resources to make up for no expansion
					numResource += nodeBlobs.Count(r => r.connectedToBlobNet) * 1.5f;
					coreIncome = 4;
				}
				else if (currentStrain.strainType == StrainTypes.RegenerativeMateria)
				{
					coreIncome = 4;
				}

				//One biomass for each resource node
				var newBiomass = (numResource + coreIncome) * econModifier;

				AddToResources(newBiomass);
			}
		}

		private float AddToResources(float newBiomass)
		{
			var used = 0f;

			//Reset to max if over
			if (resources >= maxBiomass)
			{
				resources = maxBiomass;
				return maxBiomass;
			}
			//Add only to max if it would go above max
			else if (resources + newBiomass > maxBiomass)
			{
				used = maxBiomass - resources;
			}
			else
			{
				used = newBiomass;
			}

			resources += used;
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

			blobStructure.spriteHandler.SetSpriteSO(blobStructure.activeSprite, newVariantIndex: Random.Range(0, blobStructure.activeSprite.Variance.Count));
			blobStructure.spriteHandler.SetColor(currentStrain.color);
		}

		#endregion

		#region CountSpace

		private void AddNonSpaceBlob(GameObject newBlob)
		{
			if(MatrixManager.IsSpaceAt(newBlob.GetComponent<RegisterObject>().WorldPositionServer, true, registerPlayer.Matrix.MatrixInfo)) return;

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
				if(node == null || !node.connectedToBlobNet) continue;

				var healthToRestore = node.blobType == BlobConstructs.Core ? 3f : 1f;

				if (currentStrain.strainType == StrainTypes.NetworkedFibers && node.blobType == BlobConstructs.Core)
				{
					healthToRestore *= 2.5f;
				}
				else if (currentStrain.strainType == StrainTypes.RegenerativeMateria && node.blobType == BlobConstructs.Core)
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
			if (strainRerolls > 0)
			{
				strainRerolls -= 1;
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
			strainIndex = newStrainIndex;
			UpdateBlobStrain();

			if(connectionToClient == null) return;
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

			if(connectionToClient == null) return;
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
					break;
			}

			//If less than 50% health set to damaged sprite for Normal,Strong and reflective
			if (info.AttackedIntegrity.integrity < info.AttackedIntegrity.initialIntegrity / 2
			    && info.AttackedIntegrity.integrity > 0
			    && info.AttackedIntegrity.gameObject.TryGetComponent<BlobStructure>(out var blob)
			    && blob != null
			    && (blob.blobType == BlobConstructs.Normal
			        || blob.blobType == BlobConstructs.Strong
			        || blob.blobType == BlobConstructs.Reflective))
			{
				blob.spriteHandler.SetSpriteSO(blob.inactiveSprite);
			}
		}

		private void AttackAllSides(DamageInfo info)
		{
			var pos = info.AttackedIntegrity.RegisterTile.WorldPositionServer;

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

			var pos = info.AttackedIntegrity.gameObject.AssumedWorldPosServer().RoundToInt();

			foreach (var offset in coords)
			{
				PlaceBlobOrAttack(offset + pos, true);
			}
		}

		private void SwapPositionWithBlob(DamageInfo info)
		{
			var pos = info.AttackedIntegrity.gameObject.AssumedWorldPosServer().RoundToInt();

			foreach (var offset in coords)
			{
				//Dont swap with self
				if (offset == Vector3Int.zero || !blobTiles.TryGetValue(offset + pos, out var blobStructure)) continue;

				if(blobStructure == null) continue;

				var first = info.AttackedIntegrity.GetComponent<UniversalObjectPhysics>();
				var second = blobStructure.GetComponent<UniversalObjectPhysics>();

				var posCache = pos + offset;

				first.AppearAtWorldPositionServer(second.transform.position);
				blobTiles[second.transform.position.RoundToInt()] = first.GetComponent<BlobStructure>();
				blobTiles[posCache] = blobStructure;
				second.AppearAtWorldPositionServer(posCache);

				//If moved to node or core refresh areas
				ResetArea(first.gameObject);

				if(blobStructure.blobType != BlobConstructs.Core && blobStructure.blobType != BlobConstructs.Node) return;

				ResetArea(second.gameObject);
				return;
			}
		}

		private void SpreadDamageOut(DamageInfo info)
		{
			var pos = info.AttackedIntegrity.gameObject.AssumedWorldPosServer().RoundToInt();

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
				registerObject.Matrix.ReactionManager.ExposeHotspotWorldPosition((offset + pos).To2Int(), 500);
			}
		}

		#endregion

		#region Pathfinding

		private void GenerateGrid()
		{
			bottomXCoord = blobTiles.Keys.Min(v => v.x);
			bottomYCoord = blobTiles.Keys.Min(v => v.y);

			List<GridPos> walkableGridPosList= new List<GridPos>();

			foreach (var blob in blobTiles.Keys)
			{
				walkableGridPosList.Add(new GridPos(blob.x - bottomXCoord, blob.y - bottomYCoord));
			}

			searchGrid = new DynamicGrid(walkableGridPosList);
		}

		private bool PathSearch(Vector2Int startPosVector, Vector2Int endPosVector, BlobStructure blobStructure = null)
		{
			var startPos = new GridPos(startPosVector.x - bottomXCoord, startPosVector.y - bottomYCoord);
			var endPos = new GridPos(endPosVector.x - bottomXCoord, endPosVector.y - bottomYCoord);

			JumpPointParam jpParam = new JumpPointParam(searchGrid, startPos, endPos,
				EndNodeUnWalkableTreatment.DISALLOW, DiagonalMovement.Never);

			List<GridPos> resultPathList = JumpPointFinder.FindPath(jpParam);

			if (resultPathList != null && blobStructure != null && blobStructure.lineRenderer != null)
			{
				var positions = new Vector3[resultPathList.Count];

				for (int i = 0; i < resultPathList.Count; i++)
				{
					positions[i] = new Vector3(resultPathList[i].x + bottomXCoord, resultPathList[i].y + bottomYCoord, 0);
				}

				if(connectionToClient != null)
				{
					TargetRpcSetLineRender(connectionToClient, blobStructure.location, positions);
				}
			}

			if (resultPathList?.Count > 0)
			{
				if (blobStructure != null)
				{
					var fullPath = JumpPointFinder.GetFullPath(resultPathList);

					var positions = new List<Vector3Int>();

					for (int i = 0; i < fullPath.Count; i++)
					{
						positions.Add(new Vector3Int(fullPath[i].x + bottomXCoord, fullPath[i].y + bottomYCoord, 0));
					}

					blobStructure.connectedPath = positions;
				}

				return true;
			}

			return false;
		}

		/// <summary>
		/// Toggles the cached line renderers for the client
		/// </summary>
		/// <param name="toggle"></param>
		[Client]
		public void ToggleLineRenderers(bool toggle)
		{
			foreach (var lineRendererObject in clientLinerenderers)
			{
				if(lineRendererObject == null || !lineRendererObject.TryGetComponent<LineRenderer>(out var lineRenderer)) continue;

				lineRenderer.enabled = toggle;
			}
		}

		#endregion

		#region PathCheck

		private void CheckConnections()
		{
			if (pathSearch || blobTiles.Count <= 0) return;
			pathSearch = true;

			StartCoroutine(CheckPaths());
		}

		private IEnumerator CheckPaths()
		{
			try
			{
				Profiler.BeginSample("Blob Grid");
				GenerateGrid();
				Profiler.EndSample();

				Profiler.BeginSample("Blob paths");
				NodePaths();
				EcoPaths();
				FactoryPaths();
				Profiler.EndSample();
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
			}

			pathSearch = false;
			yield break;
		}

		private void NodePaths()
		{
			var sortedNodes = nodeBlobs.OrderBy(n => Vector3.Distance(n.location, blobCore.location));

			//Do closets nodes to core first
			foreach (var blob in sortedNodes)
			{
				if(blob == null || blob == blobCore) continue;

				//If not next to any blob then failed anyway, dont need to check
				if (!ValidateNextToBlob(blob.location))
				{
					ResetConnection(blob);
					continue;
				}

				//If the last connected path still exists dont need to check until it has changed
				if (ValidateBlobPath(blob.connectedPath) && blob.connectedNode != null)
				{
					//If we're still connected to a valid node then that node connection state is ours too
					blob.connectedToBlobNet = blob.connectedNode.connectedToBlobNet;

					blob.spriteHandler.SetSpriteSO(blob.connectedNode.connectedToBlobNet
						? blob.activeSprite
						: blob.inactiveSprite);

					continue;
				}

				var foundConnection = false;

				var nodesClosest = nodeBlobs.OrderBy(n => Vector3.Distance(n.location, blob.location));

				//Try to connect to nearest node if possible
				foreach (var node in nodesClosest)
				{
					if(node == null || blob == node) continue;

					//Dont connect each way
					if(node.connectedNode == blob) continue;

					searchGrid.Reset();

					if (PathSearch(blob.location.To2Int(), node.location.To2Int(), blob))
					{
						blob.connectedNode = node;
						blob.connectedToBlobNet = node.connectedToBlobNet;
						blob.spriteHandler.SetSpriteSO(blob.activeSprite);

						foundConnection = true;
						break;
					}
				}

				//If no connection disable abilities
				if (!foundConnection)
				{
					blob.connectedNode = null;
					blob.connectedToBlobNet = false;
					blob.connectedPath.Clear();
					blob.spriteHandler.SetSpriteSO(blob.inactiveSprite);
				}
			}
		}

		private void EcoPaths()
		{
			foreach (var blob in resourceBlobs)
			{
				CheckPath(blob);
			}
		}

		private void FactoryPaths()
		{
			foreach (var blob in factoryBlobs)
			{
				CheckPath(blob.Key);
			}
		}

		private void CheckPath(BlobStructure blob)
		{
			if(blob == null) return;

			//If not next to any blob then failed anyway, dont need to check
			if (!ValidateNextToBlob(blob.location))
			{
				ResetConnection(blob);
				return;
			}

			//If the last connected path still exists dont need to check until it has changed
			if (ValidateBlobPath(blob.connectedPath) && blob.connectedNode != null)
			{
				//If we're still connected to a valid node then that node connection state is ours too
				blob.connectedToBlobNet = blob.connectedNode.connectedToBlobNet;

				blob.spriteHandler.SetSpriteSO(blob.connectedNode.connectedToBlobNet
					? blob.activeSprite
					: blob.inactiveSprite);

				return;
			}

			var nodesClosest = nodeBlobs.OrderBy(n => Vector3.Distance(n.location, blob.location));

			var foundConnection = false;

			//Try to connect to nearest node if possible
			foreach (var node in nodesClosest)
			{
				if(node == null) continue;

				searchGrid.Reset();

				if (PathSearch(blob.location.To2Int(), node.location.To2Int(), blob))
				{
					foundConnection = true;

					blob.connectedNode = node;
					blob.connectedToBlobNet = node.connectedToBlobNet;
					blob.spriteHandler.SetSpriteSO(blob.activeSprite);
					break;
				}
			}

			if (!foundConnection)
			{
				ResetConnection(blob);
			}
		}

		/// <summary>
		/// Resets line renderer and connection for specific blob structure
		/// </summary>
		/// <param name="blob"></param>
		private void ResetConnection(BlobStructure blob)
		{
			if (connectionToClient != null)
			{
				TargetRpcSetLineRender(connectionToClient, blob.location, new Vector3[0]);
			}

			blob.connectedToBlobNet = false;
			blob.connectedNode = null;
			blob.connectedPath.Clear();
			blob.spriteHandler.SetSpriteSO(blob.inactiveSprite);
		}

		#endregion

		#region Rally

		[Command]
		public void CmdRally(Vector3Int worldPos)
		{
			var count = 0;

			//15 tile radius
			var amount = Physics2D.OverlapCircleNonAlloc(worldPos.To2(), 15, sporesArray, sporeLayerMask);

			for (int i = 0; i < amount; i++)
			{
				var spore = sporesArray[i];
				if (spore == null) continue;
				if (spore.TryGetComponent<BlobAI>(out var blobAI) == false) continue;

				//Only command our spores
				if(blobAI.BlobStructure.overmindName != overmindName) continue;

				blobAI.SetTarget(worldPos);
				count++;
			}

			Chat.AddExamineMsgFromServer(gameObject, $"You command {count} of your underlings to move!");
		}

		#endregion

		public string AdminInfoString()
		{
			var adminInfo = new StringBuilder();

			adminInfo.AppendLine($"Resources: {resources}");
			adminInfo.AppendLine($"Health: {health}%");
			adminInfo.AppendLine($"Victory: {numOfNonSpaceBlobTiles / numOfTilesForVictory}%");

			return adminInfo.ToString();
		}
	}

	public enum BlobConstructs
	{
		Core,
		Node,
		Resource,
		Factory,
		Strong,
		Reflective,
		Normal,
		Rally
	}
}
