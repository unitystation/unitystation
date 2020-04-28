using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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

	// Character objects
	private GameObject characterCustomization = null;
	[SerializeField]
	private Button characterButton = null;

	// Internal variables
	private bool doCountdown;
	private float countdownTime;
	private bool isReady;

	private void OnDisable()
	{
		doCountdown = false;
		isReady = false;
		adminPanel.SetActive(false);
	}

	private void Update()
	{
		// TODO: remove once admin system is in
		if (Input.GetKeyDown(KeyCode.F7) && !BuildPreferences.isForRelease)
		{
			adminPanel.SetActive(true);
		}

		if (doCountdown)
		{
			UpdateCountdown();
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

	private void UpdateCountdown()
	{
		countdownTime -= Time.deltaTime;
		if (countdownTime <= 0)
		{
			OnCountdownEnd();
		}
		timer.text = TimeSpan.FromSeconds(countdownTime).ToString(@"mm\:ss");
	}

	public void UpdatePlayerCount(int count)
	{
		playerCount.text = count.ToString();
		currentGameMode.text = GameManager.Instance.GetGameModeName();
	}

	public void StartNowButton()
	{
		if (CustomNetworkManager.Instance._isServer == false)
		{
			Logger.LogError("Can only execute command from server.", Category.DebugConsole);
			return;
		}
		GameManager.Instance.StartRound();
	}

	public void SyncCountdown(bool started, long endTime)
	{
		Logger.Log($"SyncCountdown called with: started={started}, endTime={endTime}", Category.Round);
		TimeSpan timeTillEnd = DateTimeOffset.FromUnixTimeMilliseconds(endTime) - DateTimeOffset.UtcNow;
		countdownTime = (float)timeTillEnd.TotalSeconds;
		doCountdown = started;
		if (started)
		{
			SetUIForCountdown();
		}
		else
		{
			SetUIForWaiting();
		}
		// Update now so timer doesn't flash 0:00
		UpdateCountdown();
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

	/// <summary>
	/// Show waiting for players text
	/// </summary>
	public void SetUIForWaiting()
	{
		timerPanel.SetActive(false);
		joinPanel.SetActive(false);
		playerWaitPanel.SetActive(true);
		mainPanel.SetActive(false);
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
	}
}
