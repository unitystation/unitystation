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

	public void ShowVotePopUp(string title, string instigator, string currentCount, string timer)
	{
		ToggleButtons(true);
		gameObject.SetActive(true);
		voteTitle.text = title;
		voteInstigator.text = instigator;
		voteCount.text = currentCount;
		voteTimer.text = timer;
	}

	public void UpdateVoteWindow(string currentCount, string timer)
	{
		if (!gameObject.activeInHierarchy) return;
		voteCount.text = currentCount;
		voteTimer.text = timer;
	}

	public void CloseVoteWindow()
	{
		gameObject.SetActive(false);
	}

	public void VoteYes()
	{
		SoundManager.Play("Click01");
		if (PlayerManager.PlayerScript != null)
		{
			PlayerManager.PlayerScript.playerNetworkActions.CmdRegisterVote(true);
		}
		ToggleButtons(false);
	}

	public void VoteNo()
	{
		SoundManager.Play("Click01");
		if (PlayerManager.PlayerScript != null)
		{
			PlayerManager.PlayerScript.playerNetworkActions.CmdRegisterVote(false);
		}
		ToggleButtons(false);
	}

	void ToggleButtons(bool isOn)
	{
		yesBtn.interactable = isOn;
		noBtn.interactable = isOn;
	}
}
