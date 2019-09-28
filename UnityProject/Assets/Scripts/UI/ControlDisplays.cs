using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

public class ControlDisplays : MonoBehaviour
{
	public GameObject hudBottomHuman;
	public GameObject hudBottomGhost;
	public GameObject jobSelectWindow;
	public GameObject preRoundWindow;
	public GameObject teamSelectionWindow;
	public RectTransform panelRight;

	[SerializeField] private Animator uiAnimator;

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.PlayerSpawned, HumanUI);
		EventManager.AddHandler(EVENT.GhostSpawned, GhostUI);
		EventManager.AddHandler(EVENT.PlayerRejoined, RejoinedEvent);
	}

	void RejoinedEvent()
	{
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
		if (hudBottomGhost != null && hudBottomGhost != null)
		{
			hudBottomHuman.gameObject.SetActive(true);
			hudBottomGhost.gameObject.SetActive(false);
		}
	}

	void GhostUI()
	{
		if (hudBottomGhost != null && hudBottomGhost != null)
		{
			hudBottomHuman.gameObject.SetActive(false);
			hudBottomGhost.gameObject.SetActive(true);
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
		SoundManager.PlayRandomTrack(); //Gimme dat slap bass
		ResetUI(); //Make sure UI is back to default for next play
		UIManager.PlayerHealthUI.gameObject.SetActive(false);
		hudBottomHuman.gameObject.SetActive(false);
		hudBottomGhost.gameObject.SetActive(false);
		panelRight.gameObject.SetActive(false);
		jobSelectWindow.SetActive(false);
		teamSelectionWindow.SetActive(false);
		preRoundWindow.SetActive(false);
	}

	public void SetScreenForGame()
	{
		hudBottomHuman.gameObject.SetActive(false);
		hudBottomGhost.gameObject.SetActive(false);
		UIManager.PlayerHealthUI.gameObject.SetActive(true);
		panelRight.gameObject.SetActive(true);
		uiAnimator.Play("idle");

		SoundManager.StopMusic();
	}

	public void SetScreenForPreRound()
	{
		preRoundWindow.SetActive(true);
	}

	public void SetScreenForJobSelect()
	{
		preRoundWindow.SetActive(false);
		jobSelectWindow.SetActive(true);
	}

	public void PlayNukeDetVideo()
	{
		uiAnimator.Play("NukeDetVideo");
	}

}