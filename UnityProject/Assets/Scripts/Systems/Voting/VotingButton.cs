using TMPro;
using UnityEngine;

namespace UI
{
	public class VotingButton
	{
		public TMP_Text btnText;
		private VotePopUp popUp;

		public void Initlize(string optionTxt, VotePopUp pop)
		{
			btnText.text = optionTxt;
			popUp = pop;
		}

		public void OnClick()
		{
			popUp.Vote(btnText.text);
		}
	}
}