using System;
using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class AdminPrivReply : MonoBehaviour
	{
		[SerializeField] private Text adminGhostText = null;
		[SerializeField] private Text adminDisplayTest = null;
		[SerializeField] private InputField inputField = null;

		private string adminID;

		public void OpenAdminPrivReplay(string adminMsg, string adminId)
		{
			gameObject.SetActive(true);
			adminID = adminId;
			adminGhostText.text = adminMsg;
			adminDisplayTest.text = adminMsg;
			adminID = adminId;
			inputField.ActivateInputField();
			UIManager.PreventChatInput = true;
		}

		public void OnInputEnter()
		{
			if (string.IsNullOrWhiteSpace(inputField.text)) return;

			AdminReplyMessage.Send($"{PlayerManager.CurrentCharacterSettings.username} replied: " + inputField.text);
			Chat.AddAdminPrivMsg("You: " + inputField.text);
			inputField.text = "";

			StartCoroutine(CloseWindow());
		}

		IEnumerator CloseWindow()
		{
			yield return WaitFor.EndOfFrame;
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
			gameObject.SetActive(false);
		}

		private void OnDisable()
		{
			UIManager.PreventChatInput = false;
		}
	}
}