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

		void OnEnable()
		{
			EventManager.AddHandler(Event.PlayerSpawned, HumanUI);
			EventManager.AddHandler(Event.GhostSpawned, GhostUI);
			EventManager.AddHandler(Event.BlobSpawned, BlobUI);
		}

		void OnDisable()
		{
			EventManager.RemoveHandler(Event.PlayerSpawned, HumanUI);
			EventManager.RemoveHandler(Event.GhostSpawned, GhostUI);
			EventManager.RemoveHandler(Event.BlobSpawned, BlobUI);
		}

		public void RejoinedEvent()
		{
			// for some reason this is getting called when ControlDisplays is already destroyed when client rejoins while
			// a ghost, this check prevents a MRE
			if (!this) return;
			StartCoroutine(DetermineRejoinUI());
		}

		IEnumerator DetermineRejoinUI()
		{
			// Wait for the assigning
			while (PlayerManager.LocalPlayerScript == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			// TODO: make better system for handling lots of different UIs
			if (PlayerManager.LocalPlayerScript.IsPlayerSemiGhost)
			{
				BlobUI();
				PlayerManager.LocalPlayerScript.GetComponent<BlobPlayer>()?.TurnOnClientLight();
			}
			else if (PlayerManager.LocalPlayerScript.playerHealth == null)
			{
				GhostUI();
			}
			else
			{
				HumanUI();
			}
		}

		void HumanUI()
		{
			if (hudBottomHuman != null && hudBottomGhost != null)
			{
				hudBottomBlob.SetActive(false);
				hudBottomHuman.SetActive(true);
				hudBottomGhost.SetActive(false);
			}
			UIManager.PlayerHealthUI.gameObject.SetActive(true);
			panelRight.gameObject.SetActive(true);
			rightClickManager.SetActive(true);
			preRoundWindow.gameObject.SetActive(false);
			MusicManager.SongTracker.Stop();
		}

		void GhostUI()
		{
			if (hudBottomHuman != null && hudBottomGhost != null)
			{
				hudBottomBlob.SetActive(false);
				hudBottomHuman.SetActive(false);
				hudBottomGhost.SetActive(true);
				hudBottomGhost.AdminGhostInventory.SetActive(PlayerList.Instance.IsClientAdmin);
			}
			UIManager.PlayerHealthUI.gameObject.SetActive(true);
			panelRight.gameObject.SetActive(true);
			rightClickManager.SetActive(true);
			preRoundWindow.gameObject.SetActive(false);
			MusicManager.SongTracker.Stop();
		}

		void BlobUI()
		{
			if (hudBottomBlob != null && hudBottomHuman != null && hudBottomGhost != null)
			{
				hudBottomHuman.SetActive(false);
				hudBottomGhost.SetActive(false);
				hudBottomBlob.SetActive(true);
			}
			UIManager.PlayerHealthUI.gameObject.SetActive(false);
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
			hudBottomHuman.SetActive(false);
			hudBottomBlob.SetActive(false);
			hudBottomGhost.SetActive(false);
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
			hudBottomHuman.SetActive(false);
			hudBottomBlob.SetActive(false);
			hudBottomGhost.SetActive(false);
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
			hudBottomHuman.SetActive(false);
			hudBottomBlob.SetActive(false);
			hudBottomGhost.SetActive(false);
			panelRight.gameObject.SetActive(false);
			rightClickManager.SetActive(false);
			jobSelectWindow.SetActive(false);
			teamSelectionWindow.SetActive(false);
			preRoundWindow.gameObject.SetActive(true);
			preRoundWindow.SetUIForCountdown();

			ServerInfoMessageClient.Send(ServerData.UserID);
		}

		public void SetScreenForJoining()
		{
			ResetUI(); // Make sure UI is back to default for next play
			UIManager.PlayerHealthUI.gameObject.SetActive(false);
			hudBottomHuman.SetActive(false);
			hudBottomBlob.SetActive(false);
			hudBottomGhost.SetActive(false);
			panelRight.gameObject.SetActive(false);
			rightClickManager.SetActive(false);
			jobSelectWindow.SetActive(false);
			teamSelectionWindow.SetActive(false);
			preRoundWindow.gameObject.SetActive(true);
			preRoundWindow.SetUIForJoining();
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
