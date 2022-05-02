using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UI.Core.NetUI;
using Items.PDA;

namespace UI.Items.PDA
{
	public class GUI_PDANotes : NetPage, IPageReadyable
	{
		[SerializeField] private GUI_PDA controller = null;
		[SerializeField] private InputField textField = null;
		[SerializeField] private ContentSizeFitter contentSizeFitter = null;

		private GameObject PDAObject => controller.PDA.gameObject;

		public void OnPageActivated()
		{
			controller.SetBreadcrumb("/bin/notes.sh");
			controller.PlayDenyTone();
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
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestNoteEdit(PDAObject, textField.text);
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
			textField.text = PDAObject.GetComponent<PDANotesNetworkHandler>().NoteString;
		}
	}
}
