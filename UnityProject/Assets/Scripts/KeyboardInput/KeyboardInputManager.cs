using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static KeybindManager;

public class KeyboardInputManager : MonoBehaviour
{
	public static KeyboardInputManager Instance;
	private KeybindManager keybindManager => KeybindManager.Instance;

	public enum KeyEventType
	{
		Down,
		Up,
		Hold
	}
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
		if (!UIManager.IsInputFocus && GameData.IsInGame && CustomNetworkManager.Instance.IsClientConnected())
		{
			// Perform escape key action
			// TODO make stack system more general so each target can define its own close function (probs using unity editor)
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				if(EscapeKeyTarget.TargetStack.Count > 0)
				{
					GUI_IngameMenu.Instance.CloseMenuPanel(EscapeKeyTarget.TargetStack.Peek());
				}
				else
				{
					GUI_IngameMenu.Instance.OpenMenuPanel(GUI_IngameMenu.Instance.mainIngameMenu);
				}
			}

			// Perform the checks for all rebindable key actions
			foreach (KeyValuePair<KeyAction, KeybindObject> entry in keybindManager.userKeybinds)
			{
				if (CheckComboEvent(entry.Value.PrimaryCombo) || CheckComboEvent(entry.Value.SecondaryCombo))
				{
					// Call the function associated with the KeyAction enum
					keyActionFunctions[entry.Key]();
				}
			}
		}
	}

bool CheckComboEvent(KeyCombo keyCombo, KeyEventType keyEventType = KeyEventType.Down)
	{
		if (keyCombo.ModKey1 != KeyCode.None && !Input.GetKey(keyCombo.ModKey1))
		{
			return false;
		}
		if (keyCombo.ModKey2 != KeyCode.None && !Input.GetKey(keyCombo.ModKey2))
		{
			return false;
		}

		switch (keyEventType)
		{
			case KeyEventType.Down:
				return Input.GetKeyDown(keyCombo.MainKey);
			case KeyEventType.Up:
				return Input.GetKeyUp(keyCombo.MainKey);
			case KeyEventType.Hold:
				return Input.GetKey(keyCombo.MainKey);
			default:
				return Input.GetKeyDown(keyCombo.MainKey);
		}
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
		{ KeyAction.ActionThrow,	() => { UIManager.Action.Throw(); }},
		{ KeyAction.ActionDrop,		() => {	UIManager.Action.Drop(); }},
		{ KeyAction.ActionResist,	() => { UIManager.Action.Resist(); }},

		{  KeyAction.HandSwap, 		() => { UIManager.Hands.Swap(); }},
		{  KeyAction.HandActivate,	() => { UIManager.Hands.Activate(); }},
		{  KeyAction.HandEquip, 	() => { UIManager.Hands.Equip(); }},

		// Intents 
		{ KeyAction.IntentLeft,		() => { UIManager.Intent.CycleIntent(true); }},
		{ KeyAction.IntentRight, 	() => { UIManager.Intent.CycleIntent(false); }},
		{ KeyAction.IntentHelp, 	() => { UIManager.Intent.SetIntent(Intent.Help); }},
		{ KeyAction.IntentDisarm,	() => { UIManager.Intent.SetIntent(Intent.Disarm); }},
		{ KeyAction.IntentHarm, 	() => { UIManager.Intent.SetIntent(Intent.Harm); }},
		{ KeyAction.IntentGrab, 	() => { UIManager.Intent.SetIntent(Intent.Grab); }},

		// Chat 
		{ KeyAction.ChatLocal,		() => { ControlChat.Instance.OpenChatWindow(); }},
		{ KeyAction.ChatRadio,		() => { /* ControlChat.Instance.OpenChatWindow(Radio); */ }},
		{ KeyAction.ChatDept,		() => { /* ControlChat.Instance.OpenChatWindow(Department); */ }},

		// Body part selection
		{ KeyAction.TargetHead,		() => { UIManager.ZoneSelector.SelectAction(BodyPartType.Head); }},
		{ KeyAction.TargetChest,	() => { UIManager.ZoneSelector.SelectAction(BodyPartType.Chest); }},
		{ KeyAction.TargetLeftArm,  () => { UIManager.ZoneSelector.SelectAction(BodyPartType.LeftArm); }},
		{ KeyAction.TargetRightArm, () => { UIManager.ZoneSelector.SelectAction(BodyPartType.RightArm); }},
		{ KeyAction.TargetLeftLeg,  () => { UIManager.ZoneSelector.SelectAction(BodyPartType.LeftLeg); }},
		{ KeyAction.TargetRightLeg, () => { UIManager.ZoneSelector.SelectAction(BodyPartType.RightLeg); }},
		{ KeyAction.TargetGroin, 	() => { /* UIManager.ZoneSelector.SelectAction(BodyPartType.Groin); */ }}
	};
}