using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

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
				textField.text = Provider.GetComponent<Paper>()?.PaperString;
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
		}

		private bool IsPenInHand()
		{
			Pen pen = null;
			foreach (var itemSlot in PlayerManager.LocalPlayerScript.DynamicItemStorage.GetHandSlots())
			{
				if (itemSlot.ItemObject != null && itemSlot.ItemObject.TryGetComponent<Pen>(out pen))
				{
					break;
				}
			}

			return pen != null;
		}

		//Request an edit from server:
		public void OnTextEditEnd()
		{
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestPaperEdit(Provider.gameObject,
				textField.text);
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}
	}
}