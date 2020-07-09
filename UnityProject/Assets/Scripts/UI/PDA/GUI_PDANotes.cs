using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Items.PDA;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace UI.PDA
{
	public class GUI_PDANotes : NetPage
	{
		[SerializeField] private GUI_PDA controller;
		private GameObject cachedProvider;
		[SerializeField] private InputField textField;
		[SerializeField] private ContentSizeFitter contentSizeFitter;

		/// <summary>
		///Grabs the reference of the provider for later use
		/// </summary>
		private void Start()
		{
			cachedProvider = controller.Provider.gameObject;
		}

		/// <summary>
		/// Forgot why this is here
		/// </summary>
		public void OnEditStart()
		{
			textField.interactable = true;
			textField.ActivateInputField();
			UIManager.IsInputFocus = true;
			UIManager.PreventChatInput = true;
			CheckForInput();
		}

		/// <summary>
		/// sends the edited text to the PDANotesNetworkHandler so it can be updated with server
		/// </summary>
		public void OnTextEditEnd()
		{
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestNoteEdit(cachedProvider, textField.text);
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}

		/// <summary>
		/// Checks for input, is never used
		/// </summary>
		private async void CheckForInput()
		{
			await Task.Delay(500);
			if (!textField.isFocused)
			{
				UIManager.IsInputFocus = false;
				UIManager.PreventChatInput = false;
			}
		}

		/// <summary>
		/// Refreshes the clientside input... i think
		/// </summary>
		public void OnTextValueChange()
		{
			contentSizeFitter.enabled = false;
			contentSizeFitter.enabled = true;
			if (!textField.placeholder.enabled)
			{
				CheckLineLimit();
			}
		}
		/// <summary>
		/// Makes sure the players dont write huge notes that the server wont send due to packet size
		/// </summary>
		private void CheckLineLimit()
		{
			Canvas.ForceUpdateCanvases();
			if (textField.textComponent.cachedTextGenerator.lineCount > 20)
			{
				var sub = textField.text.Substring(0, textField.text.Length - 1);
				textField.text = sub;
			}
		}
		/// <summary>
		/// Refreshes the note text
		/// </summary>
		public void RefreshText()
		{
			if (gameObject.activeInHierarchy != true) return;
			textField.text = cachedProvider.GetComponent<PDANotesNetworkHandler>().NoteString;
		}

	}
}