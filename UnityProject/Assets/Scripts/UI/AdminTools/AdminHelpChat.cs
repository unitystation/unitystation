using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class AdminHelpChat : MonoBehaviour
	{
		[SerializeField] private InputFieldFocus chatInputField = null;
		[SerializeField] private Transform content = null;
		[SerializeField] private Transform thresholdMarker = null;

		public Transform ThresholdMarker => thresholdMarker;
		public Transform Content => content;

		public void CloseWindow()
		{
			gameObject.SetActive(false);
			chatInputField.text = "";
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
				CloseWindow();
				return;
			}

			AdminReplyMessage.Send($"{PlayerManager.CurrentCharacterSettings.username} replied: " + chatInputField.text);
			Chat.AddAdminPrivMsg("You: " + chatInputField.text);
			chatInputField.text = "";
		}
	}
}