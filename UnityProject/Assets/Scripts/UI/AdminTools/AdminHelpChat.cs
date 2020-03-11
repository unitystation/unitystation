using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class AdminHelpChat : MonoBehaviour
	{
		[SerializeField] private InputFieldFocus chatInputField = null;
		[SerializeField] private Transform content = null;
		[SerializeField] private Transform thresholdMarker = null;
		private List<ChatEntry> chatEntries = new List<ChatEntry>();

		public Transform ThresholdMarker => thresholdMarker;
		public Transform Content => content;

		public void CloseWindow()
		{
			gameObject.SetActive(false);
			chatInputField.text = "";
		}

		public void AddChatEntry(ChatEntry entry)
		{
			chatEntries.Add(entry);
			if (chatEntries.Count == 70)
			{
				var oldEntry = chatEntries[0];
				oldEntry.ReturnToPool();
				chatEntries.Remove(oldEntry);
			}
		}

		void Update()
		{
			if (chatInputField.IsFocused && KeyboardInputManager.IsEnterPressed())
			{
				OnInputEnter();
			}
		}

		public void OnInputEnter()
		{
			if (string.IsNullOrWhiteSpace(chatInputField.text))
			{
				return;
			}

			var msg = Regex.Replace(chatInputField.text, @"\t|\n|\r", "");
			AdminReplyMessage.Send($"{PlayerManager.CurrentCharacterSettings.username} replied: " + msg);
			Chat.AddAdminPrivMsg("You: " + msg);
			chatInputField.text = "";
		}
	}
}