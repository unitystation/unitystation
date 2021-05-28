using System;
using System.Collections.Generic;
using System.Linq;
using Systems.Electricity;
using Systems.MobAIs;
using Managers;
using Messages.Server;
using Mirror;
using Objects;
using Objects.Engineering;
using Objects.Research;
using TMPro;
using UnityEngine;

namespace Systems.Ai
{
	/// <summary>
	/// Main class controlling player job AI logic
	/// Sync vars in this class only get sync'd to the object owner
	/// </summary>
	public class AiPlayer : NetworkBehaviour
	{
		[SerializeField]
		private GameObject corePrefab = null;

		[SyncVar(hook = nameof(SyncCore))]
		//Ai core or card
		private GameObject coreObject;
		public GameObject CoreObject => coreObject;

		[SerializeField]
		private int interactionDistance = 29;
		public int InteractionDistance => interactionDistance;

		[SerializeField]
		private GameObject mainSprite = null;

		[SerializeField]
		private List<AiLawSet> defaultLawSets = new List<AiLawSet>();

		private SecurityCamera coreCamera = null;
		public SecurityCamera CoreCamera => coreCamera;

		//Follow card only
		private bool isCarded;

		//Valid client side and serverside for validations
		//Client sends message to where it wants to go, server keeps track to do validations
		private Transform cameraLocation;
		public Transform CameraLocation => cameraLocation;

		private PlayerScript playerScript;
		public PlayerScript PlayerScript => playerScript;

		private HasCooldowns cooldowns;

		private LightingSystem lightingSystem;

		//Clientside only
		private UI_Ai aiUi;

		private bool hasDied;

		[SyncVar(hook = nameof(SyncPowerState))]
		private bool hasPower;

		[SyncVar(hook = nameof(SyncPower))]
		private float power;

		[SyncVar(hook = nameof(SyncIntegrity))]
		private float integrity = 100;

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

		//TODO currently hard coded, when upload console implemented
		private readonly SyncDictionary<LawOrder, List<string>> aiLaws = new SyncDictionary<LawOrder, List<string>>();

		public SyncDictionary<LawOrder, List<string>> AiLaws => aiLaws;

		#region LifeCycle

		private void Awake()
		{
			playerScript = GetComponent<PlayerScript>();
			cooldowns = GetComponent<HasCooldowns>();
		}

		private void Start()
		{
			if(CustomNetworkManager.IsServer == false) return;

			//TODO beam new AI message, play sound too?

			//Set up laws
			var pickedLawSet = defaultLawSets.PickRandom();
			foreach (var law in pickedLawSet.Laws)
			{
				AddLaw(law.Law, law.LawOrder, true);
			}

			coreObject = Spawn.ServerPrefab(corePrefab, playerScript.registerTile.WorldPosition, transform.parent).GameObject;

			if (coreObject == null)
			{
				Debug.LogError($"Failed to spawn Ai core for {gameObject}");
				return;
			}

			//Force camera to core
			ServerSetCameraLocation(coreObject);

			var coreIntegrity = coreObject.GetComponent<Integrity>();
			coreIntegrity.OnWillDestroyServer.AddListener(OnCoreDestroy);
			coreIntegrity.OnApplyDamage.AddListener(OnCoreDamage);
			hasPower = true;

			//Power set up
			var apc = coreObject.GetComponent<APCPoweredDevice>();
			if (apc != null)
			{
				if (apc.ConnectToClosestAPC() == false)
				{
					Chat.AddExamineMsgFromServer(gameObject, "Core was unable to connect to APC");
				}

				apc.OnStateChangeEvent.AddListener(OnCorePowerLost);
				hasPower = apc.State != PowerState.Off;

				apc.RelatedAPC.OrNull()?.OnPowerNetworkUpdate.AddListener(OnPowerNetworkUpdate);
			}

			playerScript.SetPermanentName(playerScript.characterSettings.AiName);
			coreObject.GetComponent<ObjectAttributes>().ServerSetArticleName(playerScript.characterSettings.AiName);
			coreObject.GetComponent<AiCore>().SetLinkedPlayer(this);
		}

		private void OnDisable()
		{
			if (coreObject != null)
			{
				var coreIntegrity = coreObject.GetComponent<Integrity>();
				coreIntegrity.OnWillDestroyServer.RemoveListener(OnCoreDestroy);
				coreIntegrity.OnApplyDamage.RemoveListener(OnCoreDamage);

				var apc = coreObject.GetComponent<APCPoweredDevice>();
				if (apc != null)
				{
					apc.OnStateChangeEvent.RemoveListener(OnCorePowerLost);
					apc.RelatedAPC.OrNull()?.OnPowerNetworkUpdate.RemoveListener(OnPowerNetworkUpdate);
				}
			}
		}

		#endregion

		#region Sync Stuff

		/// <summary>
		/// Sync is used to set up client and to reset stuff for rejoining client
		/// This is only sync'd to the client which owns this object, due to setting on script
		/// </summary>
		[Client]
		private void SyncCore(GameObject oldCore, GameObject newCore)
		{
			coreObject = newCore;
			if(newCore == null) return;

			aiUi = UIManager.Instance.displayControl.hudBottomAi.GetComponent<UI_Ai>();
			aiUi.OrNull()?.SetUp(this);
			coreCamera = newCore.GetComponent<SecurityCamera>();

			lightingSystem = Camera.main.GetComponent<LightingSystem>();

			//Reset location to core
			CmdTeleportToCore();
			ClientSetCameraLocation(newCore.transform);

			//Enable security camera overlay
			SetCameras(true);

			//Tell player to open law screen so they can see shat laws they have
			TargetRpcForceLawScreen();

			//Set sprite to player layer
			//TODO currently other AIs cant see where each other is looking maybe sync this to only AI's?
			mainSprite.layer = 8;
		}

		[Client]
		private void SyncPowerState(bool oldState, bool newState)
		{
			hasPower = newState;

			//If we lose power we cant see much
			lightingSystem.fovDistance = newState ? 13 : 2;
			interactionDistance = newState ? 29 : 2;
		}

		[Client]
		private void SyncPower(float oldValue, float newValue)
		{
			power = newValue;

			aiUi.SetPowerLevel(newValue);
		}

		[Client]
		private void SyncIntegrity(float oldValue, float newValue)
		{
			integrity = newValue;

			aiUi.SetIntegrityLevel(newValue);
		}

		#endregion

		#region Camera Stuff

		[Server]
		public void ServerSetCameraLocation(GameObject newObject)
		{
			//Cant switch cameras when carded
			if (isCarded)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"You are carded, you cannot move anywhere");
				return;
			}

			//Remove old listeners
			if (cameraLocation != null && cameraLocation.gameObject != gameObject)
			{
				//Remove old integrity listener if possible, ignore if current location is core
				if (cameraLocation.TryGetComponent<Integrity>(out var oldCameraIntegrity))
				{
					oldCameraIntegrity.OnWillDestroyServer.RemoveListener(OnCameraDestroy);
				}

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
				//TODO for some reason this isnt working the sprite says on the core always
				playerScript.PlayerSync.SetPosition(cameraLocation.position);
			}
			else
			{
				cameraLocation = null;
			}

			//Tell client to move their camera to this new camera
			FollowCameraAiMessage.Send(gameObject, newObject);

			//Add new listeners
			if (newObject != null && newObject != gameObject)
			{
				//Add integrity listener to camera if possible, so we can move camera back to core if camera destroyed
				if (newObject.TryGetComponent<Integrity>(out var cameraIntegrity))
				{
					cameraIntegrity.OnWillDestroyServer.AddListener(OnCameraDestroy);
				}

				//Add power listener
				if (newObject.TryGetComponent<SecurityCamera>(out var securityCamera))
				{
					securityCamera.OnStateChange.AddListener(CameraStateChanged);
				}
			}

			if (newObject != null)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"You move to the {newObject.ExpensiveName()}");
			}
		}

		[Client]
		public void ClientSetCameraLocation(Transform newLocation)
		{
			cameraLocation = newLocation;
			Camera2DFollow.followControl.target = newLocation;
		}

		[Server]
		private void OnCameraDestroy(DestructionInfo info)
		{
			//TODO maybe go to nearest camera instead?
			if (coreObject == null) return;

			ServerSetCameraLocation(coreObject);
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

			ServerSetCameraLocation(coreObject);
		}

		[Command]
		public void CmdToggleCameraLights(bool newState)
		{
			if(OnCoolDown(NetworkSide.Server)) return;
			StartCoolDown(NetworkSide.Server);

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
		[Server]
		private void CameraStateChanged(bool newState)
		{
			//Only need to reset location when it turns off
			//TODO maybe go to nearest camera instead?
			if(newState) return;

			//Lost power so move back to core
			ServerSetCameraLocation(coreObject);
		}

		[TargetRpc]
		private void TargetRpcTurnOffCameras(NetworkConnection conn)
		{
			SetCameras(false);

			//Reset sprite to ghost layer
			mainSprite.layer = 31;
		}

		[Command]
		//Used by the Ai teleport tab to move camera
		public void CmdTeleportToCamera(GameObject newCamera)
		{
			if(OnCoolDown(NetworkSide.Server)) return;
			StartCoolDown(NetworkSide.Server);

			if(newCamera == null || newCamera.TryGetComponent<SecurityCamera>(out var securityCamera) == false) return;

			if(OpenNetworks.Contains(securityCamera.SecurityCameraChannel) == false) return;

			if (hasPower == false)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"We have no power, cannot move to {securityCamera.gameObject.ExpensiveName()}");

				//Sanity check to make sure if we have no power we are at core
				if (cameraLocation != coreObject.transform)
				{
					ServerSetCameraLocation(coreObject);
				}
				return;
			}

			if (securityCamera.CameraActive == false)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"{securityCamera.gameObject.ExpensiveName()} is inactive, cannot move to it");
				return;
			}

			ServerSetCameraLocation(newCamera);
		}

		[Command]
		//Sends camera to player or mob, validated client and serverside
		//TODO do mobs too?
		public void CmdTrackObject(GameObject objectToTrack)
		{
			if(OnCoolDown(NetworkSide.Server)) return;
			StartCoolDown(NetworkSide.Server);

			if(objectToTrack == null) return;

			if (hasPower == false)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"We have no power, cannot track {objectToTrack.ExpensiveName()}");

				//Sanity check to make sure if we have no power we are at core
				if (cameraLocation != coreObject.transform)
				{
					ServerSetCameraLocation(coreObject);
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
				if (checkPlayerScript.PlayerState == PlayerScript.PlayerStates.Ghost) return null;;

				//Dont check yourself
				if(checkPlayerScript.gameObject == gameObject) return null;;

				//If we are player get position
				objectPos = CustomNetworkManager.IsServer
					? checkPlayerScript.PlayerSync.ServerPosition
					: checkPlayerScript.PlayerSync.ClientPosition;
			}
			else
			{
				//If not player check to see if mob
				if (objectToCheck.TryGetComponent<MobAI>(out var mobAI))
				{
					//Get mob position
					objectPos = CustomNetworkManager.IsServer
						? mobAI.Cnt.ServerPosition
						: mobAI.Cnt.ClientPosition;
				}
				else
				{
					//Not player or mob
					return null;
				}
			}

			//FOV distance is 13, 19 is wall mount layer for sec cameras
			var overlap = Physics2D.OverlapCircleAll(objectPos.To2Int(),
				13); //, LayerMask.NameToLayer("WallMounts")

			foreach (var wallMount in overlap)
			{
				if(wallMount.gameObject.TryGetComponent<SecurityCamera>(out var securityCamera) == false) continue;

				if(securityCamera.CameraActive == false) continue;

				if(OpenNetworks.Contains(securityCamera.SecurityCameraChannel) == false) continue;

				//Do linecast and raycast to see if we can see player
				var check = MatrixManager.Linecast(securityCamera.RegisterObject.WorldPosition,
					LayerTypeSelection.Walls, LayerMask.NameToLayer("Door Closed"),
					objectPos);

				//If hit wall or closed door, and above tolerance skip
				if(check.ItHit && Vector3.Distance(objectPos, check.HitWorld) > 0.5f) continue;

				//Else we must have reached player, therefore we can see and track them
				//Only need to find first camera that finds them
				return securityCamera;
			}

			return null;
		}

		#endregion

		#region AiCore

		[Server]
		private void OnCoreDestroy(DestructionInfo info)
		{
			info.Destroyed.OnWillDestroyServer.RemoveListener(OnCoreDestroy);

			Death();
		}

		//Called when the core has lost power
		[Server]
		private void OnCorePowerLost(Tuple<PowerState, PowerState> oldAndNewStates)
		{
			if (oldAndNewStates.Item2 == PowerState.Off)
			{
				hasPower = false;

				//Set serverside interaction distance validation
				interactionDistance = 2;
				Chat.AddExamineMsgFromServer(gameObject, "Core power has failed");

				//Force move to core
				ServerSetCameraLocation(coreObject);

				power = 0;
				return;
			}

			hasPower = true;

			//Reset distance validation value
			interactionDistance = 29;

			if (oldAndNewStates.Item2 == PowerState.LowVoltage && oldAndNewStates.Item1 == PowerState.Off)
			{
				Chat.AddExamineMsgFromServer(gameObject, "Your core power is failing");
			}
		}

		[Server]
		//Called when the connected apc does a power network update
		private void OnPowerNetworkUpdate(APC apc)
		{
			power = Mathf.Clamp(apc.CalculateChargePercentage() * 100, 0, 100);
		}

		//Called when the core is damaged
		[Server]
		private void OnCoreDamage(DamageInfo info)
		{
			SetIntegrity(info.AttackedIntegrity.PercentageDamaged);
		}

		[Server]
		private void SetIntegrity(float newValue)
		{
			//Dont allow negative
			newValue = Mathf.Max(0, newValue);

			integrity = newValue;

			//Dont allow negative
			integrity = Mathf.Max(0, integrity);

			//Check for death
			if (integrity.Approx(0))
			{
				Death();
			}
		}

		[Command]
		public void CmdToggleFloorBolts()
		{
			if(OnCoolDown(NetworkSide.Server)) return;
			StartCoolDown(NetworkSide.Server);

			if(coreObject == null || coreObject.TryGetComponent<ObjectBehaviour>(out var objectBehaviour) == false) return;

			var newState = objectBehaviour.IsNotPushable;

			Chat.AddActionMsgToChat(gameObject, $"You {(newState ? "disengage" : "engage")} your core floor bolts",
				$"{coreObject.ExpensiveName()} {(newState ? "disengages" : "engages")} its floor bolts");
			objectBehaviour.ServerSetPushable(newState);
		}

		#endregion

		#region misc actions

		[Command]
		public void CmdCallShuttle(string reason)
		{
			if(OnCoolDown(NetworkSide.Server)) return;
			StartCoolDown(NetworkSide.Server);

			if (string.IsNullOrEmpty(reason))
			{
				Chat.AddExamineMsgFromServer(gameObject, "You must specify a reason to call the shuttle");
				return;
			}

			if (reason.Trim().Length < 10)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You must provide a longer reason when calling the shuttle");
				return;
			}

			if (GameManager.Instance.PrimaryEscapeShuttle.CallShuttle(out var result))
			{
				CentComm.MakeShuttleCallAnnouncement(TimeSpan.FromSeconds(GameManager.Instance.PrimaryEscapeShuttle.InitialTimerSeconds).ToString(), reason);
			}

			Chat.AddExamineMsgFromServer(gameObject, result);
		}

		#endregion

		#region Death

		[Server]
		private void Death()
		{
			if(hasDied) return;
			hasDied = true;

			if (connectionToClient != null)
			{
				TargetRpcTurnOffCameras(connectionToClient);
			}

			Chat.AddExamineMsgFromServer(gameObject, $"You have been destroyed");

			//Transfer player to ghost
			PlayerSpawn.ServerSpawnGhost(playerScript.mind);

			//Despawn this player object
			_ = Despawn.ServerSingle(gameObject);
		}

		#endregion

		#region Laws

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

				aiLaws[order].Add(newLaw);
			}

			//Dont spam client on init
			if(init) return;

			Chat.AddExamineMsgFromServer(gameObject, "Your Laws Have Been Updated!");

			//Tell player to open law screen so they dont miss that their laws have changed
			TargetRpcForceLawScreen();
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

						newLaws[order] = lawGroups.Value;
					}
				}
			}

			aiLaws.Clear();

			foreach (var law in newLaws)
			{
				aiLaws.Add(law);
			}

			Chat.AddExamineMsgFromServer(gameObject, "Your Laws Have Been Updated!");

			//Tell player to open law screen so they dont miss that their laws have changed
			TargetRpcForceLawScreen();
		}

		//Removes all laws except core and traitor, unless is purge then will remove core as well
		[Server]
		public void ResetLaws(bool isPurge = false)
		{
			var oldLaws = aiLaws;

			foreach (var law in oldLaws)
			{
				if ((isPurge == false && law.Key == LawOrder.Core) || law.Key == LawOrder.Traitor) continue;

				aiLaws.Remove(law.Key);
			}

			Chat.AddExamineMsgFromServer(gameObject, "Your Laws Have Been Updated!");

			//Tell player to open law screen so they dont miss that their laws have changed
			TargetRpcForceLawScreen();
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

		//Makes player open law screen
		[TargetRpc]
		private void TargetRpcForceLawScreen()
		{
			aiUi.OpenLaws();
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

		public bool OnCoolDown(NetworkSide side)
		{
			return cooldowns.IsOn(CooldownID.Asset(CommonCooldowns.Instance.Interaction, side));
		}

		public void StartCoolDown(NetworkSide side)
		{
			cooldowns.TryStart(CommonCooldowns.Instance.Interaction, side);
		}

		//TODO when moving to card, remove power and integrity listeners from old core.
		//TODO keep track of our integrity
	}
}
