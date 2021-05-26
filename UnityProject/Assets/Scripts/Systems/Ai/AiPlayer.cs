using System;
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
		private GameObject coreObject;
		public GameObject CoreObject => coreObject;

		[SerializeField]
		private int interactionDistance = 25;
		public int InteractionDistance => interactionDistance;

		//Valid client side and serverside for validations
		//Client sends message to where it wants to go, server keeps track to do validations
		private Transform cameraLocation;
		public Transform CameraLocation => cameraLocation;

		private PlayerScript playerScript;

		#region LifeCycle

		private void Awake()
		{
			playerScript = GetComponent<PlayerScript>();
			playerScript.SetState(PlayerScript.PlayerStates.Ai);
		}

		private void Start()
		{
			if(CustomNetworkManager.IsServer == false) return;

			//TODO beam new AI message

			coreObject = Spawn.ServerPrefab(corePrefab, parent: transform.parent).GameObject;

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

			ClientSetCameraLocation(newCore.transform);
			SetCameras(true);
		}

		#endregion

		#region Camera Stuff

		[Server]
		public void ServerSetCameraLocation(GameObject newObject)
		{
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

		#endregion

		#region AiCore

		private void OnCoreDestroy(DestructionInfo info)
		{
			info.Destroyed.OnWillDestroyServer.RemoveListener(OnCoreDestroy);

			if (connectionToClient != null)
			{
				TargetRpcTurnOffCameras(connectionToClient);
			}

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
