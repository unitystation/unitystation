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
		[SerializeField] private Text adminGhostText;
		[SerializeField] private Text adminDisplayTest;
		[SerializeField] private InputField inputField;

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

			AdminReplyMessage.Send(adminID, $"{PlayerManager.CurrentCharacterSettings.username} replied: " + inputField.text);
			Chat.AddAdminReplyMsg("PM to-<b>Admins</b>: " + inputField.text);
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