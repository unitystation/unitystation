using System;
using System.Collections.Generic;
using Messages.Server;
using Mirror;
using Objects;
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

		[SerializeField]
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

		//Clientside only
		private UI_Ai aiUi;

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

			//TODO beam new AI message

			coreObject = Spawn.ServerPrefab(corePrefab, playerScript.registerTile.WorldPosition, transform.parent).GameObject;

			if (coreObject == null)
			{
				Debug.LogError($"Failed to spawn Ai core for {gameObject}");
				return;
			}

			ServerSetCameraLocation(coreObject);
			coreObject.GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnCoreDestroy);
		}

		private void OnDisable()
		{
			if (coreObject != null)
			{
				coreObject.GetComponent<Integrity>().OnWillDestroyServer.RemoveListener(OnCoreDestroy);
			}
		}

		#endregion

		#region Sync Stuff

		[Client]
		private void SyncCore(GameObject oldCore, GameObject newCore)
		{
			coreObject = newCore;
			if(newCore == null) return;

			aiUi = UIManager.Instance.displayControl.hudBottomAi.GetComponent<UI_Ai>();
			aiUi.OrNull()?.SetUp(this);
			ClientSetCameraLocation(newCore.transform);
			SetCameras(true);
		}

		#endregion

		#region Camera Stuff

		[Server]
		public void ServerSetCameraLocation(GameObject newObject)
		{
			//Cant switch cameras when carded
			if (isCarded)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"You are carded, you have no core to go to");
				return;
			}

			//Remove old integrity listener if possible, ignore if current location is core
			if (cameraLocation != null && cameraLocation.gameObject != gameObject && newObject.TryGetComponent<Integrity>(out var oldCameraIntegrity))
			{
				oldCameraIntegrity.OnWillDestroyServer.RemoveListener(OnCameraDestroy);
			}

			cameraLocation = newObject ? newObject.transform : null;
			FollowCameraAiMessage.Send(gameObject, newObject);

			//Add integrity listener to camera if possible, so we can move camera back to core if camera destroyed
			if (newObject != null && newObject != gameObject && newObject.TryGetComponent<Integrity>(out var cameraIntegrity))
			{
				cameraIntegrity.OnWillDestroyServer.AddListener(OnCameraDestroy);
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
					camera.ToggleAiSprite(newState);
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
					camera.ToggleLight(newState);
				}
			}

			Chat.AddExamineMsgFromServer(gameObject, $"You turn the camera lights {(newState ? "on" : "off")}");
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

		[TargetRpc]
		private void TargetRpcTurnOffCameras(NetworkConnection conn)
		{
			SetCameras(false);
		}

		#endregion
	}
}
