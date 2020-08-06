using UnityEngine;
using System.Collections;
using Audio.Managers;
using Audio.Containers;
using DatabaseAPI;
using ServerInfo;

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
	public GameObject hudBottomGhost;
	public GameObject jobSelectWindow;
	public GameObject teamSelectionWindow;
	public GameObject disclaimer;
	public RectTransform panelRight;
	public GUI_PreRoundWindow preRoundWindow;

	[SerializeField]
	private GameObject rightClickManager = null;

	[SerializeField] private Animator uiAnimator = null;
	[SerializeField] private VideoPlayerController videoController = null;
	public VideoPlayerController VideoPlayer => videoController;

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.PlayerSpawned, HumanUI);
		EventManager.AddHandler(EVENT.GhostSpawned, GhostUI);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.PlayerSpawned, HumanUI);
		EventManager.RemoveHandler(EVENT.GhostSpawned, GhostUI);
	}

	public void RejoinedEvent()
	{
		//for some reason this is getting called when ControlDisplays is already destroyed when client rejoins while
		//a ghost, this check prevents a MRE
		if (!this) return;
		StartCoroutine(DetermineRejoinUI());
	}

	IEnumerator DetermineRejoinUI()
	{
		//Wait for the assigning
		while (PlayerManager.LocalPlayerScript == null)
		{
			yield return WaitFor.EndOfFrame;
		}

		if (PlayerManager.LocalPlayerScript.playerHealth == null)
		{
			GhostUI();
		}
		else
		{
			HumanUI();
		}
	}

	void Update()
	{
		TempFixMissingRightHud();
	}

	//Temp fix for strange bug where right hud is missing when joining headless server
	void TempFixMissingRightHud()
	{
		if (CustomNetworkManager.Instance == null) return;
		if (CustomNetworkManager.Instance._isServer) return;
		if (PlayerManager.LocalPlayerScript == null) return;
		if (PlayerManager.LocalPlayerScript.playerHealth == null) return;
		if (!PlayerManager.LocalPlayerScript.playerHealth.IsDead &&
		    !UIManager.PlayerHealthUI.gameObject.activeInHierarchy)
		{
			UIManager.PlayerHealthUI.gameObject.SetActive(true);
		}
		if (!PlayerManager.LocalPlayerScript.playerHealth.IsDead &&
		    !UIManager.PlayerHealthUI.humanUI)
		{
			UIManager.PlayerHealthUI.humanUI = true;
		}

	}

	void HumanUI()
	{
		if (hudBottomHuman != null && hudBottomGhost != null)
		{
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
			hudBottomHuman.SetActive(false);
			hudBottomGhost.SetActive(true);
		}
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
		ResetUI(); //Make sure UI is back to default for next play
		UIManager.PlayerHealthUI.gameObject.SetActive(false);
		UIActionManager.Instance.OnRoundEnd();
		hudBottomHuman.SetActive(false);
		hudBottomGhost.SetActive(false);
		panelRight.gameObject.SetActive(false);
		rightClickManager.SetActive(false);
		jobSelectWindow.SetActive(false);
		teamSelectionWindow.SetActive(false);
		preRoundWindow.gameObject.SetActive(false);
		disclaimer.SetActive(true);
		UIManager.Instance.adminChatButtons.transform.parent.gameObject.SetActive(false);
	}

	public void SetScreenForGame()
	{
		hudBottomHuman.SetActive(false);
		hudBottomGhost.SetActive(false);
		UIManager.PlayerHealthUI.gameObject.SetActive(true);
		panelRight.gameObject.SetActive(true);
		rightClickManager.SetActive(false);
		uiAnimator.Play("idle");
		disclaimer.SetActive(false);
		preRoundWindow.gameObject.SetActive(true);
		preRoundWindow.SetUIForMapLoading();
	}

	public void SetScreenForPreRound()
	{
		ResetUI(); //Make sure UI is back to default for next play
		UIManager.PlayerHealthUI.gameObject.SetActive(false);
		hudBottomHuman.SetActive(false);
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
		ResetUI(); //Make sure UI is back to default for next play
		UIManager.PlayerHealthUI.gameObject.SetActive(false);
		hudBottomHuman.SetActive(false);
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