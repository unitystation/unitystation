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
using UI.CharacterCreator;
using UnityEngine;
using UnityEngine.UI;
using Util.Independent.FluentRichText;

namespace UI.Systems.PreRound
{
	public class GUI_PreRoundWindow : SingletonManager<GUI_PreRoundWindow>
	{
		public PreRoundLoadingArea LoadingArea = null;
		public PreRoundButtonsScreen ButtonsArea = null;
		public PreRoundCountdownDisplay CountdownArea = null;

		public CharacterCustomization characterCustomization = null;

		public Action<string> OnClientLoadUpdateStatus;

		public GameObject adminPanel = null;

		private Toggle joinButton;
		private Button characterButton;


		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			SetInfoScreenOn();
			CountdownArea.OnFinishedCountingDown += ButtonsArea.RefreshGameModeText;
			_ = DelayChecks();
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			CountdownArea.OnFinishedCountingDown -= ButtonsArea.RefreshGameModeText;
		}

		private async UniTask DelayChecks()
		{
			await UniTask.WaitForSeconds(0.5f);
			PopulateWithStandardGameModeButtons();
			ButtonsArea.RefreshGameModeText();
		}

		private void UpdateMe()
		{
			if (Input.GetKeyDown(KeyCode.F7))
			{
				TryShowAdminPanel();
			}
		}

		private void TryShowAdminPanel()
		{
			if (PlayerList.HasTAGClient(TAG.MANAGE_ROUND_START))
			{
				adminPanel.SetActive(true);
			}
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

		public void PopulateLobbyScreenButtons(string gamemodeTitle, List<Tuple<string, System.Action>> buttonActions)
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
			if (PlayerList.HasTAGClient(TAG.MANAGE_ROUND_START))
			{
				ButtonsArea.CreateInteractableButton("[Admin] Start Now", StartNowButton);
			}
			joinButton = ButtonsArea.CreateInteractableToggle(CountdownArea.IsCountingDown ? "Ready Up!" : "Join Round", OnJoinButton);
			characterButton = ButtonsArea.CreateInteractableButton("Character", OnCharacterButton);

			CountdownArea.OnFinishedCountingDown += () =>
			{
				joinButton.interactable = true;
				joinButton.GetComponentInChildren<TMP_Text>().text = "Join Round";
			};
		}

		public void StartNowButton()
		{
			AdminCommandsManager.Instance.CmdStartRound();
		}

		public void OnCharacterButton()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			characterCustomization.SetActive(true);
		}

		/// <summary>
		/// Show the job select screen
		/// </summary>
		public void OnJoinButton(bool isOn)
		{
			if (HasCharacters() == false) return;
			if (NoJobWarn() == false) return;
			if (CountdownArea.IsCountingDown)
			{
				characterButton.interactable = !isOn;
				joinButton.GetComponentInChildren<TMP_Text>().text = (!isOn) ? "Ready" : "Unready";
				PlayerManager.LocalViewerScript?.SetReady(isOn);
			}
			else
			{
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
			return false;
		}

		private void SetInfoScreenOn()
		{
			InfoPanelMessageClient.Send();
		}
	}
}
