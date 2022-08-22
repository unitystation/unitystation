using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class VotePopUp : MonoBehaviour
	{
		[SerializeField] private Text voteTitle = null;
		[SerializeField] private Text voteInstigator = null;
		[SerializeField] private Text voteCount = null;
		[SerializeField] private Text voteTimer = null;
		[SerializeField] private GameObject buttonsList = null;
		[SerializeField] private Button vetoBtn = null;
		[SerializeField] private GameObject buttonTemp = null;

		private int buttonPresses = 0;

		public void ShowVotePopUp(string title, string instigator, string currentCount, string timer, List<string> options)
		{
			buttonPresses = 0;
			gameObject.SetActive(true);
			GenerateButtons(options);
			DisableButtons(false);
			voteTitle.text = title;
			voteInstigator.text = instigator;
			voteCount.text = currentCount;
			voteTimer.text = timer;

			if (PlayerList.Instance.AdminToken == null) return;

			vetoBtn.gameObject.SetActive(true);
		}

		private void DisableButtons(bool state = true)
		{
			foreach (var btn in buttonsList.GetComponentsInChildren<Button>())
			{
				btn.interactable = !state;
			}
		}

		private void GenerateButtons(List<string> options)
		{
			//clear out all buttons
			foreach (var btn in buttonsList.GetComponentsInChildren<Button>())
			{
				Destroy(btn.gameObject);
			}

			foreach (var newBtn in options)
			{
				var b = Instantiate(buttonTemp, buttonsList.transform);
				b.GetComponent<VotingButton>().Initialize(newBtn, this);
			}
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

		public void Vote(string vote)
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			if (PlayerManager.LocalPlayerScript != null)
			{
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRegisterVote(vote);
			}

			buttonPresses++;
			DisableButtons();
		}

		public void AdminVeto()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			if (PlayerManager.LocalPlayerScript != null)
			{
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdVetoRestartVote();
			}
			buttonPresses++;
		}

		public void VoteYes()
		{
			foreach (var btn in buttonsList.transform.GetComponentsInChildren<VotingButton>())
			{
				if(btn.btnText.text != "Yes") continue;
				btn.OnClick();
			}
		}
	}
}
