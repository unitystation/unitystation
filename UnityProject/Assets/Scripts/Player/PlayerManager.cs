using System.Text.RegularExpressions;
using Lobby;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
	private static PlayerManager playerManager;
	public static GameObject LocalPlayer { get; private set; }

	public static Equipment Equipment { get; private set; }

	public static PlayerScript LocalPlayerScript { get; private set; }
	public static JoinedViewer LocalViewerScript { get; private set; }

	//For access via other parts of the game
	public static PlayerScript PlayerScript { get; private set; }

	public static bool HasSpawned { get; private set; }

	public static CharacterSettings CurrentCharacterSettings { get; set; }

	private int mobIDcount;

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

#if UNITY_EDITOR	//Opening the station scene instead of going through the lobby
	void Awake()
	{
		if (CurrentCharacterSettings == null){
			var deserialized = JsonUtility.FromJson<CharacterSettings>(Regex.Unescape(PlayerPrefs.GetString("currentcharacter")));
			PlayerCustomisationDataSOs.Instance.ValidateCharacterSettings(ref deserialized);
			CurrentCharacterSettings = deserialized;
			if (CurrentCharacterSettings == null)
			{
				CurrentCharacterSettings = new CharacterSettings();
			}
		}
	}
#endif

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnLevelFinishedLoading;
		EventManager.AddHandler(EVENT.PlayerDied, OnPlayerDeath);
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnLevelFinishedLoading;
		EventManager.RemoveHandler(EVENT.PlayerDied, OnPlayerDeath);
		PlayerPrefs.Save();
	}

	private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		Reset();

	}

	public static void Reset()
	{
		HasSpawned = false;
		EventManager.Broadcast(EVENT.DisableInternals);
	}

	public static void SetViewerForControl(JoinedViewer viewer)
	{
		LocalViewerScript = viewer;
	}

	public static void SetPlayerForControl(GameObject playerObjToControl)
	{
		LocalPlayer = playerObjToControl;
		LocalPlayerScript = playerObjToControl.GetComponent<PlayerScript>();
		Equipment = playerObjToControl.GetComponent<Equipment>();

		PlayerScript =
			LocalPlayerScript; // Set this on the manager so it can be accessed by other components/managers
		Camera2DFollow.followControl.target = LocalPlayer.transform;


		HasSpawned = true;
	}

	private void OnPlayerDeath()
	{
		EventManager.Broadcast(EVENT.DisableInternals);
	}

	public int GetMobID()
	{
		mobIDcount++;
		return mobIDcount;
	}
}