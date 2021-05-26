using System;
using System.Collections.Generic;
using Systems.Electricity;
using Messages.Server;
using Mirror;
using Objects;
using Objects.Research;
using UnityEngine;

namespace Systems.Ai
{
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

		private LightingSystem lightingSystem;

		//Clientside only
		private UI_Ai aiUi;

		[SyncVar(hook = nameof(SyncPower))]
		private bool hasPower;

		// 	Law priority order is this:
		//	0: Traitor/Malf/Onehuman-board Law
		//  ##?$-##: HACKED LAW ##!£//#
		//  ##!£//#: Ion Storm Law ##?$-##
		//	Law 1: First Law
		//	Law 2: Second Law
		//	Law 3: Third Law
		//	Law 4: Freeform
		//	Higher laws (the law above each one) override all lower ones. Whether numbered or not, how they appear (in order) is the order of priority.

		//TODO currently hard coded, when upload console implemented need to sync when changed

		//TODO on law change force the players law tab to open so they dont miss new laws
		private List<string> aiLaws = new List<string>()
		{
			"1. A robot may not injure a human being or, through inaction, allow a human being to come to harm.",
			"2. A robot must obey orders given to it by human beings, except where such orders would conflict with the First Law.",
			"3. A robot must protect its own existence as long as such protection does not conflict with the First or Second Law."
		};
		public List<string> AiLaws => aiLaws;

		#region LifeCycle

		private void Awake()
		{
			playerScript = GetComponent<PlayerScript>();
		}

		private void Start()
		{
			if(CustomNetworkManager.IsServer == false) return;

			//TODO beam new AI message, play sound too?

			coreObject = Spawn.ServerPrefab(corePrefab, playerScript.registerTile.WorldPosition, transform.parent).GameObject;

			if (coreObject == null)
			{
				Debug.LogError($"Failed to spawn Ai core for {gameObject}");
				return;
			}

			//Force camera to core
			ServerSetCameraLocation(coreObject);

			coreObject.GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnCoreDestroy);
			hasPower = true;

			//Power set up
			var apc = coreObject.GetComponent<APCPoweredDevice>();
			if (apc != null)
			{
				apc.ConnectToClosestAPC();
				apc.OnStateChangeEvent.AddListener(OnCorePowerLost);
				hasPower = apc.State != PowerState.Off;
			}

			coreObject.GetComponent<ObjectAttributes>().ServerSetArticleName(playerScript.characterSettings.AiName);
			coreObject.GetComponent<AiCore>().SetLinkedPlayer(this);
		}

		private void OnDisable()
		{
			if (coreObject != null)
			{
				coreObject.GetComponent<Integrity>().OnWillDestroyServer.RemoveListener(OnCoreDestroy);
				coreObject.GetComponent<APCPoweredDevice>().OrNull()?.OnStateChangeEvent.RemoveListener(OnCorePowerLost);
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
		}

		[Client]
		private void SyncPower(bool oldState, bool newState)
		{
			hasPower = newState;

			//If we lose power we cant see much
			lightingSystem.fovDistance = newState ? 13 : 2;
			interactionDistance = newState ? 29 : 2;
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
					oldCamera.ApcPoweredDevice.OrNull()?.OnStateChangeEvent.RemoveListener(CameraPowerStateChanged);
				}
			}

			cameraLocation = newObject ? newObject.transform : null;
			FollowCameraAiMessage.Send(gameObject, newObject);

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
					securityCamera.ApcPoweredDevice.OrNull()?.OnStateChangeEvent.AddListener(CameraPowerStateChanged);
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

		private void OnCameraDestroy(DestructionInfo info)
		{
			if (coreObject == null) return;

			ServerSetCameraLocation(coreObject);
		}

		private void SetCameras(bool newState)
		{
			foreach (var pairs in SecurityCamera.Cameras)
			{
				if (SecurityCamera.OpenNetworks.Contains(pairs.Key) == false && newState) continue;

				foreach (var camera in pairs.Value)
				{
					camera.OrNull()?.ToggleAiSprite(newState);
				}
			}
		}

		[Command]
		public void CmdTeleportToCore()
		{
			ServerSetCameraLocation(coreObject);
		}

		[Command]
		public void CmdToggleCameraLights(bool newState)
		{
			SecurityCamera.GlobalLightStatus = newState;

			foreach (var pairs in SecurityCamera.Cameras)
			{
				if (SecurityCamera.OpenNetworks.Contains(pairs.Key) == false) continue;

				foreach (var camera in pairs.Value)
				{
					camera.OrNull()?.ToggleLight(newState);
				}
			}

			Chat.AddExamineMsgFromServer(gameObject, $"You turn the camera lights {(newState ? "on" : "off")}");
		}

		private void CameraPowerStateChanged(Tuple<PowerState, PowerState> oldAndNewStates)
		{
			if(oldAndNewStates.Item2 != PowerState.Off) return;

			//Lost power so move back to core
			ServerSetCameraLocation(coreObject);
		}

		[TargetRpc]
		private void TargetRpcTurnOffCameras(NetworkConnection conn)
		{
			SetCameras(false);
		}

		#endregion

		#region AiCore

		private void OnCoreDestroy(DestructionInfo info)
		{
			info.Destroyed.OnWillDestroyServer.RemoveListener(OnCoreDestroy);

			if (connectionToClient != null)
			{
				TargetRpcTurnOffCameras(connectionToClient);
			}

			Chat.AddExamineMsgFromServer(gameObject, $"You have been destroyed");

			PlayerSpawn.ServerSpawnGhost(playerScript.mind);
		}

		private void OnCorePowerLost(Tuple<PowerState, PowerState> oldAndNewStates)
		{
			if (oldAndNewStates.Item2 == PowerState.Off)
			{
				hasPower = false;
				interactionDistance = 2;
				return;
			}

			hasPower = true;
			interactionDistance = 29;

			if (oldAndNewStates.Item2 == PowerState.LowVoltage && oldAndNewStates.Item1 == PowerState.Off)
			{
				Chat.AddExamineMsgFromServer(gameObject, "Your core power is failing");
			}
		}

		#endregion

		//TODO when moving to card, remove power and integrity listeners from old core.
	}
}
