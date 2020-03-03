using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class KickBanEntryPage : MonoBehaviour
	{
		[SerializeField] private GameObject kickPage = null;
		[SerializeField] private GameObject banPage = null;

		[SerializeField] private Text kickTitle = null;
		[SerializeField] private InputField kickReasonField = null;

		[SerializeField] private Text banTitle = null;
		[SerializeField] private InputField banReasonField = null;
		[SerializeField] private InputField minutesField = null;

		private AdminPlayerEntryData playerToKickCache;

		public void SetPage(bool isBan, AdminPlayerEntryData playerToKick)
		{
			playerToKickCache = playerToKick;
			UIManager.IsInputFocus = true;
			UIManager.PreventChatInput = true;
			if (!isBan)
			{
				kickPage.SetActive(true);
				kickTitle.text = $"Kick Player: {playerToKick.name}";
				kickReasonField.text = "";
				kickReasonField.ActivateInputField();
			}
			else
			{
				banPage.SetActive(true);
				banTitle.text = $"Ban Player: {playerToKick.name}";
				banReasonField.text = "";
				banReasonField.ActivateInputField();
				minutesField.text = "";
			}

			gameObject.SetActive(true);
		}

		public void OnDoKick()
		{
			if (string.IsNullOrEmpty(kickReasonField.text))
			{
				Logger.LogError("Kick reason field needs to be completed!", Category.Admin);
				return;
			}

			RequestKickMessage.Send(ServerData.UserID, PlayerList.Instance.AdminToken, playerToKickCache.uid,
				kickReasonField.text);

			ClosePage();
		}

		public void OnDoBan()
		{
			if (string.IsNullOrEmpty(banReasonField.text))
			{
				Logger.LogError("Ban reason field needs to be completed!", Category.Admin);
				return;
			}

			if (string.IsNullOrEmpty(minutesField.text))
			{
				Logger.LogError("Duration field needs to be completed!", Category.Admin);
				return;
			}

			int minutes;
			int.TryParse(minutesField.text, out minutes);
			RequestKickMessage.Send(ServerData.UserID, PlayerList.Instance.AdminToken, playerToKickCache.uid,
				banReasonField.text, true, minutes);
			ClosePage();
		}

		public void ClosePage()
		{
			gameObject.SetActive(false);
			kickPage.SetActive(false);
			banPage.SetActive(false);
			UIManager.IsInputFocus = false;
			var manager = FindObjectOfType<PlayerManagePage>();
			manager.RefreshPage();
			UIManager.PreventChatInput = false;
		}
	}
}