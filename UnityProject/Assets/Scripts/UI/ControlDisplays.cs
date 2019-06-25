using UnityEngine;

public class ControlDisplays : MonoBehaviour
{
	public GameObject backGround;
	public GameObject hudBottomHuman;
	public GameObject hudBottomGhost;
	public GameObject jobSelectWindow;
	public GameObject teamSelectionWindow;
	public RectTransform panelRight;
	public UIManager parentScript;

	public GameObject nukeOpsGameMode;

	[SerializeField]
	private Animator uiAnimator;

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.PlayerSpawned, HumanUI);
		EventManager.AddHandler(EVENT.GhostSpawned, GhostUI);
	}

	void HumanUI()
	{
		hudBottomHuman.gameObject.SetActive(true);
		hudBottomGhost.gameObject.SetActive(false);
	}

	void GhostUI()
	{
		hudBottomHuman.gameObject.SetActive(false);
		hudBottomGhost.gameObject.SetActive(true);
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
		backGround.SetActive(true);
		panelRight.gameObject.SetActive(false);
		jobSelectWindow.SetActive(false);
		teamSelectionWindow.SetActive(false);
	}

	public void SetScreenForGame()
	{
		hudBottomHuman.gameObject.SetActive(false);
		hudBottomGhost.gameObject.SetActive(false);
		UIManager.PlayerHealthUI.gameObject.SetActive(true);
		backGround.SetActive(false);
		panelRight.gameObject.SetActive(true);
		uiAnimator.Play("idle");

		SoundManager.StopMusic();
	}

	public void PlayNukeDetVideo()
	{
		uiAnimator.Play("NukeDetVideo");
	}

	public void DetermineGameMode()
	{
		//if(GameManager.Instance.gameMode == GameMode.nukeops){
			nukeOpsGameMode.SetActive(true);
		//}
	}

}