using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using static KeybindManager;

public class ControlSettingsMenu : MonoBehaviour 
{
	public GameObject KeybindItemTemplate;
	public GameObject KeyCapturePanel;
	[HideInInspector]
	public KeybindDict tempKeybinds;
	private int KeybindCount;
	private List<GameObject> KeybindItemList;
	private KeybindManager keybindManager => KeybindManager.Instance;
	private ModalPanelManager modalPanelManager => ModalPanelManager.Instance;
	private GUI_IngameMenu ingameMenu => GUI_IngameMenu.Instance;

	void Awake()
	{
		KeybindCount = System.Enum.GetNames(typeof(KeyAction)).Length;
		KeybindItemList = new List<GameObject>();
	}

	void OnEnable()
	{
		tempKeybinds = keybindManager.userKeybinds.Clone();
		PopulateKeybindScrollView();
	}
	
	// Menu button functions
	// ==================================================
	public void SaveButton()
	{
		modalPanelManager.Confirm(
			"Are you sure?",
			() =>
			{
				keybindManager.SaveKeybinds(tempKeybinds);
			},
			"Save"
		);
	}
	public void ResetToDefaultButton()
	{
		modalPanelManager.Confirm(
			"Are you sure?", 
			() =>
			{
				keybindManager.ResetKeybinds();
				tempKeybinds = keybindManager.userKeybinds.Clone();
				PopulateKeybindScrollView();
			},
			"Reset"
		);
	}

	// Keybind list functions
	// ==================================================

	/// <summary>
	/// Populates the list of keybinds, will destroy old ones if they already exist
	/// </summary>
	public void PopulateKeybindScrollView()
	{
		Logger.Log("Populating keybind scroll view", Category.Keybindings);
		if (KeybindItemList.Count > 0)
		{
			Logger.Log("Removing old keybind objects", Category.Keybindings);
			// Destroy all items in list if it already exists
			foreach (GameObject item in KeybindItemList)
			{
				Destroy(item.gameObject);
			}
			KeybindItemList.Clear();
		}
		
		for (int i = 1; i < KeybindCount; i++)
		{
			// Convert i to a KeyAction enum and get the corresponding keybind
			KeyAction action = (KeyAction)i;
			KeybindObject keybind = tempKeybinds[action];
			// Logger.Log("Adding: " + action, Category.Keybindings);

			GameObject newKeybindItem = Instantiate(KeybindItemTemplate) as GameObject;
			// Add item to the list so we can destroy it again later
			KeybindItemList.Add(newKeybindItem);

			// Set the correct labels and onClick functions
			newKeybindItem.GetComponent<KeybindItemTemplate>().SetupKeybindItem(action, keybind);

			// Give the object the same parent as the template (the scroll view content)
			newKeybindItem.transform.SetParent(KeybindItemTemplate.transform.parent, false);
			newKeybindItem.SetActive(true);
		}
	}

	public IEnumerator ChangeKeybind(KeyAction selectedAction, bool isPrimary)
	{
		KeyValuePair<KeyAction, KeybindObject> conflictingKVP;
		KeyCombo capturedKeyCombo = KeyCombo.None;
		bool isConflictPrimary = true;

		// Wait for the keycombo to be captured
		KeyCapturePanel.SetActive(true);
		UIManager.IsInputFocus = true;
		while (capturedKeyCombo == KeyCombo.None)
		{
			capturedKeyCombo = keybindManager.CaptureKeyCombo();
			yield return null;
		}
		KeyCapturePanel.SetActive(false);

		// If null stop the function (null is returned if Escape was captured)
		if (capturedKeyCombo == null)
		{
			Logger.Log("Captured Escape key, cancelling change", Category.Keybindings);
			UIManager.IsInputFocus = false;
			yield break;
		}

		Logger.Log("Captured key combo: " + capturedKeyCombo.ToString(), Category.Keybindings);

		conflictingKVP = tempKeybinds.CheckConflict(capturedKeyCombo, ref isConflictPrimary);
		KeyAction conflictingAction = conflictingKVP.Key;
		KeybindObject conflictingKeybindObject = conflictingKVP.Value;
		
		if (conflictingAction == KeyAction.None)
		{
			// No conflicts found so set the new keybind and refresh the view
			tempKeybinds.Set(selectedAction, capturedKeyCombo, isPrimary);
			// Make sure the player can move around again
			UIManager.IsInputFocus = false;
			PopulateKeybindScrollView();
		}
		// Check that the conflict isn't with itself, if it is just ignore it
		else if (conflictingAction != selectedAction)
		{
			// Check if the user wants to change the keybind 
			modalPanelManager.Confirm(
				"Warning!\n\nThis combination is already being used by:\n" + conflictingKeybindObject.Name + "\nAre you sure you want to override it?",
				() => 
				{
					// User confirms they want to change the keybind
					tempKeybinds.Set(selectedAction, capturedKeyCombo, isPrimary);
					tempKeybinds.Remove(conflictingAction, isConflictPrimary);
					UIManager.IsInputFocus = false;
					PopulateKeybindScrollView();
				},
				"Yes",
				() =>
				{
					UIManager.IsInputFocus = false;
				}
			);
		}
	}
}
