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
		{ KeyAction.MoveUp,			() => {Logger.Log("Moving up!");}},
		{ KeyAction.MoveLeft, 		() => {Logger.Log("Moving left!");}},
		{ KeyAction.MoveDown,		() => {Logger.Log("Moving down!");}},
		{ KeyAction.MoveRight,		() => {Logger.Log("Moving right!");}},

		// Actions	  
		{ KeyAction.ActionThrow,	() => {Logger.Log("Throwing!");}},
		{ KeyAction.ActionDrop,		() => {Logger.Log("Dropping!");}},
		{ KeyAction.ActionResist,	() => {Logger.Log("Resisting!");}},

		{  KeyAction.HandSwap, 		() => {Logger.Log("Swapping hands");}},
		{  KeyAction.HandActivate,	() => {Logger.Log("Activating hands");}},
		{  KeyAction.HandEquip, 	() => {Logger.Log("Equipping hands");}},

		// Intents 
		{ KeyAction.IntentLeft,		() => {Logger.Log("Intent left");}},
		{ KeyAction.IntentRight, 	() => {Logger.Log("Intent right");}},
		{ KeyAction.IntentHelp, 	() => {Logger.Log("Intent help");}},
		{ KeyAction.IntentDisarm,	() => {Logger.Log("Intent Disarm");}},
		{ KeyAction.IntentHarm, 	() => {Logger.Log("Intent Harm");}},
		{ KeyAction.IntentGrab, 	() => {Logger.Log("Intent Grab");}},

		// Chat 
		{ KeyAction.ChatLocal,		() => {Logger.Log("Chat Local");}},
		{ KeyAction.ChatRadio,		() => {Logger.Log("Chat Radio");}},
		{ KeyAction.ChatDept,		() => {Logger.Log("Chat Dept");}},

		// Body part selection
		{ KeyAction.TargetHead,		() => {Logger.Log("Target Head");}},
		{ KeyAction.TargetChest,	() => {Logger.Log("Target Chest");}},
		{ KeyAction.TargetLeftArm,  () => {Logger.Log("Target LeftArm");}},
		{ KeyAction.TargetRightArm, () => {Logger.Log("Target RightArm");}},
		{ KeyAction.TargetLeftLeg,  () => {Logger.Log("Target LeftLeg");}},
		{ KeyAction.TargetRightLeg, () => {Logger.Log("Target RightLeg");}},
		{ KeyAction.TargetGroin, 	() => {Logger.Log("Target Groin");}}
	};
}