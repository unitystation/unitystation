using TMPro;
using UnityEngine;

namespace UI
{
	public class VotingButton : MonoBehaviour
	{
		public TMP_Text btnText;
		private VotePopUp popUp;

		public void Initlize(string optionTxt, VotePopUp pop)
		{
			popUp = pop;
			btnText.text = optionTxt;
			gameObject.SetActive(true);
		}

		public void OnClick()
		{
			if (popUp == null)
			{
				Logger.LogError("ITS FAKIN NOT SET YA DAFT CUNT");
				return;
			}
			popUp.Vote(btnText.text);
		}
	}
}