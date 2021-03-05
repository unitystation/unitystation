using System;
using System.Collections;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DatabaseAPI;
using ServerInfo;
using AdminCommands;

public class GUI_PreRoundWindow : MonoBehaviour
{
	// Text objects
	[SerializeField]
	private TMP_Text currentGameMode = null;
	[SerializeField]
	private TMP_Text timer = null;
	[SerializeField]
	private TMP_Text playerCount = null;
	[SerializeField]
	private TMP_Text readyText = null;


	[SerializeField] private TMP_Text loadingText = null;

	[SerializeField] private Scrollbar loadingBar = null;

	[SerializeField] private GameObject normalWindows = null;

	[SerializeField] private GameObject warnText = null;

	// UI panels
	[SerializeField]
	private GameObject adminPanel = null;
	[SerializeField]
	private GameObject playerWaitPanel = null;
	[SerializeField]
	private GameObject mainPanel = null;
	[SerializeField]
	private GameObject timerPanel = null;
	[SerializeField]
	private GameObject joinPanel = null;

	[SerializeField]
	private GameObject mapLoadingPanel = null;

	[SerializeField] private GameObject rejoiningRoundPanel = null;

	// Character objects
	[SerializeField]
	private GameObject characterCustomization = null;

	[SerializeField]
	private GUI_JobPreferences localJobPref = null;

	[SerializeField]
	private Button characterButton = null;

	public GameObject serverInfo;

	// Internal variables
	private bool doCountdown;
	private double countdownEndTime;
	private bool isReady;

	public static GUI_PreRoundWindow Instance;

	private bool startedAlready = false;

	void Awake()
	{
		//localJobPref = null;
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	private void OnDisable()
	{
		doCountdown = false;
		isReady = false;
		adminPanel.SetActive(false);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.F7))
		{
			TryShowAdminPanel();
		}

		if (doCountdown)
		{
			UpdateCountdownUI();
		}
	}

	private void TryShowAdminPanel()
	{
		if (PlayerList.Instance.AdminToken != null)
		{
			adminPanel.SetActive(true);
		}
	}

	private void OnCountdownEnd()
	{
		doCountdown = false;
		if (isReady)
		{
			// Server should spawn the player so hide this window
			gameObject.SetActive(false);
		}
		else
		{
			SetUIForJoining();
		}
	}

	/// <summary>
	/// Update the UI based on the current countdown time
	/// </summary>
	private void UpdateCountdownUI()
	{
		if (NetworkTime.time >= countdownEndTime)
		{
			OnCountdownEnd();
		}
		timer.text = TimeSpan.FromSeconds(countdownEndTime - NetworkTime.time).ToString(@"mm\:ss");

		if (GameManager.Instance.QuickLoad && mapLoadingPanel.activeSelf == false)
		{
			if (startedAlready == true) return;
			startedAlready = true;
			StartCoroutine(WaitForInitialisation());
		}
	}

	private IEnumerator WaitForInitialisation()
	{
		yield return null;
		SetReady(true);
		StartNowButton();
	}

	public void UpdatePlayerCount(int count)
	{
		playerCount.text = count.ToString();
		currentGameMode.text = GameManager.Instance.GetGameModeName();
	}

	public void StartNowButton()
	{
		AdminCommandsManager.Instance.CmdStartRound(ServerData.UserID, PlayerList.Instance.AdminToken);
	}

	public void SyncCountdown(bool started, double endTime)
	{
		Logger.LogFormat("SyncCountdown called with: started={0}, endTime={1}, current NetworkTime={2}", Category.Round,
			started, endTime, NetworkTime.time);
		countdownEndTime = endTime;
		doCountdown = started;
		if (started)
		{
			SetUIForCountdown();
			// Update the timer now so it doesn't flash 0:00
			UpdateCountdownUI();
		}
		else
		{
			SetUIForWaiting();
		}
	}

	public void OnCharacterButton()
	{
		SoundManager.Play(SingletonSOSounds.Instance.Click01);
		characterCustomization.SetActive(true);
	}

	/// <summary>
	/// Toggle isReady and update the UI
	/// </summary>
	public void OnReadyButton()
	{
		SoundManager.Play(SingletonSOSounds.Instance.Click01);
		SetReady(!isReady);
		TryShowAdminPanel();
	}

	/// <summary>
	/// Show the job select screen
	/// </summary>
	public void OnJoinButton()
	{

		SoundManager.Play(SingletonSOSounds.Instance.Click01);
		UIManager.Display.SetScreenForJobSelect();
	}

	/// <summary>
	/// Sets the new ready status. Will tell the server about the new ready state if it has changed.
	/// </summary>
	/// <param name="ready"></param>
	private void SetReady(bool ready)
	{
		NoJobWarn(localJobPref.JobPreferences.Count == 0);
		if (isReady != ready)
		{
			// Ready status changed so tell the server
			PlayerManager.LocalViewerScript.SetReady(ready);
		}
		isReady = ready;
		characterButton.interactable = !ready;
		readyText.text = (!ready) ? "Ready" : "Unready";
	}

	/// <summary>
	/// Warns the player when they have no job selected and default their job preference
	/// </summary>
	/// <param name="noJob"></param>
	private void NoJobWarn(bool noJob)
	{
		if (noJob)
		{
			warnText.SetActive(true);
			localJobPref.SetAssistantDefault();
		}
		else
		{
			warnText.SetActive(false);
		}
	}

	private void SetInfoScreenOn()
	{
		ServerInfoLobbyMessageClient.Send(ServerData.UserID);
		serverInfo.SetActive(false);
		if(string.IsNullOrEmpty(ServerInfoUI.serverDesc)) return;
		serverInfo.SetActive(true);
	}

	/// <summary>
	/// Show waiting for players text
	/// </summary>
	public void SetUIForWaiting()
	{
		timerPanel.SetActive(false);
		joinPanel.SetActive(false);
		playerWaitPanel.SetActive(true);
		mainPanel.SetActive(false);
		rejoiningRoundPanel.SetActive(false);

		SetInfoScreenOn();
	}

	/// <summary>
	/// Show timer and ready button
	/// </summary>
	public void SetUIForCountdown()
	{
		SetReady(isReady);
		timerPanel.SetActive(true);
		joinPanel.SetActive(false);
		playerWaitPanel.SetActive(false);
		mainPanel.SetActive(true);
		rejoiningRoundPanel.SetActive(false);

		SetInfoScreenOn();
	}

	/// <summary>
	/// Show round started and join button
	/// </summary>
	public void SetUIForJoining()
	{
		warnText.SetActive(false);
		joinPanel.SetActive(true);
		timerPanel.SetActive(false);
		playerWaitPanel.SetActive(false);
		mainPanel.SetActive(true);
		rejoiningRoundPanel.SetActive(false);
		SetInfoScreenOn();
	}

	public void ShowRejoiningPanel()
	{
		normalWindows.SetActive(false);
		mapLoadingPanel.SetActive(false);
		rejoiningRoundPanel.SetActive(true);
	}

	public void CloseRejoiningPanel()
	{
		normalWindows.SetActive(false);
		mapLoadingPanel.SetActive(false);
		rejoiningRoundPanel.SetActive(false);
	}

	public void SetUIForMapLoading()
	{
		rejoiningRoundPanel.SetActive(false);
		normalWindows.SetActive(false);
		mapLoadingPanel.SetActive(true);
	}

	public void UpdateLoadingBar(string text, float loadedAmt)
	{
		loadingText.text = text;
		loadingBar.size = loadedAmt;
	}

	public void CloseMapLoadingPanel()
	{
		normalWindows.SetActive(true);
		mapLoadingPanel.SetActive(false);
		UpdateLoadingBar("Preparing..", 0.1f);
	}
}
