using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;

public class VotePopUp : MonoBehaviour
{
	[SerializeField] private Text voteTitle = null;
	[SerializeField] private Text voteInstigator = null;
	[SerializeField] private Text voteCount = null;
	[SerializeField] private Text voteTimer = null;
	[SerializeField] private Button yesBtn = null;
	[SerializeField] private Button noBtn = null;
	[SerializeField] private Button vetoBtn = null;

	private int buttonPresses = 0;

	public void ShowVotePopUp(string title, string instigator, string currentCount, string timer)
	{
		buttonPresses = 0;
		gameObject.SetActive(true);
		yesBtn.interactable = true;
		noBtn.interactable = true;
		voteTitle.text = title;
		voteInstigator.text = instigator;
		voteCount.text = currentCount;
		voteTimer.text = timer;

		if (PlayerList.Instance.AdminToken == null) return;

		vetoBtn.gameObject.SetActive(true);
	}

	public void UpdateVoteWindow(string currentCount, string timer)
	{
		if (!gameObject.activeInHierarchy) return;
		voteCount.text = currentCount;
		voteTimer.text = timer;
	}

	public void CloseVoteWindow()
	{
		vetoBtn.gameObject.SetActive(false);
		gameObject.SetActive(false);
	}

	public void VoteYes()
	{
		SoundManager.Play("Click01");
		if (PlayerManager.PlayerScript != null)
		{
			PlayerManager.PlayerScript.playerNetworkActions.CmdRegisterVote(true);
		}

		buttonPresses ++;
		yesBtn.interactable = false;
		noBtn.interactable = true;
		ToggleButtons(false);
	}

	public void VoteNo()
	{
		SoundManager.Play("Click01");
		if (PlayerManager.PlayerScript != null)
		{
			PlayerManager.PlayerScript.playerNetworkActions.CmdRegisterVote(false);
		}
		buttonPresses++;
		yesBtn.interactable = true;
		noBtn.interactable = false;
		ToggleButtons(false);
	}

	public void AdminVeto()
	{
		SoundManager.Play("Click01");
		if (PlayerManager.PlayerScript != null)
		{
			PlayerManager.PlayerScript.playerNetworkActions.CmdVetoRestartVote(ServerData.UserID, PlayerList.Instance.AdminToken);
		}
		buttonPresses++;
		ToggleButtons(false);
	}

	void ToggleButtons(bool isOn)
	{
		if (buttonPresses < 10) return;

		yesBtn.interactable = isOn;
		noBtn.interactable = isOn;
	}
}
