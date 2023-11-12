using Messages.Client.Admin;
using TMPro;
using UnityEngine;

namespace AdminTools
{
	public enum ChatWindowType
	{
		PlayerToAdmin = 0,
		PlayerToMentor = 1,
		PrayerToAdmin = 2,
	}

	public class AdminHelpChat : MonoBehaviour
	{
		[SerializeField] private ChatScroll chatScroll = null;

		[Tooltip("Only needed if window has no chatscroll")]
		[SerializeField] private InputFieldFocus inputFieldFocus = null;

		[SerializeField] private TMP_InputField TMP_InputField = null;

		[SerializeField] ChatWindowType chatWindowType = ChatWindowType.PlayerToAdmin;

		public void CloseWindow()
		{
			gameObject.SetActive(false);
		}

		private void OnEnable()
		{
			if (chatScroll == null) return;
			chatScroll.OnInputFieldSubmit += OnInputReceived;
		}

		private void OnDisable()
		{
			if (chatScroll == null) return;
			chatScroll.OnInputFieldSubmit -= OnInputReceived;
		}

		public void AddChatEntry(string message)
		{
			if (chatScroll == null) return;
			chatScroll.AddNewChatEntry(new ChatEntryData
			{
				Message =  message
			});
		}

		public void OnInputReceived(string message)
		{
			switch(chatWindowType)
			{
				case ChatWindowType.PlayerToAdmin:
					AdminReplyMessage.Send(message);
					break;
				case ChatWindowType.PlayerToMentor:
					MentorReplyMessage.Send(message);
					break;
				case ChatWindowType.PrayerToAdmin:
					if (TMP_InputField != null)
					{
						PrayerReplyMessage.Send(TMP_InputField.text);
					}
					else
					{
						PrayerReplyMessage.Send(inputFieldFocus.text);
					}
					Chat.AddPrayerPrivMsg("You pray to the gods.");
					CloseWindow();
					break;
				default:
					AdminReplyMessage.Send(message);
					break;

			}
		}
	}
}
