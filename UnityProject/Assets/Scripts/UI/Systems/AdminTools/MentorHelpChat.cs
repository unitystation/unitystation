using UnityEngine;

namespace AdminTools
{
	public class MentorHelpChat : MonoBehaviour
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
			MentorReplyMessage.Send($"{PlayerManager.CurrentCharacterSettings.Username} replied: " + message);
		}
	}
}