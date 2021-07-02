using System;
using TMPro;
using UnityEngine;

namespace UI.Core
{
	public class GeneralInputField : MonoBehaviour
	{
		public Action<string> OnProceed;

		[SerializeField]
		private TMP_Text titleText = null;

		[SerializeField]
		private TMP_InputField input = null;

		private GameObject lastOpener;
		private bool focusCheck;

		#region focus Check

		void Update()
		{
			if (input.isFocused && focusCheck == false)
			{
				InputFocus();
			}
			else if (input.isFocused == false && focusCheck)
			{
				InputUnfocus();
			}
		}

		private void InputFocus()
		{
			focusCheck = true;
			//disable keyboard commands while input is focused
			UIManager.IsInputFocus = true;
		}
		private void InputUnfocus()
		{
			focusCheck = false;
			//disable keyboard commands while input is focused
			UIManager.IsInputFocus = false;
		}

		#endregion

		public void OnOpen(GameObject openerObject, Action<string> onProceed, string title, string inputText = "")
		{
			if(openerObject == null || onProceed == null) return;
			titleText.text = title;

			//Only delete contents if its a new thing which needs this input field
			if (openerObject != lastOpener)
			{
				input.text = String.Empty;
			}

			if (inputText != "")
			{
				input.text = inputText;
			}

			lastOpener = openerObject;

			OnProceed = onProceed;
			gameObject.SetActive(true);
		}

		public void OnProceedPressed()
		{
			OnProceed?.Invoke(input.text);
			OnProceed = null;
			input.text = String.Empty;
			gameObject.SetActive(false);
		}
		public void OnCancel()
		{
			gameObject.SetActive(false);
			OnProceed = null;
		}
	}
}
