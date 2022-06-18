using Messages.Client.Admin;
using UnityEngine;

namespace AdminTools
{
	public class AdminHelpChat : MonoBehaviour
	{
		[SerializeField] private ChatScroll chatScroll = null;

		public void CloseWindow()
		{
			gameObject.SetActive(false);
		}

		private void OnEnable()
		{
			chatScroll.OnInputFieldSubmit += OnInputReceived;
		}

		private void OnDisable()
		{
			chatScroll.OnInputFieldSubmit -= OnInputReceived;
		}

		public void AddChatEntry(string message)
		{
			chatScroll.AddNewChatEntry(new ChatEntryData
			{
				Message = message
			});
		}

		void OnInputReceived(string message)
		{
			AdminReplyMessage.Send(message);
		}
	}
}
