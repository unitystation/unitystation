using UnityEngine;

public class ControlDisplays : MonoBehaviour
{
	public GameObject backGround;
	public RectTransform hudBottom;
	public GameObject jobSelectWindow;
	public GameObject teamSelectionWindow;
	public RectTransform panelRight;
	public UIManager parentScript;

	public GameObject nukeOpsGameMode;

	[SerializeField]
	private Animator uiAnimator;

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
		hudBottom.gameObject.SetActive(false);
		backGround.SetActive(true);
		panelRight.gameObject.SetActive(false);
		jobSelectWindow.SetActive(false);
		teamSelectionWindow.SetActive(false);
	}

	public void SetScreenForGame()
	{
		UIManager.PlayerHealthUI.gameObject.SetActive(true);
		hudBottom.gameObject.SetActive(true);
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