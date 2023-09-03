using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using UnityEngine.UI;
using static KeybindManager;

namespace UI
{
	public class KeybindItemTemplate : MonoBehaviour
	{
		public Text ActionText;
		public Button PrimaryButton;
		public Button PrimaryRemoveButton;
		public Button SecondaryButton;
		public Button SecondaryRemoveButton;

		[SerializeField]
		private ControlSettingsMenu controlSettingsMenu = null;
		private KeyAction ItemAction;

		public void SetupKeybindItem(KeyAction action, DualKeyCombo keybind, KeybindMetadata metadata)
		{
			// Setup the action text and store it
			ActionText.text = metadata.Name;
			ItemAction = action;

			// Only activate the remove button if a KeyCombo is present
			PrimaryRemoveButton.gameObject.SetActive(keybind.PrimaryCombo != KeyCombo.None);
			PrimaryButton.GetComponentInChildren<Text>().text = keybind.PrimaryCombo.ToString();
			PrimaryButton.onClick.AddListener(primary_onClick);
			PrimaryRemoveButton.onClick.AddListener(primaryRemove_onClick);

			// Setup the secondary buttons too
			SecondaryRemoveButton.gameObject.SetActive(keybind.SecondaryCombo != KeyCombo.None);
			SecondaryButton.GetComponentInChildren<Text>().text = keybind.SecondaryCombo.ToString();
			SecondaryButton.onClick.AddListener(secondary_onClick);
			SecondaryRemoveButton.onClick.AddListener(secondaryRemove_onClick);
		}

		#region Button Clicking Function

		private void primary_onClick()
		{
			Loggy.Log("Changing primary " + ItemAction + " keybind", Category.Keybindings);
			StartCoroutine(controlSettingsMenu.ChangeKeybind(ItemAction, true));
		}

		private void primaryRemove_onClick()
		{
			controlSettingsMenu.RemoveKeybind(ItemAction, true);
			PrimaryButton.GetComponentInChildren<Text>().text = "None";
			PrimaryRemoveButton.gameObject.SetActive(false);
		}

		private void secondary_onClick()
		{
			Loggy.Log("Changing secondary " + ItemAction + " keybind", Category.Keybindings);
			StartCoroutine(controlSettingsMenu.ChangeKeybind(ItemAction, false));
		}

		private void secondaryRemove_onClick()
		{
			controlSettingsMenu.RemoveKeybind(ItemAction, false);
			SecondaryButton.GetComponentInChildren<Text>().text = "None";
			SecondaryRemoveButton.gameObject.SetActive(false);
		}

		#endregion
	}
}
