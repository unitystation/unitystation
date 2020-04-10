using System;
using UnityEngine;
using UnityEngine.UI;

public class GUI_PreRoundWindow : MonoBehaviour
{
	[SerializeField]
	private Text currentGameMode = null;
	[SerializeField]
	private Text timer = null;
	[SerializeField]
	private Text playerCount = null;

	[SerializeField]
	private GameObject adminPanel = null;
	[SerializeField]
	private GameObject playerWaitPanel = null;
	[SerializeField]
	private GameObject countdownPanel = null;
	[SerializeField]
	private GameObject characterCustomization = null;

	private bool doCountdown;
	private float countdownTime;

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
				doCountdown = false;
				// Server should tell client what to do at the end of the countdown
			}
		}

		UpdateUI();
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
}
