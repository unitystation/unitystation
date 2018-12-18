using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static KeybindManager;

public class KeyboardInput : MonoBehaviour
{
	public static KeyboardInput Instance;
	private KeybindManager keybindManager => KeybindManager.Instance;
	void Awake ()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	void Update()
	{
		CheckKeyboardInput();
	}

	void CheckKeyboardInput()
	{
		if (!UIManager.IsInputFocus)
		{
			// Perform the checks for all rebindable key actions
			foreach (KeyValuePair<KeyAction, KeybindObject> entry in keybindManager.userKeybinds)
			{
				if (isComboPressed(entry.Value.PrimaryCombo) || isComboPressed(entry.Value.SecondaryCombo))
				{
					// Call the function associated with the KeyAction enum
					keyActionFunctions[entry.Key]();
				}
			}
		}
	}

	bool isComboPressed(KeyCombo keyCombo)
	{
		if (keyCombo.ModKey1 != KeyCode.None && !Input.GetKey(keyCombo.ModKey1))
		{
			return false;
		}
		if (keyCombo.ModKey2 != KeyCode.None && !Input.GetKey(keyCombo.ModKey2))
		{
			return false;
		}
		return Input.GetKeyDown(keyCombo.MainKey);
	}

	public Dictionary<KeyAction, System.Action> keyActionFunctions = new Dictionary<KeyAction, System.Action>
	{
		// TODO: replace all these log messages with the apropriate functions (movement input system will need refactoring)
		// Movement
		{ KeyAction.MoveUp,			() => {/* Logger.Log("Moving up!", Category.Keybindings); */}},
		{ KeyAction.MoveLeft, 		() => {/* Logger.Log("Moving left!", Category.Keybindings); */}},
		{ KeyAction.MoveDown,		() => {/* Logger.Log("Moving down!", Category.Keybindings) */;}},
		{ KeyAction.MoveRight,		() => {/* Logger.Log("Moving right!", Category.Keybindings); */}},

		// Actions	  
		{ KeyAction.ActionThrow,	() => {/* Logger.Log("Throwing!", Category.Keybindings); */}},
		{ KeyAction.ActionDrop,		() => {/* Logger.Log("Dropping!", Category.Keybindings); */}},
		{ KeyAction.ActionResist,	() => {/* Logger.Log("Resisting!", Category.Keybindings); */}},

		{  KeyAction.HandSwap, 		() => {/* Logger.Log("Swapping hands", Category.Keybindings); */}},
		{  KeyAction.HandActivate,	() => {/* Logger.Log("Activating hands", Category.Keybindings); */}},
		{  KeyAction.HandEquip, 	() => {/* Logger.Log("Equipping hands", Category.Keybindings); */}},

		// Intents 
		{ KeyAction.IntentLeft,		() => {/* Logger.Log("Intent left", Category.Keybindings); */}},
		{ KeyAction.IntentRight, 	() => {/* Logger.Log("Intent right", Category.Keybindings); */}},
		{ KeyAction.IntentHelp, 	() => {/* Logger.Log("Intent help", Category.Keybindings); */}},
		{ KeyAction.IntentDisarm,	() => {/* Logger.Log("Intent Disarm", Category.Keybindings); */}},
		{ KeyAction.IntentHarm, 	() => {/* Logger.Log("Intent Harm", Category.Keybindings); */}},
		{ KeyAction.IntentGrab, 	() => {/* Logger.Log("Intent Grab", Category.Keybindings); */}},

		// Chat 
		{ KeyAction.ChatLocal,		() => {/* Logger.Log("Chat Local", Category.Keybindings); */}},
		{ KeyAction.ChatRadio,		() => {/* Logger.Log("Chat Radio", Category.Keybindings); */}},
		{ KeyAction.ChatDept,		() => {/* Logger.Log("Chat Dept", Category.Keybindings); */}},

		// Body part selection
		{ KeyAction.TargetHead,		() => {/* Logger.Log("Target Head", Category.Keybindings); */}},
		{ KeyAction.TargetChest,	() => {/* Logger.Log("Target Chest", Category.Keybindings); */}},
		{ KeyAction.TargetLeftArm,  () => {/* Logger.Log("Target LeftArm", Category.Keybindings); */}},
		{ KeyAction.TargetRightArm, () => {/* Logger.Log("Target RightArm", Category.Keybindings); */}},
		{ KeyAction.TargetLeftLeg,  () => {/* Logger.Log("Target LeftLeg", Category.Keybindings); */}},
		{ KeyAction.TargetRightLeg, () => {/* Logger.Log("Target RightLeg", Category.Keybindings); */}},
		{ KeyAction.TargetGroin, 	() => {/* Logger.Log("Target Groin", Category.Keybindings); */}}
	};
}