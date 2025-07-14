using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AdminCommands;
using Cysharp.Threading.Tasks;
using Managers;
using Messages.Client.Lobby;
using Mirror;
using Shared.Managers;
using TMPro;
using UI.Character;
using UI.CharacterCreator;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Util.Independent.FluentRichText;

namespace UI.Systems.PreRound
{
	public class GUI_PreRoundWindow : SingletonManager<GUI_PreRoundWindow>
	{
		public PreRoundLoadingWait LoadingWait = null;
		public Transform ActiveContentArea = null;
		public PreRoundLoadingArea LoadingArea = null;
		public PreRoundButtonsScreen ButtonsArea = null;
		public PreRoundCountdownDisplay CountdownArea = null;

		public GameObject characterCustomization = null;

		public Action<string> OnClientLoadUpdateStatus;

		public GameObject adminPanel = null;

		private Toggle joinButton;
		private Button characterButton;
		private Button adminStartButton;


		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			SetInfoScreenOn();
			CountdownArea.OnFinishedCountingDown.AddListener(ButtonsArea.RefreshGameModeText);
			CountdownArea.OnFinishedCountingDown.AddListener(CheckForRoundStatusForTitle);
			CountdownArea.OnFinishedCountingDown.AddListener(ChangeJoinButtonForRoundStarted);
			_ = DelayedCheck();
		}

		private async UniTask DelayedCheck()
		{
			SwitchToLoadingPage();
			ButtonsArea.SetTitle("Loading..");
			// This might seem like an unnecessary delay, but it's actually required because the game likes to hang while loading; which delays the receiving of specific info from net messages
			// that update the state of the round to players.
			await UniTask.WaitForSeconds(2f);
			await UniTask.WaitUntil(IsClientDoneLoadingScenes);
			await UniTask.WaitUntil(IsLoadingAreaNoLongerActive);
			LoadingArea?.UpdateLoadingBar("Awaiting Server..", "Preparing Player", 0.85f);
			await UniTask.WaitUntil(IsServerDonePreparingThePlayer);
			LoadingArea?.UpdateLoadingBar("Awaiting Server..", "Finished Preparing Player", 2f);
			HideLoadingArea();
			ButtonsArea.RefreshGameModeText();
			CheckForRoundStatusForTitle(); // maybe find a way to make this reactive with networked events?
			PopulateWithStandardGameModeButtons();
			SwitchToMainPage();
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			CountdownArea.OnFinishedCountingDown.RemoveListener(ButtonsArea.RefreshGameModeText);
			CountdownArea.OnFinishedCountingDown.RemoveListener(CheckForRoundStatusForTitle);
			CountdownArea.OnFinishedCountingDown.RemoveListener(ChangeJoinButtonForRoundStarted);
			GameManager.Instance.OnCurrentRoundStateChange -= ChangeJoinButtonForRoundStarted;
		}

		private void UpdateMe()
		{
			if (Input.GetKeyDown(KeyCode.F7))
			{
				TryShowAdminPanel();
			}

			CheckForRoundStatusForTitle();
		}

		private void TryShowAdminPanel()
		{
			if (PlayerList.HasTAGClient(TAG.MANAGE_ROUND_START))
			{
				adminPanel.SetActive(true);
			}
		}

		private bool IsClientDoneLoadingScenes()
		{
			if (CustomNetworkManager.IsServer == false)
			{
				return SubSceneManager.Instance.ClientIsFullyDoneLoadingOnSubsceneManager;
			}
			return SubSceneManager.Instance.clientIsLoadingSubscene == false;
		}

		private bool IsLoadingAreaNoLongerActive()
		{
			return LoadingArea.gameObject.activeSelf == false;
		}

		private bool IsServerDonePreparingThePlayer()
		{
			return PlayerManager.LocalViewerScript?.ServerDoneLoading == true;
		}

		private IEnumerator WaitForInitialisation()
		{
			var maxWaitTime = 0;
			yield return WaitFor.EndOfFrame;

			if (GameManager.Instance.QuickJoinLoad)
			{
				while (SubsystemMatrixQueueInit.InitializedAll == false || maxWaitTime < 150)
				{
					yield return WaitFor.Seconds(0.55f);
					maxWaitTime++;
				}
				StartNowButton();
			}
		}

		public void HideLoadingArea()
		{
			LoadingArea.SetActive(false);
		}

		public void PopulateLobbyScreenWithButtons(string gamemodeTitle, List<Tuple<string, System.Action>> buttonActions)
		{
			ButtonsArea.SetTitle(gamemodeTitle);
			foreach (var ba in buttonActions)
			{
				ButtonsArea.CreateInteractableButton(ba.Item1, ba.Item2);
			}
			StartCoroutine(WaitForInitialisation());
		}

		public void PopulateWithStandardGameModeButtons()
		{
			ButtonsArea.ClearAllButtons();
			AddStartNowButtonForAdmins();
			joinButton = ButtonsArea.CreateInteractableToggle(GameManager.Instance.CurrentRoundState == RoundState.PreRound ? "Ready Up!" : "Join Round", OnJoinButton);
			characterButton = ButtonsArea.CreateInteractableButton("Character", OnCharacterButton);
			CountdownArea.OnFinishedCountingDown.AddListener(ChangeJoinButtonForRoundStarted);
			GameManager.Instance.OnCurrentRoundStateChange += ChangeJoinButtonForRoundStarted;
		}

		private void ChangeJoinButtonForRoundStarted()
		{
			joinButton.interactable = true;
			joinButton.GetComponentInChildren<TMP_Text>().text = GameManager.Instance.CurrentRoundState == RoundState.PreRound ? "Ready Up!" : "Join Round";
			CheckForRoundStatusForTitle();
		}

		private void AddStartNowButtonForAdmins()
		{
			if (adminStartButton != null)
			{
				adminStartButton.gameObject.SetActive(true);
				return;
			}
			if (PlayerList.HasTAGClient(TAG.MANAGE_ROUND_START))
			{
				adminStartButton = ButtonsArea.CreateInteractableButton("[A] Start Now".Color(Color.yellow), StartNowButton);
			}
		}

		public void StartNowButton()
		{
			AdminCommandsManager.Instance.CmdStartRound();
			CheckForRoundStatusForTitle();
		}

		public void OnCharacterButton()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			if (characterCustomization == null)
			{
				characterCustomization = FindAnyObjectByType<CharacterSettings>().gameObject;
			}
			characterCustomization.SetActive(true);
		}

		/// <summary>
		/// Show the job select screen
		/// </summary>
		public void OnJoinButton(bool isOn)
		{
			AddStartNowButtonForAdmins();
			ChangeJoinButtonForRoundStarted();
			if (HasCharacters() == false) return;
			//if (NoJobWarn() == false) return;
			if (GameManager.Instance.CurrentRoundState == RoundState.PreRound)
			{
				characterButton.interactable = !isOn;
				joinButton.GetComponentInChildren<TMP_Text>().text = (!isOn) ? "Ready" : "Unready";
				PlayerManager.LocalViewerScript?.SetReady(isOn);
			}
			else
			{
				if (SubSceneManager.Instance.clientIsLoadingSubscene)
				{
					ModalPanelManager.Instance.Inform("The game is still loading, please wait few more seconds.");
				}
				UIManager.Display.SetScreenForJobSelect();
			}
		}

		/// <summary>
		/// Warns the player when they have no job selected and default their job preference
		/// </summary>
		/// <param name="noJob"></param>
		private bool NoJobWarn()
		{
			bool hasPreferences = PlayerManager.ActiveCharacter.JobPreferences.Count != 0;
			if (hasPreferences)
			{
				return true;
			}
			ModalPanelManager.Instance.Inform("No job preferences found. Please select some in your character sheet.");
			return false;
		}

		private bool HasCharacters()
		{
			bool hasCharacters = PlayerManager.CharacterManager.ActiveCharacter != null;
			if (hasCharacters)
			{
				return true;
			}
			characterCustomization.SetActive(true);
			Chat.AddExamineMsgToClient("No active character sheet detected".Color(Color.red));
			ModalPanelManager.Instance.Inform("No character sheet detected. Please create or choose one.");
			return false;
		}

		private void SetInfoScreenOn()
		{
			InfoPanelMessageClient.Send();
		}

		private void CheckForRoundStatusForTitle()
		{
			switch (GameManager.Instance.CurrentRoundState)
			{
				case RoundState.PreRound:
					ButtonsArea.SetTitle("Waiting for new round..");
					break;
				case RoundState.Started:
					ButtonsArea.SetTitle("Welcome to UnityStation.");
					break;
				case RoundState.Ended:
				case RoundState.Restarting:
					ButtonsArea.SetTitle("Shift has ended!");
					break;
				case RoundState.None:
				default:
					ButtonsArea.SetTitle("Welcome to UnityStation!");
					break;
			}
		}

		public void SwitchToLoadingPage()
		{
			ActiveContentArea.gameObject.SetActive(false);
			LoadingWait.gameObject.SetActive(true);
		}

		public void SwitchToMainPage()
		{
			ActiveContentArea.gameObject.SetActive(true);
			LoadingWait.gameObject.SetActive(false);
		}
	}
}
