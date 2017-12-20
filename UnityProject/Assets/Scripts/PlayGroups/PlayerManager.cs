using UnityEngine;
using UnityEngine.SceneManagement;

//Handles control and spawn of player prefab
namespace PlayGroup
{
	public class PlayerManager : MonoBehaviour
	{
		private static PlayerManager playerManager;
		public static GameObject LocalPlayer { get; private set; }

		public static Equipment.Equipment Equipment { get; private set; }

		public static PlayerScript LocalPlayerScript { get; private set; }

		//For access via other parts of the game
		public static PlayerScript PlayerScript { get; private set; }

		public static bool HasSpawned { get; private set; }

		//To fix playername bug when running two instances on 1 machine
		public static string PlayerNameCache { get; set; }

		public static PlayerManager Instance
		{
			get
			{
				if (!playerManager)
				{
					playerManager = FindObjectOfType<PlayerManager>();
				}

				return playerManager;
			}
		}

		private void OnEnable()
		{
			SceneManager.sceneLoaded += OnLevelFinishedLoading;
		}

		private void OnDisable()
		{
			SceneManager.sceneLoaded -= OnLevelFinishedLoading;
		}

		private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
		{
			Reset();
		}

		public static void Reset()
		{
			HasSpawned = false;
		}

		public static void SetPlayerForControl(GameObject playerObjToControl)
		{
			LocalPlayer = playerObjToControl;
			LocalPlayerScript = playerObjToControl.GetComponent<PlayerScript>();

			PlayerScript =
				LocalPlayerScript; // Set this on the manager so it can be accessed by other components/managers
			Camera2DFollow.followControl.target = LocalPlayer.transform;

			HasSpawned = true;
		}

		public static bool PlayerInReach(Transform transform)
		{
			if (PlayerScript != null)
			{
				return PlayerScript.IsInReach(transform.position);
			}
			return false;
		}
	}
}