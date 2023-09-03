using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using AdminCommands;
using Logs;
using Messages.Client.Lobby;
using Systems.Character;
using UI.Character;

namespace UI
{
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

		[SerializeField] private GameObject notEnoughReady = null;

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

		// Internal variables
		private bool doCountdown;
		private double countdownEndTime;
		private bool isReady;

		public static GUI_PreRoundWindow Instance;

		private bool startedAlready = false;

		private void Awake()
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

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			EventManager.AddHandler(Event.PostRoundStarted, OnCountdownEnd);
			SetInfoScreenOn();
		}

		private void OnDisable()
		{
			startedAlready = false;
			doCountdown = false;
			isReady = false;
			adminPanel.SetActive(false);
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			EventManager.RemoveHandler(Event.PostRoundStarted, OnCountdownEnd);
		}

		private void UpdateMe()
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
				notEnoughReady.SetActive(true);
				return;
			}

			timer.text = TimeSpan.FromSeconds(countdownEndTime - NetworkTime.time).ToString(@"mm\:ss");

			if (GameManager.Instance.QuickLoad && mapLoadingPanel.activeSelf == false)
			{
				if (startedAlready == true || this.isActiveAndEnabled == false) return;
				startedAlready = true;
				StartCoroutine(WaitForInitialisation());
			}
		}

		private IEnumerator WaitForInitialisation()
		{
			yield return null;

			if (GameManager.Instance.QuickJoinLoad)
			{
				SetReady(true);
			}

			StartNowButton();
		}

		public void UpdatePlayerCount(int count)
		{
			playerCount.text = count.ToString();
			currentGameMode.text = GameManager.Instance.GetGameModeName();
		}

		public void StartNowButton()
		{
			AdminCommandsManager.Instance.CmdStartRound();
		}

		public void SyncCountdown(bool started, double endTime)
		{
			Loggy.LogFormat("SyncCountdown called with: started={0}, endTime={1}, current NetworkTime={2}", Category.Round,
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
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			characterCustomization.SetActive(true);
		}

		/// <summary>
		/// Toggle isReady and update the UI
		/// </summary>
		public void OnReadyButton()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			if (PlayerManager.ActiveCharacter == null)
			{
				warnText.SetActive(true);
				return;
			}
			SetReady(!isReady);
			TryShowAdminPanel();
		}

		/// <summary>
		/// Show the job select screen
		/// </summary>
		public void OnJoinButton()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			if (PlayerManager.CharacterManager.Characters.Any() == false)
			{
				characterCustomization.SetActive(true);
				Chat.AddExamineMsgToClient("<color=red>No character sheets detected.</color>");
				return;
			}
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
			InfoPanelMessageClient.Send();
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
		}

		/// <summary>
		/// Show round started and join button
		/// </summary>
		public void SetUIForJoining()
		{
			notEnoughReady.SetActive(false);
			warnText.SetActive(false);
			joinPanel.SetActive(true);
			timerPanel.SetActive(false);
			playerWaitPanel.SetActive(false);
			mainPanel.SetActive(true);
			rejoiningRoundPanel.SetActive(false);
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
			if (PlayerManager.CharacterManager.Characters.Any() == false)
			{
				characterCustomization.SetActive(true);
			}
		}
	}
}
