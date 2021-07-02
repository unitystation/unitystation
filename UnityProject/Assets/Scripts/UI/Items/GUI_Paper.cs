using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI;

namespace UI.Items
{
	public class GUI_Paper : NetTab
	{
		[SerializeField] private TMP_InputField textField = default;

		public override void OnEnable()
		{
			base.OnEnable();
			StartCoroutine(WaitForProvider());
			textField.readOnly = true;
		}

		IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			RefreshText();
		}

		public override void RefreshTab()
		{
			RefreshText();
			base.RefreshTab();
		}

		public void RefreshText()
		{
			if (Provider != null)
			{
				textField.text = Provider.GetComponent<Paper>().PaperString;
			}
		}

		public void ClosePaper()
		{
			ControlTabs.CloseTab(Type, Provider);
		}

		public void OnEditStart()
		{
			if (!IsPenInHand())
			{
				textField.readOnly = true;
				return;
			}
			else
			{
				textField.readOnly = false;
				textField.ActivateInputField();
			}

			UIManager.IsInputFocus = true;
			UIManager.PreventChatInput = true;
			CheckForInput();
		}

		private bool IsPenInHand()
		{
			Pen pen = null;
			foreach (var itemSlot in PlayerManager.LocalPlayerScript.DynamicItemStorage.GetHandSlots())
			{
				if (itemSlot.ItemObject != null && itemSlot.ItemObject.TryGetComponent<Pen>(out pen))
				{
				}
			}

			return pen != null;
		}

		//Safety measure:
		private async void CheckForInput()
		{
			await Task.Delay(500);
			if (!textField.isFocused)
			{
				UIManager.IsInputFocus = false;
				UIManager.PreventChatInput = false;
			}
		}

		//Request an edit from server:
		public void OnTextEditEnd()
		{
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestPaperEdit(Provider.gameObject,
				textField.text);
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}
	}
}