using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

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
		TeamSelect,
		JobSelect
	}
	public GameObject hudBottomHuman;
	public GameObject hudBottomGhost;
	public GameObject jobSelectWindow;
	public GameObject preRoundWindow;
	public GameObject teamSelectionWindow;
	public RectTransform panelRight;

	[SerializeField]
	private GameObject rightClickManager;

	[SerializeField] private Animator uiAnimator;
	[SerializeField] private VideoPlayerController videoController;
	public VideoPlayerController VideoPlayer => videoController;

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.PlayerSpawned, HumanUI);
		EventManager.AddHandler(EVENT.GhostSpawned, GhostUI);
		EventManager.AddHandler(EVENT.PlayerRejoined, RejoinedEvent);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.PlayerSpawned, HumanUI);
		EventManager.RemoveHandler(EVENT.GhostSpawned, GhostUI);
		EventManager.RemoveHandler(EVENT.PlayerRejoined, RejoinedEvent);
	}

	void RejoinedEvent()
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
		SoundManager.StopAmbient();
		SoundManager.SongTracker.StartPlayingRandomPlaylist();
		ResetUI(); //Make sure UI is back to default for next play
		UIManager.PlayerHealthUI.gameObject.SetActive(false);
		UIActionManager.Instance.OnRoundEnd();
		hudBottomHuman.SetActive(false);
		hudBottomGhost.SetActive(false);
		panelRight.gameObject.SetActive(false);
		rightClickManager.SetActive(false);
		jobSelectWindow.SetActive(false);
		teamSelectionWindow.SetActive(false);
		preRoundWindow.SetActive(false);
		GUI_IngameMenu.Instance.disclamerWindow.SetActive(true);
	}

	public void SetScreenForGame()
	{
		GUI_IngameMenu.Instance.disclamerWindow.SetActive(false);
		hudBottomHuman.SetActive(false);
		hudBottomGhost.SetActive(false);
		UIManager.PlayerHealthUI.gameObject.SetActive(true);
		panelRight.gameObject.SetActive(true);
		rightClickManager.SetActive(false);
		uiAnimator.Play("idle");
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
		preRoundWindow.SetActive(true);
	}

	public void SetScreenForTeamSelect()
	{
		preRoundWindow.SetActive(false);
		teamSelectionWindow.SetActive(true);
	}

	public void SetScreenForJobSelect()
	{
		preRoundWindow.SetActive(false);
		jobSelectWindow.SetActive(true);
	}
	
	public void PlayStrandedVideo()
	{
		uiAnimator.Play("StrandedVideo");
	}
}