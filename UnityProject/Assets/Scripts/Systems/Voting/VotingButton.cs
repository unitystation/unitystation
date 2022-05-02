using TMPro;
using UnityEngine;

namespace UI
{
	public class VotingButton : MonoBehaviour
	{
		public TMP_Text btnText;
		private VotePopUp popUp;

		public void Initialize(string optionTxt, VotePopUp pop)
		{
			popUp = pop;
			btnText.text = optionTxt;
			gameObject.SetActive(true);
		}

		public void OnClick()
		{
			popUp.Vote(btnText.text);
		}
	}
}