using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static KeybindManager;

namespace UI
{
	public class ControlSettingsMenu : MonoBehaviour
	{
		[SerializeField]
		private GameObject KeybindItemTemplate = null;
		[SerializeField]
		private GameObject KeybindHeadingTemplate = null;
		[SerializeField]

		private GameObject KeyCapturePanel = null;
		private KeybindDict tempKeybinds;
		private int KeybindCount;
		private int ActionTypeCount;
		private List<GameObject> KeybindItemList = new List<GameObject>();
		private Dictionary<ActionType, GameObject> KeybindHeadingDict = new Dictionary<ActionType, GameObject>();
		private KeybindManager KeybindManager => KeybindManager.Instance;
		private ModalPanelManager ModalPanelManager => ModalPanelManager.Instance;

		void Awake()
		{
			KeybindCount = System.Enum.GetNames(typeof(KeyAction)).Length;
			ActionTypeCount = System.Enum.GetNames(typeof(ActionType)).Length;

			// Create a header for each type of action (start i at 1 to skip default)
			for (int i = 1; i < ActionTypeCount; i++)
			{
				GameObject newKeybindHeading = Instantiate(KeybindHeadingTemplate);
				ActionType actionType = (ActionType)i;

				// Add the heading to the dictionary so it can be looked up later
				KeybindHeadingDict.Add(actionType, newKeybindHeading);
				// Set the text to be the name of the actionType
				newKeybindHeading.GetComponentInChildren<Text>().text = actionType + " Controls";
				// Give the object the same parent as the template (the scroll view content)
				newKeybindHeading.transform.SetParent(KeybindHeadingTemplate.transform.parent, false);
				newKeybindHeading.SetActive(true);
			}
		}

		void OnEnable()
		{
			tempKeybinds = KeybindManager.userKeybinds.Clone();
			PopulateKeybindScrollView();
		}

		#region Menu Button Functions

		[System.Obsolete("Changing a key now auto saves it")]
		public void SaveButton()
		{
			ModalPanelManager.Confirm(
				"Are you sure?",
				() =>
				{
					KeybindManager.SaveKeybinds(tempKeybinds);
				},
				"Save And Close"
			);
		}

		// Broadcast message from options
		public void ResetDefaults()
		{
			ModalPanelManager.Confirm(
				"Are you sure?",
				() =>
				{
					KeybindManager.ResetKeybinds();
					tempKeybinds = KeybindManager.userKeybinds.Clone();
					PopulateKeybindScrollView();
				},
				"Reset"
			);
		}

		#endregion

		#region Keybind List Functions

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
					Destroy(item);
				}
				KeybindItemList.Clear();
			}

			// Reverse loop direction so items are in intended order under the headings
			for (int i = KeybindCount; i > 0; i--)
			{
				// Convert i to a KeyAction enum and get the corresponding keybind
				KeyAction action = (KeyAction)i;

				// Check if there is an entry for the action, not all of them will have entries
				if (!tempKeybinds.ContainsKey(action)) continue;

				DualKeyCombo actionKeybind = tempKeybinds[action];
				KeybindMetadata actionMetadata = KeybindManager.keyActionMetadata[action];

				// Only add the action if it can be rebound
				if (!actionMetadata.Rebindable) continue;

				// Create the item and give it the same parent as the template (the scroll view content)
				GameObject newKeybindItem = Instantiate(KeybindItemTemplate, KeybindItemTemplate.transform.parent, false);

				// Add item to the list so we can destroy it again later
				KeybindItemList.Add(newKeybindItem);

				// Set the correct labels and onClick functions
				newKeybindItem.GetComponent<KeybindItemTemplate>().SetupKeybindItem(action, actionKeybind, actionMetadata);

				// Grab the index of the appropriate heading and put the keybind under it
				int headingIndex = KeybindHeadingDict[actionMetadata.Type].transform.GetSiblingIndex();
				newKeybindItem.transform.SetSiblingIndex(headingIndex + 1);

				newKeybindItem.SetActive(true);
			}
		}

		/// <summary>
		/// Changes the keybind for an action. Must be run as a coroutine!
		/// </summary>
		/// <param name="selectedAction">The action to change the keybinding of</param>
		/// <param name="isPrimary">Is this a primary or a secondary keybinding?</param>
		public IEnumerator ChangeKeybind(KeyAction selectedAction, bool isPrimary)
		{
			KeyValuePair<KeyAction, DualKeyCombo> conflictingKVP;
			KeyCombo capturedKeyCombo = KeyCombo.None;
			bool isConflictPrimary = true;

			// Wait for the keycombo to be captured
			KeyCapturePanel.SetActive(true);
			UIManager.IsInputFocus = true;
			while (capturedKeyCombo == KeyCombo.None)
			{
				capturedKeyCombo = KeybindManager.CaptureKeyCombo();
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

			KeybindMetadata conflictingKeybindMetadata;

			if (conflictingAction == KeyAction.None)
			{
				// No conflicts found so set the new keybind and refresh the view
				tempKeybinds.Set(selectedAction, capturedKeyCombo, isPrimary);
				KeybindManager.SaveKeybinds(tempKeybinds);
				// Make sure the player can move around again
				UIManager.IsInputFocus = false;
				PopulateKeybindScrollView();
			}
			// Check if the conflict is with itself
			else if (conflictingAction == selectedAction)
			{
				// Get the metadata for the keybind
				conflictingKeybindMetadata = KeybindManager.keyActionMetadata[conflictingAction];
				// Inform the user
				ModalPanelManager.Inform("\nThis combination is already being used by:\n" + conflictingKeybindMetadata.Name);
				UIManager.IsInputFocus = false;
			}
			// Conflict with any other action
			else
			{
				conflictingKeybindMetadata = KeybindManager.keyActionMetadata[conflictingAction];
				// Check if the user wants to change the keybind
				ModalPanelManager.Confirm(
					"Warning!\n\nThis combination is already being used by:\n" + conflictingKeybindMetadata.Name + "\nAre you sure you want to override it?",
					() =>
					{
					// User confirms they want to change the keybind
					tempKeybinds.Set(selectedAction, capturedKeyCombo, isPrimary);
						tempKeybinds.Remove(conflictingAction, isConflictPrimary);
						KeybindManager.SaveKeybinds(tempKeybinds);
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

		/// <summary>
		/// Removes a keybind for an action. Auto-saves the change immediately
		/// </summary>
		/// <param name="selectedAction">The action to remove a keybinding from</param>
		/// <param name="isPrimary">Remove the primary or secondary keybinding?</param>
		public void RemoveKeybind(KeyAction selectedAction, bool isPrimary)
		{
			tempKeybinds.Remove(selectedAction, isPrimary);
			KeybindManager.SaveKeybinds(tempKeybinds);
		}

		#endregion
	}
}
