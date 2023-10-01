using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Systems.Electricity;
using Systems.MobAIs;
using Managers;
using Messages.Server;
using Mirror;
using Objects;
using Objects.Engineering;
using Objects.Research;
using UI.Systems.MainHUD.UI_Bottom;
using UnityEngine;
using HealthV2;

namespace Systems.Ai
{
	/// <summary>
	/// Main class controlling player job AI logic
	/// This isn't the class which is on the AiCore or InteliCard that is AiVessel
	/// Sync vars in this class only get sync'd to the object owner
	/// </summary>
	public class AiPlayer : NetworkBehaviour, IAdminInfo, IFullyHealable, IGib
	{
		[SerializeField]
		private GameObject corePrefab = null;


		public List<BrainLaws> LinkedCyborgs = new List<BrainLaws>();


		[SyncVar(hook = nameof(SyncCore))]
		//Ai core or card
		private NetworkIdentity IDvesselObject;

		private GameObject vesselObject
		{
			get => IDvesselObject.OrNull()?.gameObject;
			set => SyncCore(IDvesselObject, value.NetWorkIdentity());
		}


		public GameObject VesselObject => vesselObject;

		[SerializeField]
		private int interactionDistance = 29;
		public int InteractionDistance => interactionDistance;

		[SerializeField]
		private GameObject mainSprite = null;

		[SerializeField]
		private List<AiLawSet> defaultLawSets = new List<AiLawSet>();

		//Only valid on core not card
		private SecurityCamera coreCamera = null;
		public SecurityCamera CoreCamera => coreCamera;

		//Valid client side and serverside for validations
		//Client sends message to where it wants to go, server keeps track to do validations
		private Transform cameraLocation;
		public Transform CameraLocation => cameraLocation;

		private PlayerScript playerScript;
		public PlayerScript PlayerScript => playerScript;

		private HasCooldowns cooldowns;

		private LightingSystem lightingSystem;

		private LineRenderer lineRenderer;

		//Clientside only
		private UI_Ai aiUi;

		private bool hasDied;
		public bool HasDied => hasDied;

		[SyncVar(hook = nameof(SyncPowerState))]
		private bool hasPower;

		[SyncVar(hook = nameof(SyncPower))]
		private float power;

		[SyncVar(hook = nameof(SyncIntegrity))]
		private float integrity = StartingIntegrity;
		public float Integrity => integrity;

		public static readonly float StartingIntegrity = 100;

		[SyncVar(hook = nameof(SyncNumberOfCameras))]
		private uint numberOfCameras = 100;

		//Client and server accurate
		private bool isCarded;
		public bool IsCarded => isCarded;

		//Whether the Ai is allowed to perform actions, changed when Ai is carded
		[SyncVar]
		private bool allowRemoteAction = true;
		public bool AllowRemoteAction => allowRemoteAction;

		//Whether the Ai is allowed to use the radio
		[SyncVar]
		private bool allowRadio = true;
		public bool AllowRadio => allowRadio;

		private bool isPurging;
		public bool IsPurging => isPurging;

		//Remove one integrity every 1 second
		private const float purgeDamageInterval = 1f;

		private bool tryingToRestorePower;
		private Coroutine routine;

		private bool isMalf = false;

		public bool IsMalf
		{
			get => isMalf;
			set => isMalf = value;
		}

		//TODO make into sync list, will need to be sync as it is used in some validations client and serverside
		private List<string> openNetworks = new List<string>()
		{
			"Station"
		};
		public List<string> OpenNetworks => openNetworks;

		// 	Law priority order is this:
		//	0: Traitor/Malf/Onehuman-board Law
		//  ##?$-##: HACKED LAW ##!£//#
		//  ##!£//#: Ion Storm Law ##?$-##
		//	Law 1: First Law
		//	Law 2: Second Law
		//	Law 3: Third Law
		//	Law 4: Freeform
		//	Higher laws (the law above each one) override all lower ones. Whether numbered or not, how they appear (in order) is the order of priority.

		//Is sync'd manually to owner client so is accurate on owner client
		//Tried to use sync dictionary from mirror but didnt work correctly, wouldnt sync the values correctly
		private Dictionary<LawOrder, List<string>> aiLaws = new Dictionary<LawOrder, List<string>>();
		public Dictionary<LawOrder, List<string>> AiLaws => aiLaws;

		[Serializable]
		public struct LawSyncData
		{
			public LawOrder LawOrder;
			public string[] Laws;
		}

		#region LifeCycle

		private void Awake()
		{
			playerScript = GetComponent<PlayerScript>();
			playerScript.OnActionControlPlayer += PlayerEnterBody;

			cooldowns = GetComponent<HasCooldowns>();
			lineRenderer = GetComponentInChildren<LineRenderer>();
		}

		private void Start()
		{
			if(CustomNetworkManager.IsServer == false) return;

			//TODO beam new AI message, play sound too?

			//Set up laws
			SetRandomDefaultLawSet();

			var newVesselObject = Spawn.ServerPrefab(corePrefab, playerScript.RegisterPlayer.WorldPosition, transform.parent).GameObject;

			if (newVesselObject == null)
			{
				Debug.LogError($"Failed to spawn Ai core for {gameObject}");
				return;
			}

			//Set new vessel
			ServerSetNewVessel(newVesselObject);

			newVesselObject.GetComponent<AiVessel>().SetLinkedPlayer(this);

			isCarded = false;
		}

		public override void OnStartClient()
		{
			base.OnStartClient();

			if(PlayerManager.LocalPlayerScript  == null ||
			   PlayerManager.LocalPlayerScript.PlayerType != PlayerTypes.Ai) return;

			SetSpriteVisibility(true);
		}

		private void AddVesselListeners()
		{
			var coreIntegrity = vesselObject.GetComponent<Integrity>();
			coreIntegrity.OnWillDestroyServer.AddListener(OnCoreDestroy);
			coreIntegrity.OnApplyDamage.AddListener(OnCoreDamage);
			hasPower = true;

			//Power set up
			var apc = vesselObject.GetComponent<APCPoweredDevice>();
			if (apc != null)
			{
				if (apc.ConnectToClosestApc() == false)
				{
					Chat.AddExamineMsgFromServer(gameObject, "Core was unable to connect to APC");
				}

				apc.OnStateChangeEvent += OnCorePowerLost;
				hasPower = apc.State != PowerState.Off;

				apc.RelatedAPC.OrNull()?.OnPowerNetworkUpdate.AddListener(OnPowerNetworkUpdate);
			}
		}

		private void OnDisable()
		{
			RemoveVesselListeners();
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PurgeLoop);
		}

		private void RemoveVesselListeners()
		{
			if (vesselObject == null) return;

			var coreIntegrity = vesselObject.GetComponent<Integrity>();
			coreIntegrity.OnWillDestroyServer.RemoveListener(OnCoreDestroy);
			coreIntegrity.OnApplyDamage.RemoveListener(OnCoreDamage);

			var apc = vesselObject.GetComponent<APCPoweredDevice>();
			if (apc != null)
			{
				apc.OnStateChangeEvent -= OnCorePowerLost;
				apc.RelatedAPC.OrNull()?.OnPowerNetworkUpdate.RemoveListener(OnPowerNetworkUpdate);
			}
		}

		public void PlayerEnterBody()
		{
			if (hasAuthority == false) return;
			playerScript.Mind.SetPermanentName(playerScript.characterSettings.AiName);
			Init();

			SyncCore(IDvesselObject, IDvesselObject);
			SyncPowerState(hasPower, hasPower);
			CmdSetVisibilityToOtherAis();
		}

		#endregion

		#region Sync Stuff

		private void Init()
		{
			if (aiUi == null)
			{
				aiUi = UIManager.Instance.displayControl.hudBottomAi.GetComponent<UI_Ai>();
			}

			if (lightingSystem == null)
			{
				lightingSystem = Camera.main.GetComponent<LightingSystem>();
			}
		}


		/// <summary>
		/// Sync is used to set up client and to reset stuff for rejoining client
		/// This is only sync'd to the client which owns this object, due to setting on script
		/// </summary>
		private void SyncCore(NetworkIdentity oldCore, NetworkIdentity newCore)
		{
			IDvesselObject = newCore;
			if(vesselObject == null) return;

			//Something weird with headless and local host triggering the sync even though its set to owner
			if (CustomNetworkManager.IsHeadless || hasAuthority == false) return;

			Init();
			aiUi.OrNull()?.SetUp(this);
			coreCamera = vesselObject.GetComponent<SecurityCamera>();

			//Reset location to core
			CmdTeleportToCore();

			isCarded = vesselObject.GetComponent<AiVessel>().IsInteliCard;
			if (isCarded == false)
			{
				ClientSetCameraLocation(vesselObject.transform);
			}

			//Ask server to force sync laws
			CmdAskForLawUpdate();
		}

		[Client]
		private void SyncPowerState(bool oldState, bool newState)
		{
			hasPower = newState;

			if (CustomNetworkManager.IsHeadless || PlayerManager.LocalPlayerObject != gameObject) return;

			Init();

			//If we lose power we cant see much
			lightingSystem.fovDistance = newState ? 13 : 2;
			interactionDistance = newState ? 29 : 2;
		}

		[Client]
		private void SyncPower(float oldValue, float newValue)
		{
			power = newValue;

			if (CustomNetworkManager.IsHeadless || PlayerManager.LocalPlayerObject != gameObject) return;

			Init();

			aiUi.SetPowerLevel(newValue);
		}

		[Client]
		private void SyncIntegrity(float oldValue, float newValue)
		{
			integrity = newValue;

			if (CustomNetworkManager.IsHeadless || PlayerManager.LocalPlayerObject != gameObject) return;

			Init();

			aiUi.SetIntegrityLevel(newValue);
		}

		[Client]
		private void SyncNumberOfCameras(uint oldValue, uint newValue)
		{
			numberOfCameras = newValue;

			if (CustomNetworkManager.IsHeadless || PlayerManager.LocalPlayerObject != gameObject) return;

			Init();

			aiUi.SetNumberOfCameras(newValue);
		}

		#endregion

		#region Camera Stuff

		[Server]
		public void ServerSetCameraLocation(GameObject newObject, bool ignoreCardCheck = false, bool moveMessage = true)
		{
			//Cant switch cameras when carded
			if (isCarded && ignoreCardCheck == false)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"You are carded, you cannot move anywhere");
				return;
			}

			//Remove old listeners
			if (cameraLocation != null && cameraLocation.gameObject != newObject)
			{
				//Remove old power listener
				if (cameraLocation.TryGetComponent<SecurityCamera>(out var oldCamera))
				{
					oldCamera.OnStateChange.RemoveListener(CameraStateChanged);
				}
			}

			if (newObject != null)
			{
				//Set location for validation checks
				cameraLocation = newObject.transform;

				//This is to move the player object so we can see the Ai Eye sprite underneath us
				//TODO for some reason this isnt always working the sprite sometimes stays on the core, or last position
				playerScript.PlayerSync.AppearAtWorldPositionServer(cameraLocation.gameObject.AssumedWorldPosServer(), false);
			}
			else
			{
				cameraLocation = null;
			}

			//Tell client to move their camera to this new camera
			FollowCameraAiMessage.Send(gameObject, newObject);

			//Add new listeners
			if (newObject != null && cameraLocation.gameObject != newObject)
			{
				//Add power listener
				if (newObject.TryGetComponent<SecurityCamera>(out var securityCamera))
				{
					securityCamera.OnStateChange.AddListener(CameraStateChanged);
				}
			}

			if (newObject != null && isCarded == false && moveMessage)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"You move to the {newObject.ExpensiveName()}");
			}
		}

		[Server]
		private void ServerSetCameraLocationVessel()
		{
			ServerSetCameraLocation(GetRootVesselGameobject(), true);
		}

		[Client]
		public void ClientSetCameraLocation(Transform newLocation)
		{
			cameraLocation = newLocation;
			Camera2DFollow.followControl.target = newLocation;
		}

		//Called when camera is destroyed clientside
		[Client]
		private void SetCameras(bool newState)
		{
			foreach (var pairs in SecurityCamera.Cameras)
			{
				if (OpenNetworks.Contains(pairs.Key) == false && newState) continue;

				foreach (var camera in pairs.Value)
				{
					if (camera.CameraActive || newState == false)
					{
						camera.OrNull()?.ToggleAiSprite(newState);
					}
				}
			}
		}

		[Command]
		public void CmdTeleportToCore()
		{
			if(OnCoolDown(NetworkSide.Server)) return;
			StartCoolDown(NetworkSide.Server);

			//Set camera to vessel location, checks if carded or in core
			ServerSetCameraLocationVessel();
		}

		[Command]
		public void CmdToggleCameraLights(bool newState)
		{
			if(OnCoolDown(NetworkSide.Server)) return;
			StartCoolDown(NetworkSide.Server);

			if (allowRemoteAction == false)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"Remote actions have been disabled");
				return;
			}

			SecurityCamera.GlobalLightStatus = newState;

			foreach (var pairs in SecurityCamera.Cameras)
			{
				if (OpenNetworks.Contains(pairs.Key) == false) continue;

				foreach (var camera in pairs.Value)
				{
					camera.OrNull()?.ToggleLight(newState);
				}
			}

			Chat.AddExamineMsgFromServer(gameObject, $"You turn the camera lights {(newState ? "on" : "off")}");
		}

		//Called when camera is deactivated, e.g loss of power or wires cut
		//Or when camera is destroyed
		[Server]
		private void CameraStateChanged(bool newState)
		{
			//Only need to reset location when it turns off
			if(newState) return;

			if (hasPower && cameraLocation != null)
			{
				var validCameras = GetValidCameras().OrderBy(c =>
					Vector3.Distance(cameraLocation.position, c.gameObject.AssumedWorldPosServer())).ToArray();

				if (validCameras.Any())
				{
					//Move to nearest camera instead
					ServerSetCameraLocation(validCameras.First().gameObject);
					return;
				}
			}

			//Lost power so move back to core
			ServerSetCameraLocation(vesselObject);
		}

		private void ToggleCameras(bool newState)
		{
			if(connectionToClient == null) return;
			TargetRpcToggleCameras(connectionToClient, newState);
			SetVisibilityToOtherAis(false);
		}

		[TargetRpc]
		//Sets the camera and Ai player sprites for this player
		private void TargetRpcToggleCameras(NetworkConnection conn, bool newState)
		{
			//Set cameras
			SetCameras(newState);

			//Set our sprite state
			SetSpriteVisibility(newState);
		}

		[Command]
		//Used by the Ai teleport tab to move camera
		public void CmdTeleportToCamera(GameObject newCamera, bool moveMessage)
		{
			if(OnCoolDown(NetworkSide.Server)) return;
			StartCoolDown(NetworkSide.Server);

			if (isCarded)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"Can only move to different camera when in core");
				return;
			}

			if(newCamera == null || newCamera.TryGetComponent<SecurityCamera>(out var securityCamera) == false) return;

			if(OpenNetworks.Contains(securityCamera.SecurityCameraChannel) == false) return;

			if (hasPower == false)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"We have no power, cannot move to {securityCamera.gameObject.ExpensiveName()}");

				//Sanity check to make sure if we have no power we are at core
				if (cameraLocation != vesselObject.transform)
				{
					ServerSetCameraLocation(vesselObject);
				}
				return;
			}

			if (securityCamera.CameraActive == false)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"{securityCamera.gameObject.ExpensiveName()} is inactive, cannot move to it");
				return;
			}

			ServerSetCameraLocation(newCamera, moveMessage: moveMessage);
		}

		[Command]
		//Sends camera to player or mob, validated client and serverside
		public void CmdTrackObject(GameObject objectToTrack)
		{
			if(OnCoolDown(NetworkSide.Server)) return;
			StartCoolDown(NetworkSide.Server);

			if (isCarded)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"Can only track lifeforms when in core");
				return;
			}

			if(objectToTrack == null) return;

			if (hasPower == false)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"We have no power, cannot track {objectToTrack.ExpensiveName()}");

				//Sanity check to make sure if we have no power we are at core
				if (cameraLocation != vesselObject.transform)
				{
					ServerSetCameraLocation(vesselObject);
				}
				return;
			}

			var cameraThatCanSee = CanSeeObject(objectToTrack);

			if (cameraThatCanSee == null)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"Failed to track {objectToTrack.ExpensiveName()}");
				return;
			}

			ServerSetCameraLocation(cameraThatCanSee.gameObject);
		}

		//Client and serverside can use this to validate
		public SecurityCamera CanSeeObject(GameObject objectToCheck)
		{
			Vector3Int objectPos;

			//Check to see if player
			if (objectToCheck.TryGetComponent<PlayerScript>(out var checkPlayerScript))
			{
				//Dont check ghosts
				if (checkPlayerScript.PlayerType == PlayerTypes.Ghost) return null;

				//Dont check yourself
				if(checkPlayerScript.gameObject == gameObject) return null;

				//If we are player get position
				objectPos = checkPlayerScript.RegisterPlayer.WorldPosition;
			}
			else
			{
				//If not player check to see if mob
				if (objectToCheck.TryGetComponent<MobAI>(out var mobAI))
				{
					//Get mob position
					objectPos =  mobAI.registerObject.WorldPosition;
				}
				else
				{
					//Not player or mob
					return null;
				}
			}

			//FOV distance is 13 and only check wall mount layer for sec cameras
			var overlap = Physics2D.OverlapCircleAll(objectPos.To2Int(),
				13, LayerMask.GetMask("WallMounts"));

			foreach (var wallMount in overlap)
			{
				if(wallMount.gameObject.TryGetComponent<SecurityCamera>(out var securityCamera) == false) continue;

				if(securityCamera.CameraActive == false) continue;

				if(OpenNetworks.Contains(securityCamera.SecurityCameraChannel) == false) continue;

				//Do linecast and raycast to see if we can see player
				var check = MatrixManager.Linecast(securityCamera.RegisterObject.WorldPosition,
					LayerTypeSelection.Walls, LayerMask.GetMask("Door Closed"),
					objectPos);

				//If hit wall or closed door, and above tolerance skip
				if(check.ItHit && Vector3.Distance(objectPos, check.HitWorld) > 0.5f) continue;

				//Else we must have reached player, therefore we can see and track them
				//Only need to find first camera that finds them
				return securityCamera;
			}

			return null;
		}

		[Server]
		public void ServerSetNumberOfCameras(int number)
		{
			//Shouldn't ever happen but just in case
			if (number < 0)
			{
				number = 0;
			}

			numberOfCameras = (uint)number;
		}

		#endregion

		#region Key Move Camera

		//Moving the camera using the arrow keys
		[Client]
		public void MoveCameraByKey(MoveAction moveAction)
		{
			if (isCarded)
			{
				Chat.AddExamineMsgToClient("You are carded, you cannot move throughout the camera network");
				return;
			}

			var lowerDegree = 0;

			switch (moveAction)
			{
				case MoveAction.MoveUp:
					lowerDegree = 0;
					break;
				case MoveAction.MoveLeft:
					lowerDegree = 90;
					break;
				case MoveAction.MoveDown:
					lowerDegree = 180;
					break;
				case MoveAction.MoveRight:
					lowerDegree = 270;
					break;
				default:
					return;
			}

			var chosenCameras = new List<SecurityCamera>();
			var aiPlayerCameraLocation = cameraLocation == null ? vesselObject.AssumedWorldPosServer() : cameraLocation.position;

			foreach (var securityCamera in GetValidCameras())
			{
				var securityCameraLocation = securityCamera.gameObject.AssumedWorldPosServer();

				var direction = securityCameraLocation - aiPlayerCameraLocation;
				var angle = (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 45;

				if (Mathf.Sign(angle) == -1)
				{
					angle += 360;
				}

				//Check to see if between the correct angle for the move type
				if (angle > lowerDegree && angle <= lowerDegree + 90)
				{
					chosenCameras.Add(securityCamera);
				}
			}

			if(chosenCameras.Count == 0) return;

			var sortedCameras = chosenCameras.OrderBy(c =>
				Vector3.Distance(aiPlayerCameraLocation, c.gameObject.AssumedWorldPosServer()));

			//Move to nearest camera
			CmdTeleportToCamera(sortedCameras.First().gameObject, false);
		}

		#endregion

		#region AiCore

		[Server]
		public void ServerSetNewVessel(GameObject newVessel)
		{
			RemoveVesselListeners();
			vesselObject = newVessel;

			playerScript.SetPlayerChatLocation(newVessel);

			isCarded = newVessel.GetComponent<AiVessel>().IsInteliCard;

			if (isCarded)
			{
				hasPower = true;
			}

			ToggleCameras(isCarded == false);

			//Force camera to core/card
			ServerSetCameraLocationVessel();

			AddVesselListeners();
		}

		[Server]
		public void ServerSetPermissions(bool allowInteractions, bool allowRadio)
		{
			allowRemoteAction = allowInteractions;
			this.allowRadio = allowRadio;
		}

		[Server]
		private void OnCoreDestroy(DestructionInfo info)
		{
			info.Destroyed.OnWillDestroyServer.RemoveListener(OnCoreDestroy);

			Death();
		}

		//Called when the core is damaged
		[Server]
		private void OnCoreDamage(DamageInfo info)
		{
			//Do negative of damage to remove it
			ChangeIntegrity(-info.Damage);
		}

		//Positive to heal, negative to damage
		[Server]
		private void ChangeIntegrity(float byValue)
		{
			var newIntegrity = integrity + byValue;

			//Integrity has to be between 0 and 100 due to the slider setting for the intelicard GUI
			newIntegrity = Mathf.Clamp(newIntegrity, 0, StartingIntegrity);

			integrity = newIntegrity;

			//Check for death
			if (integrity.Approx(0))
			{
				Death();
			}

			if (isCarded)
			{
				vesselObject.GetComponent<AiVessel>().UpdateGui();
			}
		}

		public void FullyHeal()
		{
			ChangeIntegrity(StartingIntegrity);
		}

		[Command]
		public void CmdToggleFloorBolts()
		{
			if(OnCoolDown(NetworkSide.Server)) return;
			StartCoolDown(NetworkSide.Server);

			if (isCarded)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"Can only toggle floor bolts when in core");
				return;
			}

			if(vesselObject == null || vesselObject.TryGetComponent<UniversalObjectPhysics>(out var objectBehaviour) == false) return;

			var newState = !objectBehaviour.IsNotPushable;

			Chat.AddActionMsgToChat(gameObject, $"You {(newState ? "disengage" : "engage")} your core floor bolts",
				$"{vesselObject.ExpensiveName()} {(newState ? "disengages" : "engages")} its floor bolts");
			objectBehaviour.SetIsNotPushable(newState);
		}

		[Server]
		private GameObject GetRootVesselGameobject()
		{
			return isCarded ? ServerGetCardLocationObject() : VesselObject;
		}

		[Server]
		private GameObject ServerGetCardLocationObject()
		{
			var cardPickupable = vesselObject.GetComponent<Pickupable>();
			if (cardPickupable.OrNull()?.ItemSlot != null)
			{
				var rootPlayer = cardPickupable.ItemSlot.RootPlayer();
				if (rootPlayer != null)
				{
					return rootPlayer.gameObject;
				}

				return cardPickupable.ItemSlot.GetRootStorageOrPlayer().gameObject;
			}

			//Else we must be on the floor so return ourselves
			return vesselObject;
		}

		#endregion

		#region Power

		//Called when the core has lost power
		[Server]
		private void OnCorePowerLost(PowerState old , PowerState newState)
		{
			if (newState == PowerState.Off)
			{
				hasPower = false;
				allowRadio = false;

				//Set serverside interaction distance validation
				interactionDistance = 2;
				Chat.AddExamineMsgFromServer(gameObject, "Core power has failed");

				//Force move to core
				ServerSetCameraLocation(vesselObject);

				power = 0;

				if (tryingToRestorePower == false)
				{
					tryingToRestorePower = true;
					routine = StartCoroutine(TryRestartPower());
				}

				return;
			}

			hasPower = true;
			allowRadio = true;

			//Reset distance validation value
			interactionDistance = 29;

			if (newState == PowerState.LowVoltage)
			{
				Chat.AddExamineMsgFromServer(gameObject, "Your core power is failing!");
			}

			if (newState == PowerState.OverVoltage)
			{
				Chat.AddExamineMsgFromServer(gameObject, "Your core power voltage is too high!");
			}
		}

		[Server]
		//Called when the connected apc does a power network update
		private void OnPowerNetworkUpdate(APC apc)
		{
			power = Mathf.Clamp(apc.CalculateChargePercentage() * 100, 0, 100);
		}

		private IEnumerator TryRestartPower()
		{
			SendChatMessage("Backup battery online. Scanners, camera, and radio interface offline. Beginning fault-detection.");
			yield return WaitFor.Seconds(5);
			PowerRestoreIntervalCheck();

			SendChatMessage("Fault confirmed: missing external power. Shutting down main control system to save power.");
			yield return WaitFor.Seconds(2);
			PowerRestoreIntervalCheck();

			SendChatMessage("Emergency control system online. Verifying connection to power network...");
			yield return WaitFor.Seconds(5);
			PowerRestoreIntervalCheck();

			//Check to see if connected to APCPoweredDevice
			var apc = vesselObject.OrNull()?.GetComponent<APCPoweredDevice>();
			if (apc == null)
			{
				SendChatMessage("ERROR: Unable to verify! No power connection detected!");
				StopRestore();

				//Shouldn't need to yield break but just in case
				yield break;
			}

			//Check to see if connected to APC
			if (apc.RelatedAPC == null)
			{
				//No APC connection, try to find nearest
				SendChatMessage("Unable to verify! No connection to APC detected!");
				yield return WaitFor.Seconds(2);
				PowerRestoreIntervalCheck();
				yield return WaitFor.EndOfFrame;

				SendChatMessage("APC connection protocols activated. Attempting to interface with nearest APC...");
				yield return WaitFor.Seconds(5);
				PowerRestoreIntervalCheck();
				yield return WaitFor.EndOfFrame;

				apc.ConnectToClosestApc();

				yield return WaitFor.Seconds(1);

				if (apc.RelatedAPC == null)
				{
					SendChatMessage("ERROR: Failed to interface! No APC's detected! Recovery operation ceasing!");
					StopRestore();

					//Shouldn't need to yield break but just in case
					yield break;
				}

				//Found APC check power again
				PowerRestoreIntervalCheck(true);
				yield return WaitFor.EndOfFrame;
			}

			//We have an APC but still no power...
			SendChatMessage("Connection to APC verified. Searching for fault in internal power network...");
			yield return WaitFor.Seconds(5);
			PowerRestoreIntervalCheck();
			yield return WaitFor.EndOfFrame;

			//TODO once APC can be shut off check here for that and to force reactivate if it was turned off
			SendChatMessage("APC internal power network operational. Searching for fault in external power network...");
			yield return WaitFor.Seconds(2);
			PowerRestoreIntervalCheck();
			yield return WaitFor.EndOfFrame;

			//Check for APC again, might have been destroyed...
			var apcSecondCheck = vesselObject.OrNull()?.GetComponent<APCPoweredDevice>();
			if (apcSecondCheck == null || apcSecondCheck.RelatedAPC == null)
			{
				SendChatMessage("ERROR: Connection to APC has failed whilst trying to find fault!");
				StopRestore();

				//Shouldn't need to yield break but just in case
				yield break;
			}

			//Check for department battery
			var batteries = apcSecondCheck.RelatedAPC.DepartmentBatteries.Where(x => x != null).ToArray();
			if (batteries.Length == 0)
			{
				SendChatMessage("ERROR: Unable to locate external power network department battery! Physical fault cannot be fixed!");
				StopRestore();

				//Shouldn't need to yield break but just in case
				yield break;
			}

			SendChatMessage("APC external power network operational. Searching for fault in external power network provider...");
			yield return WaitFor.Seconds(2);
			PowerRestoreIntervalCheck();
			yield return WaitFor.EndOfFrame;

			for (int i = 0; i < batteries.Length; i++)
			{
				var battery = batteries[i];
				if(battery == null) continue;
				SendChatMessage("Operational department battery found.");
				yield return WaitFor.Seconds(1);
				PowerRestoreIntervalCheck();
				yield return WaitFor.EndOfFrame;

				if (battery == null)
				{
					SendChatMessage("ERROR: Lost Connection to department battery!");

					//Only stop checking if this is the last battery
					if(i != batteries.Length - 1) continue;

					SendChatMessage("ERROR: All external power providers have been checked. Recovery operation ceasing!");
					StopRestore();

					//Shouldn't need to yield break but just in case
					yield break;
				}

				if (battery.CurrentState == BatteryStateSprite.Empty)
				{
					SendChatMessage("Fault Found: Department battery power is at 0%. Unable to fix fault.");

					//Only stop checking if this is the last battery
					if(i != batteries.Length - 1) continue;

					SendChatMessage("ERROR: All external power providers have been checked. Recovery operation ceasing!");
					StopRestore();

					//Shouldn't need to yield break but just in case
					yield break;
				}

				//Battery is not empty so see if it is off
				if (battery.isOn == false)
				{
					SendChatMessage("Fault Found: Department battery power supply is turned off! Loading control program into power port software.");
					yield return WaitFor.Seconds(1);
					PowerRestoreIntervalCheck();
					yield return WaitFor.EndOfFrame;

					SendChatMessage("Transfer complete. Forcing battery to execute program.");
					yield return WaitFor.Seconds(5);
					PowerRestoreIntervalCheck();
					yield return WaitFor.EndOfFrame;

					SendChatMessage("Receiving control information from battery.");
					yield return WaitFor.Seconds(1);
					PowerRestoreIntervalCheck();
					yield return WaitFor.EndOfFrame;

					if (battery == null)
					{
						SendChatMessage("ERROR: Lost Connection to department battery!");

						//Only stop checking if this is the last battery
						if(i != batteries.Length - 1) continue;

						SendChatMessage("ERROR: All external power providers have been checked. Recovery operation ceasing!");
						StopRestore();

						//Shouldn't need to yield break but just in case
						yield break;
					}

					SendChatMessage("Assuming direct control. Forcing power supply on!");

					//Force turn on the supply
					battery.isOn = true;
					battery.UpdateServerState();
					break;
				}
			}

			//Power back online!
			tryingToRestorePower = false;
		}

		private void PowerRestoreIntervalCheck(bool weRestoredPower = false)
		{
			//Check to see if we have died or carded during routine...
			if (hasDied || isCarded)
			{
				if (isCarded)
				{
					SendChatMessage("InteliCard Power Online. Alert cancelled. Power has been restored.");
				}

				StopRestore();
				return;
			}

			//If we still dont have power continue
			if(hasPower == false) return;

			SendChatMessage(weRestoredPower ? "Alert cancelled. Power has been restored."
				: "Alert cancelled. Power has been restored without our assistance.");

			StopRestore();
		}

		private void StopRestore()
		{
			StopCoroutine(routine);
			tryingToRestorePower = false;
		}

		private void SendChatMessage(string message)
		{
			Chat.AddExamineMsgFromServer(gameObject, message);
		}

		#endregion

		#region Misc actions

		[Command]
		public void CmdCallShuttle(string reason)
		{
			if(OnCoolDown(NetworkSide.Server)) return;
			StartCoolDown(NetworkSide.Server);

			if (allowRemoteAction == false)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"Remote actions have been disabled");
				return;
			}

			if (string.IsNullOrEmpty(reason))
			{
				Chat.AddExamineMsgFromServer(gameObject, "You must specify a reason to call the shuttle");
				return;
			}

			//Remove tags
			reason = Chat.StripTags(reason);

			if (reason.Trim().Length < 10)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You must provide a longer reason when calling the shuttle");
				return;
			}

			if (GameManager.Instance.PrimaryEscapeShuttle.CallShuttle(out var result))
			{
				CentComm.MakeShuttleCallAnnouncement(GameManager.Instance.PrimaryEscapeShuttle.InitialTimerSeconds, reason);
			}

			Chat.AddExamineMsgFromServer(gameObject, result);
		}

		[Client]
		public void ShowInteractionLine(Vector3[] positions, bool hit)
		{
			lineRenderer.enabled = false;
			StopCoroutine(LineFade(hit));

			lineRenderer.positionCount = positions.Length;
			lineRenderer.SetPositions(positions);
			lineRenderer.enabled = true;

			StartCoroutine(LineFade(hit));
		}

		private IEnumerator LineFade(bool hit)
		{
			var a = 1f;
			var colour = hit ? Color.red : Color.green;

			while (a > 0.1)
			{
				lineRenderer.startColor = colour;
				lineRenderer.endColor = colour;
				yield return WaitFor.Seconds(0.1f);
				a -= 0.1f;
				a = Mathf.Clamp(a, 0f, 1f);
				colour.a = a;
			}

			lineRenderer.enabled = false;
		}

		[Command]
		private void CmdSetVisibilityToOtherAis()
		{
			SetVisibilityToOtherAis(true);
		}

		[Server]
		//Sets Ai sprite for all players
		private void SetVisibilityToOtherAis(bool isVisible)
		{
			foreach (var player in PlayerList.Instance.GetAllPlayers())
			{
				if(player.Script.PlayerType != PlayerTypes.Ai) continue;

				player.Script.GetComponent<AiPlayer>().OrNull()?.TargetRpcSetSpriteVisibility(connectionToClient, isVisible);
			}
		}

		[TargetRpc]
		private void TargetRpcSetSpriteVisibility(NetworkConnection conn, bool isVisible)
		{
			//Reset sprite layer, 31 ghost, 8 for players
			SetSpriteVisibility(isVisible);
		}


		[Client]
		private void SetSpriteVisibility(bool isVisible)
		{
			//Reset sprite layer, 8 for players, 31 for ghosts
			mainSprite.layer = isVisible ? 8 : 31;
		}

		#endregion

		#region Death

		[Server]
		public void Suicide()
		{
			Death();
		}

		[Server]
		private void Death()
		{
			if(hasDied) return;
			hasDied = true;

			ToggleCameras(false);

			Chat.AddExamineMsgFromServer(gameObject, $"You have been destroyed");

			var vessel = vesselObject.GetComponent<AiVessel>();

			if (vessel != null)
			{
				//0 is empty, 1 is full, 2 is dead sprite
				vessel.SetLinkedPlayer(null);
				vessel.VesselSpriteHandler.ChangeSprite(2);
			}

			//Transfer player to ghost
			playerScript.Mind.Ghost();

			//Despawn this player object
			_ = Despawn.ServerSingle(gameObject);
		}

		[Server]
		public void SetPurging(bool newState)
		{
			isPurging = newState;

			Chat.AddExamineMsgFromServer(gameObject, $"You are{(newState ? "" : " no longer")} being purged!");

			if (isCarded)
			{
				vesselObject.GetComponent<AiVessel>().UpdateGui();
			}

			if (isPurging)
			{
				UpdateManager.Add(PurgeLoop, purgeDamageInterval);
				return;
			}

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PurgeLoop);
		}

		private void PurgeLoop()
		{
			ChangeIntegrity(-1);
		}

		public void OnGib()
		{
			Death();
		}

		#endregion

		#region Laws

		[Server]
		public void UploadLawModule(HandApply interaction, bool isUploadConsole = false)
		{
			//Check Ai isn't dead, or carded and disallowed interactions
			if (HasDied || (isCarded && allowRemoteAction == false))
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"Unable to connect to {gameObject.ExpensiveName()}");
				return;
			}

			//Must have used module, but do check in case
			if (interaction.HandObject.TryGetComponent<AiLawModule>(out var module) == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"Can only use a module on this {(isUploadConsole ? "console" : "core")}");
				return;
			}

			var lawFromModule = module.GetLawsFromModule(interaction.PerformerPlayerScript);

			if (module.AiModuleType == AiModuleType.Purge || module.AiModuleType == AiModuleType.Reset)
			{
				var isPurge = module.AiModuleType == AiModuleType.Purge;
				ResetLaws(isPurge);
				Chat.AddActionMsgToChat(interaction.Performer, $"You {(isPurge ? "purge" : "reset")} all of {gameObject.ExpensiveName()}'s laws",
					$"{interaction.Performer.ExpensiveName()} {(isPurge ? "purges" : "resets")} all of {gameObject.ExpensiveName()}'s laws");
				return;
			}

			if (lawFromModule.Count == 0)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "No laws to upload");
				return;
			}

			//If we are only adding core laws then we must mean to remove old core laws
			//This means we are assuming that the law set must only have core laws if it is to replace the old laws fully
			var notOnlyCoreLaws = false;

			foreach (var law in lawFromModule)
			{
				if (law.Key != AiPlayer.LawOrder.Core)
				{
					notOnlyCoreLaws = true;
					break;
				}
			}

			SetLaws(lawFromModule, true, notOnlyCoreLaws);

			Chat.AddActionMsgToChat(interaction.Performer, $"You change {gameObject.ExpensiveName()} laws",
				$"{interaction.Performer.ExpensiveName()} changes {gameObject.ExpensiveName()} laws");
		}

		//Add one law
		//Wont allow more than one traitor law
		[Server]
		public void AddLaw(string newLaw, LawOrder order, bool init = false)
		{
			if (aiLaws.ContainsKey(order) == false)
			{
				aiLaws.Add(order, new List<string>() {newLaw});
			}
			else
			{
				if (order == LawOrder.Traitor && aiLaws[order].Count > 0)
				{
					//Can only have one traitor law
					return;
				}

				if (aiLaws[order].Contains(newLaw))
				{
					//Cant add the same law with the same string more than once
					return;
				}

				aiLaws[order].Add(newLaw);
			}

			//Dont spam client on init
			if(init) return;

			Chat.AddExamineMsgFromServer(gameObject, "Your Laws Have Been Updated!");

			//Tell player to open law screen so they dont miss that their laws have changed
			ServerUpdateClientLaws();
		}

		//Set a new list of laws, used mainly to set new core laws, can remove core laws if parameter set true
		//Wont replace hacked. ion laws and freeform can be replaced if parameters set to false
		[Server]
		public void SetLaws(Dictionary<LawOrder, List<string>> newLaws, bool keepIonLaws = true, bool keepCoreLaws = false, bool keepFreeform = true)
		{
			foreach (var lawGroups in aiLaws)
			{
				if (lawGroups.Key == LawOrder.Traitor)
				{
					TryAddLaw(LawOrder.Traitor);
				}

				if (keepIonLaws && lawGroups.Key == LawOrder.Hacked)
				{
					TryAddLaw(LawOrder.Hacked);
				}

				if (keepIonLaws && lawGroups.Key == LawOrder.IonStorm)
				{
					TryAddLaw(LawOrder.IonStorm);
				}

				if (keepCoreLaws && lawGroups.Key == LawOrder.Core)
				{
					TryAddLaw(LawOrder.Core);
				}

				if (keepFreeform && lawGroups.Key == LawOrder.Freeform)
				{
					TryAddLaw(LawOrder.Freeform);
				}

				void TryAddLaw(LawOrder order)
				{
					if (newLaws.ContainsKey(order) == false)
					{
						newLaws.Add(order, lawGroups.Value);
					}
					else
					{
						if (order == LawOrder.Traitor && newLaws[order].Count > 0)
						{
							//Can only have one traitor law
							return;
						}

						var lawsInNewGroup = lawGroups.Value;

						//Only allow unique laws, dont allow multiple of the same law
						foreach (var lawToAdd in lawGroups.Value)
						{
							if (newLaws[order].Contains(lawToAdd))
							{
								lawsInNewGroup.Remove(lawToAdd);
							}
						}

						newLaws[order].AddRange(lawsInNewGroup);
					}
				}
			}

			aiLaws = newLaws;

			Chat.AddExamineMsgFromServer(gameObject, "Your Laws Have Been Updated!");

			//Tell player to open law screen so they dont miss that their laws have changed
			ServerUpdateClientLaws();
		}

		//Removes all laws except core and traitor, unless is purge then will remove core as well
		[Server]
		public void ResetLaws(bool isPurge = false)
		{
			var lawsToRemove = new Dictionary<LawOrder, List<string>>();

			foreach (var law in aiLaws)
			{
				if ((isPurge == false && law.Key == LawOrder.Core) || law.Key == LawOrder.Traitor) continue;

				lawsToRemove.Add(law.Key, law.Value);
			}

			foreach (var law in lawsToRemove)
			{
				aiLaws.Remove(law.Key);
			}

			Chat.AddExamineMsgFromServer(gameObject, "Your Laws Have Been Updated!");

			ServerUpdateClientLaws();
		}

		public string GetLawsString()
		{
			var lawString = new StringBuilder();

			foreach (var law in GetLaws())
			{
				lawString.AppendLine(law);
			}

			return lawString.ToString();
		}

		//Valid server and client side
		//Gets list of laws with numbering correct
		public List<string> GetLaws()
		{
			var lawsToReturn = new List<string>();

			//Order laws by their enum value
			// 0 laws first, freeform last
			var laws = AiLaws.OrderBy(x => x.Key);

			var count = 1;
			var number = "";

			foreach (var lawGroup in laws)
			{
				if (lawGroup.Key == AiPlayer.LawOrder.Traitor)
				{
					number = "0. ";
				}
				else if(lawGroup.Key == AiPlayer.LawOrder.Hacked)
				{
					number = "@#$# ";
				}
				else if(lawGroup.Key == AiPlayer.LawOrder.IonStorm)
				{
					number = "@#!# ";
				}

				for (int i = 0; i < lawGroup.Value.Count; i++)
				{
					if (lawGroup.Key == AiPlayer.LawOrder.Core || lawGroup.Key == AiPlayer.LawOrder.Freeform)
					{
						number = $"{count}. ";
						count++;
					}

					lawsToReturn.Add(number + lawGroup.Value[i]);
				}
			}

			return lawsToReturn;
		}

		[Server]
		private void ServerUpdateClientLaws()
		{

			foreach (var Cyborg in LinkedCyborgs)
			{
				Cyborg.SetLaws(aiLaws);
			}


			var data = new List<LawSyncData>();

			foreach (var lawGroup in aiLaws)
			{
				data.Add(new LawSyncData()
				{
					LawOrder = lawGroup.Key,
					Laws = lawGroup.Value.ToArray()
				});
			}

			TargetRpcForceLawUpdate(data);
		}

		//Force a law update on player and makes player open law screen
		[TargetRpc]
		private void TargetRpcForceLawUpdate(List<LawSyncData> newData)
		{
			aiLaws.Clear();

			foreach (var lawData in newData)
			{
				aiLaws.Add(lawData.LawOrder, lawData.Laws.ToList());
			}

			Init();

			if (aiUi.aiPlayer == null)
			{
				aiUi.SetUp(this);
			}

			aiUi.OpenLaws();
		}

		[Command]
		private void CmdAskForLawUpdate()
		{
			ServerUpdateClientLaws();

			//Sync number of cameras here for new player.
			SecurityCamera.SyncNumberOfCameras();
		}

		[ContextMenu("randomise laws")]
		public void SetRandomDefaultLawSet()
		{
			var pickedLawSet = defaultLawSets.PickRandom();
			foreach (var law in pickedLawSet.Laws)
			{
				AddLaw(law.Law, law.LawOrder, true);
			}
		}

		[ContextMenu("debug log laws")]
		public void DebugLogLaws()
		{
			foreach (var law in GetLaws())
			{
				Debug.LogError(law);
			}
		}

		public enum LawOrder
		{
			//Cannot be removed

			// Law 0
			Traitor,

			//Can be removed

			//  @#$#
			// Added by Hacked Module
			Hacked,

			//  @#!#
			// Added by event
			IonStorm,

			// Core 1-....
			// Normal Laws, wont be removed on reset only purge
			Core,

			// Additional Laws can be removed on reset
			// Core +1-....
			Freeform
		}

		#endregion

		private List<SecurityCamera> GetValidCameras(bool addCurrentLocation = false)
		{
			var validCameras = new List<SecurityCamera>();

			foreach (var cameraGroup in SecurityCamera.Cameras)
			{
				if(openNetworks.Contains(cameraGroup.Key) == false) continue;

				foreach (var securityCamera in cameraGroup.Value)
				{
					if(securityCamera.CameraActive == false) continue;

					if(addCurrentLocation == false && securityCamera.gameObject.transform == cameraLocation) continue;

					validCameras.Add(securityCamera);
				}
			}

			return validCameras;
		}

		public bool OnCoolDown(NetworkSide side)
		{
			return cooldowns.IsOn(CooldownID.Asset(CommonCooldowns.Instance.Interaction, side));
		}

		public void StartCoolDown(NetworkSide side)
		{
			cooldowns.TryStart(CommonCooldowns.Instance.Interaction, side);
		}

		public string AdminInfoString()
		{
			var adminInfo = new StringBuilder();

			adminInfo.AppendLine($"Energy: {power}%");
			adminInfo.AppendLine($"Integrity: {integrity}%");

			foreach (var law in GetLaws())
			{
				adminInfo.AppendLine(law);
			}

			return adminInfo.ToString();
		}
	}
}
