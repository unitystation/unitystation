using System;
using UnityEngine;
using System.Collections;
using Audio.Managers;
using Audio.Containers;
using Blob;
using DatabaseAPI;
using JetBrains.Annotations;
using ServerInfo;
using UI.Systems.Ghost;
using UI.Action;

namespace UI
{
	public class ControlDisplays : MonoBehaviour
	{
		/// <summary>
		/// Represents which screen to open with generic function
		/// </summary>
		public enum Screens
		{
			SlotReset,
			Lobby,
			Game,
			PreRound,
			Joining,
			TeamSelect,
			JobSelect
		}

		public GameObject hudBottomHuman;
		public UI_GhostOptions hudBottomGhost;
		public GameObject hudBottomBlob;
		public GameObject hudBottomAi;
		public GameObject currentHud;

		public GameObject jobSelectWindow;
		public GameObject teamSelectionWindow;
		[CanBeNull] public GameObject disclaimer;
		public RectTransform panelRight;
		public GUI_PreRoundWindow preRoundWindow;

		[SerializeField]
		private GameObject rightClickManager = null;

		[SerializeField] private Animator uiAnimator = null;
		[SerializeField] private VideoPlayerController videoController = null;
		public VideoPlayerController VideoPlayer => videoController;

		private void OnEnable()
		{
			EventManager.AddHandler(Event.PlayerSpawned, DetermineUI);
			EventManager.AddHandler(Event.GhostSpawned, DetermineUI);
			EventManager.AddHandler(Event.BlobSpawned, DetermineUI);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.PlayerSpawned, DetermineUI);
			EventManager.RemoveHandler(Event.GhostSpawned, DetermineUI);
			EventManager.RemoveHandler(Event.BlobSpawned, DetermineUI);
		}

		public void RejoinedEvent()
		{
			// for some reason this is getting called when ControlDisplays is already destroyed when client rejoins while
			// a ghost, this check prevents a MRE
			if (!this) return;
			StartCoroutine(DetermineRejoinUI());
		}

		private IEnumerator DetermineRejoinUI()
		{
			// Wait for the assigning
			while (PlayerManager.LocalPlayerScript == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			DetermineUI();
		}

		private void DetermineUI()
		{
			// TODO: make better system for handling lots of different UIs
			if (PlayerManager.LocalPlayerScript.PlayerState == PlayerScript.PlayerStates.Blob)
			{
				SetUi(hudBottomBlob);
				PlayerManager.LocalPlayerScript.GetComponent<BlobPlayer>()?.TurnOnClientLight();
			}
			else if (PlayerManager.LocalPlayerScript.PlayerState == PlayerScript.PlayerStates.Ai)
			{
				SetUi(hudBottomAi);
			}
			else if (PlayerManager.LocalPlayerScript.playerHealth == null)
			{
				SetUi(hudBottomGhost.gameObject);
			}
			else
			{
				SetUi(hudBottomHuman);
			}
		}

		private void SetUi(GameObject newUi)
		{
			if (currentHud == null)
			{
				currentHud = hudBottomHuman;
			}

			if (newUi == hudBottomGhost.gameObject)
			{
				hudBottomGhost.AdminGhostInventory.SetActive(PlayerList.Instance.IsClientAdmin);
			}

			//Turn off old UI
			ToggleCurrentHud(false);

			currentHud = newUi;

			//Turn on new UI
			ToggleCurrentHud(true);

			UIManager.PlayerHealthUI.gameObject.SetActive(true);
			panelRight.gameObject.SetActive(true);
			rightClickManager.SetActive(true);
			preRoundWindow.gameObject.SetActive(false);
			MusicManager.SongTracker.Stop();
		}

		/// <summary>
		/// Generic UI changing function for net messages
		/// </summary>
		/// <param name="screen">The UI action to perform</param>
		public void SetScreenFor(Screens screen)
		{
			Logger.Log($"Setting screen for {screen}", Category.UI);
			switch (screen)
			{
				case Screens.SlotReset:
					ResetUI();
					break;
				case Screens.Lobby:
					SetScreenForLobby();
					break;
				case Screens.Game:
					SetScreenForGame();
					break;
				case Screens.PreRound:
					SetScreenForPreRound();
					break;
				case Screens.Joining:
					SetScreenForJoining();
					break;
				case Screens.TeamSelect:
					SetScreenForTeamSelect();
					break;
				case Screens.JobSelect:
					SetScreenForJobSelect();
					break;
			}
		}

		/// <summary>
		///     Clears all of the UI slot items
		/// </summary>
		public void ResetUI()
		{
			foreach (UI_ItemSlot itemSlot in GetComponentsInChildren<UI_ItemSlot>())
			{
				itemSlot.Reset();
			}
		}

		public void SetScreenForLobby()
		{
			SoundAmbientManager.StopAllAudio();
			MusicManager.SongTracker.StartPlayingRandomPlaylist();
			ResetUI(); // Make sure UI is back to default for next play
			UIManager.PlayerHealthUI.gameObject.SetActive(false);
			UIActionManager.Instance.OnRoundEnd();
			ToggleCurrentHud(false);
			panelRight.gameObject.SetActive(false);
			rightClickManager.SetActive(false);
			jobSelectWindow.SetActive(false);
			teamSelectionWindow.SetActive(false);
			preRoundWindow.gameObject.SetActive(false);
			if (disclaimer != null) disclaimer.SetActive(true);
			UIManager.Instance.adminChatButtons.transform.parent.gameObject.SetActive(false);
			UIManager.Instance.mentorChatButtons.transform.parent.gameObject.SetActive(false);
		}

		public void SetScreenForGame()
		{
			ToggleCurrentHud(false);
			UIManager.PlayerHealthUI.gameObject.SetActive(true);
			panelRight.gameObject.SetActive(true);
			rightClickManager.SetActive(false);
			uiAnimator.Play("idle");
			if (disclaimer != null) disclaimer.SetActive(false);
			preRoundWindow.gameObject.SetActive(true);
			preRoundWindow.SetUIForMapLoading();
		}

		public void SetScreenForPreRound()
		{
			ResetUI(); // Make sure UI is back to default for next play
			UIManager.PlayerHealthUI.gameObject.SetActive(false);
			ToggleCurrentHud(false);
			panelRight.gameObject.SetActive(false);
			rightClickManager.SetActive(false);
			jobSelectWindow.SetActive(false);
			teamSelectionWindow.SetActive(false);
			preRoundWindow.gameObject.SetActive(true);
			preRoundWindow.SetUIForCountdown();

			ServerInfoMessageClient.Send();
		}

		public void SetScreenForJoining()
		{
			ResetUI(); // Make sure UI is back to default for next play
			UIManager.PlayerHealthUI.gameObject.SetActive(false);
			ToggleCurrentHud(false);
			panelRight.gameObject.SetActive(false);
			rightClickManager.SetActive(false);
			jobSelectWindow.SetActive(false);
			teamSelectionWindow.SetActive(false);
			preRoundWindow.gameObject.SetActive(true);
			preRoundWindow.SetUIForJoining();
		}

		private void ToggleCurrentHud(bool toggle)
		{
			hudBottomHuman.SetActive(false);
			hudBottomGhost.SetActive(false);
			hudBottomBlob.SetActive(false);
			hudBottomAi.SetActive(false);

			if (currentHud == null) return;

			currentHud.SetActive(toggle);
		}

		public void SetScreenForTeamSelect()
		{
			preRoundWindow.gameObject.SetActive(false);
			teamSelectionWindow.SetActive(true);
		}

		public void SetScreenForJobSelect()
		{
			preRoundWindow.gameObject.SetActive(false);
			jobSelectWindow.SetActive(true);
		}

		public void PlayStrandedVideo()
		{
			uiAnimator.Play("StrandedVideo");
		}
	}
}
