using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUI_PreRoundWindow : MonoBehaviour
{
	[SerializeField]
	private TMP_Text currentGameMode = null;
	[SerializeField]
	private TMP_Text timer = null;
	[SerializeField]
	private TMP_Text playerCount = null;

	[SerializeField]
	private GameObject adminPanel = null;
	[SerializeField]
	private GameObject playerWaitPanel = null;
	[SerializeField]
	private GameObject countdownPanel = null;
	[SerializeField]
	private GameObject characterCustomization = null;
	[SerializeField]
	private Button characterButton = null;
	[SerializeField]
	private TMP_Text readyText = null;

	private bool doCountdown;
	private float countdownTime;
	private bool isReady;

	private void OnEnable()
	{
		if (GameManager.Instance.CurrentRoundState == RoundState.PreRound)
		{
			// In pre-round so setup button for readying
			SetReady(false);
		}
		else
		{
			// Not in pre-round so setup button for joining
			readyText.text = "Join now!";
		}
	}

	void OnDisable()
	{
		doCountdown = false;
		adminPanel.SetActive(false);
	}

	void Update()
	{
		// TODO: remove once admin system is in
		if (Input.GetKeyDown(KeyCode.F7) && !BuildPreferences.isForRelease)
		{
			adminPanel.SetActive(true);
		}

		if (doCountdown)
		{
			countdownTime -= Time.deltaTime;
			if (countdownTime <= 0)
			{
				OnCountdownEnd();
			}
		}

		UpdateUI();
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
			// Change the ready button to a join button so the player can still edit their character
			readyText.text = "Join now!";

		}
	}
	public void UpdateUI()
	{
		if (PlayerList.Instance == null) return;
		playerCount.text = PlayerList.Instance.ClientConnectedPlayers.Count.ToString();
		currentGameMode.text = GameManager.Instance.GetGameModeName();
		timer.text = TimeSpan.FromSeconds(this.countdownTime).ToString(@"mm\:ss");
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

	public void SyncCountdown(bool started, float time)
	{
		Logger.Log($"SyncCountdown called with: started={started}, time={time}", Category.Round);
		countdownTime = time;
		doCountdown = started;
		UpdateUI();
		countdownPanel.SetActive(started);
		playerWaitPanel.SetActive(!started);
	}

	public void OnCharacterButton()
	{
		SoundManager.Play("Click01");
		characterCustomization.SetActive(true);
	}

	public void OnReadyButton()
	{
		SoundManager.Play("Click01");

		if (GameManager.Instance.CurrentRoundState == RoundState.PreRound)
		{
			// Player can only toggle ready status in pre-round phase
			SetReady(!isReady);
		}
		else
		{
			// Let the player choose a job when not in pre-round phase
			UIManager.Display.SetScreenForJobSelect();
		}
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
}
