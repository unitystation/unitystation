using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;

public class VotePopUp : MonoBehaviour
{
	[SerializeField] private Text voteTitle;
	[SerializeField] private Text voteInstigator;
	[SerializeField] private Text voteCount;
	[SerializeField] private Text voteTimer;

	public void ShowVotePopUp(string title, string instigator, string currentCount, string timer)
	{
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
		CloseVoteWindow();
		if (PlayerManager.PlayerScript != null)
		{
			PlayerManager.PlayerScript.playerNetworkActions.CmdRegisterVote(ServerData.UserID, true);
		}
	}

	public void VoteNo()
	{
		SoundManager.Play("Click01");
		CloseVoteWindow();
		if (PlayerManager.PlayerScript != null)
		{
			PlayerManager.PlayerScript.playerNetworkActions.CmdRegisterVote(ServerData.UserID, false);
		}
	}
}
