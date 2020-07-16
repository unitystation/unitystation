using System;
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
	private Button characterButton = null;

	public GameObject serverInfo;

	// Internal variables
	private bool doCountdown;
	private double countdownEndTime;
	private bool isReady;

	public static GUI_PreRoundWindow Instance;

	void Awake()
	{
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
			if(PlayerList.Instance.AdminToken == null) return;
			adminPanel.SetActive(true);
		}

		if (doCountdown)
		{
			UpdateCountdownUI();
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
	}

	public void UpdatePlayerCount(int count)
	{
		playerCount.text = count.ToString();
		currentGameMode.text = GameManager.Instance.GetGameModeName();
	}

	public void StartNowButton()
	{
		ServerCommandVersionOneMessageClient.Send(ServerData.UserID, PlayerList.Instance.AdminToken, "CmdStartRound");
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
		SoundManager.Play("Click01");
		characterCustomization.SetActive(true);
	}

	/// <summary>
	/// Toggle isReady and update the UI
	/// </summary>
	public void OnReadyButton()
	{
		SoundManager.Play("Click01");
		SetReady(!isReady);
	}

	/// <summary>
	/// Show the job select screen
	/// </summary>
	public void OnJoinButton()
	{
		SoundManager.Play("Click01");
		UIManager.Display.SetScreenForJobSelect();
	}

	/// <summary>
	/// Sets the new ready status. Will tell the server about the new ready state if it has changed.
	/// </summary>
	/// <param name="ready"></param>
	private void SetReady(bool ready)
	{
		if (isReady != ready)
		{
			// Ready status changed so tell the server
			PlayerManager.LocalViewerScript.SetReady(ready);
		}
		isReady = ready;
		characterButton.interactable = !ready;
		readyText.text = (!ready) ? "Ready" : "Unready";
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
