using UnityEngine;
using UnityEngine.EventSystems;
using UI;
using Network;

//Handles control and spawn of player prefab
namespace PlayGroup
{
	public class PlayerManager: Photon.PunBehaviour
	{
		public static GameObject LocalPlayer { get; private set; }

		public static Equipment Equipment { get; private set; }

		public static PlayerScript LocalPlayerScript { get; private set; }
        
		//For access via other parts of the game
		public static PlayerScript PlayerScript { get; private set; }

		public static bool HasSpawned { get; private set; }        

		private static PlayerManager playerManager;

		public static PlayerManager Instance {
			get {
				if (!playerManager) {
					playerManager = FindObjectOfType<PlayerManager>();
				}

				return playerManager;
			}
		}

		public static void Reset()
		{
			HasSpawned = false;
		}

		public static void SetPlayerForControl(GameObject playerObjToControl)
		{
			LocalPlayer = playerObjToControl;
			LocalPlayerScript = playerObjToControl.GetComponent<PlayerScript>();
			LocalPlayerScript.IsMine = true; // Set this object to yours, the rest are for network players

			PlayerScript = LocalPlayerScript; // Set this on the manager so it can be accessed by other components/managers
			Camera2DFollow.followControl.target = LocalPlayer.transform;

			Equipment = Instance.GetComponent<Equipment>();
			Equipment.enabled = true;
		}
			
		//CHECK HERE FOR AN EXAMPLE OF INSTANTIATING ITEMS ON PHOTON
		public static void CheckIfSpawned()
		{
			Debug.Log("CHECK IF SPAWNED");

			if (!HasSpawned) {
				if (GameData.IsInGame && NetworkManager.IsConnected) {
					SpawnManager spawnManager = Instance.GetComponent<SpawnManager>();
					spawnManager.SpawnPlayer();
					HasSpawned = true;
				}
			}
		}
			
		public static bool PlayerInReach(Transform transform)
		{
			if (PlayerScript != null) {
				return PlayerScript.IsInReach(transform);
			} else {
				return false;
			}
		}
	}
}
