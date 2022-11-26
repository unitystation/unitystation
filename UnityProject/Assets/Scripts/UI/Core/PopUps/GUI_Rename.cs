using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UI.Core.NetUI;

namespace UI
{
	public class GUI_Rename : NetTab
	{
		[SerializeField]
		private InputField textField = null;
		[SerializeField]
		private NetFilledInputField networkedInputField = null;
		[SerializeField]
		private ContentSizeFitter contentSizeFitter = null;

		private Renameable renameable;

		private const int MAX_NAME_LENGTH = 42;

		private void Awake()
		{
			if (IsMasterTab == false) return;

			OnTabOpened.AddListener(newPeeper =>
			{
				if (renameable == null) return;

				if (renameable.CustomName.Length > 0)
				{
					networkedInputField.MasterSetValue(renameable.CustomName);
				}
			});
		}

		public override void OnEnable()
		{
			base.OnEnable();
			StartCoroutine(WaitForProvider());
			textField.interactable = false;
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			renameable = Provider.GetComponent<Renameable>();
		}

		public void CloseDialog()
		{
			ControlTabs.CloseTab(Type, Provider);
		}

		public void OnEditStart()
		{
			textField.interactable = true;
			textField.ActivateInputField();

			UIManager.IsInputFocus = true;
			CheckForInput();
		}

		// Safety measure:
		private async void CheckForInput()
		{
			await Task.Delay(500);
			if (textField.isFocused == false)
			{
				UIManager.IsInputFocus = false;
			}
		}

		// Request an edit from server:
		public void OnTextEditEnd()
		{
			var customName = textField.text;
			if (customName.Length > MAX_NAME_LENGTH)
			{
				customName = customName.Substring(0, MAX_NAME_LENGTH);
			}

			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestRename(Provider.gameObject, customName);
			UIManager.IsInputFocus = false;
		}

		public void OnTextValueChange()
		{
			// Only way to refresh it to get it to do its job (unity bug):
			contentSizeFitter.enabled = false;
			contentSizeFitter.enabled = true;
			if (textField.placeholder.enabled == false)
			{
				CheckLine();
			}
		}

		private void CheckLine()
		{
			Canvas.ForceUpdateCanvases();
			if (textField.text.Length > MAX_NAME_LENGTH)
			{
				var sub = textField.text.Substring(0, MAX_NAME_LENGTH);
				textField.text = sub;
			}

			textField.text = Regex.Replace(textField.text, @"\r\n?|\n", "");
		}
	}
}
