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
			// TODO make stack system more general so each target can define its own close function (probs using unity editor and events?)
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

			// Perform the checks for all key actions which have functions defined here
			foreach (KeyValuePair<KeyAction, KeybindObject> entry in keybindManager.userKeybinds)
			{
				if (keyActionFunctions.ContainsKey(entry.Key))
				{
					if (CheckComboEvent(entry.Value.PrimaryCombo) || CheckComboEvent(entry.Value.SecondaryCombo))
					{
						// Call the function associated with the KeyAction enum
						keyActionFunctions[entry.Key]();
					}
				}
			}
		}
	}

	/// <summary>
	/// Check if either of the key combos for the selected action have been pressed
	/// </summary>
	/// <param name="moveAction">The action to check</param>
	/// <param name="keyEventType">The type of key event to check for</param>
	public static bool CheckMoveAction(MoveAction moveAction)
	{
		return Instance.CheckKeyAction((KeyAction) moveAction, KeyEventType.Hold);
	}

	/// <summary>
	/// Check if either of the key combos for the selected action have been pressed
	/// </summary>
	/// <param name="keyAction">The action to check</param>
	/// <param name="keyEventType">The type of key event to check for</param>
	private bool CheckKeyAction(KeyAction keyAction, KeyEventType keyEventType = KeyEventType.Down)
	{
		KeybindObject action = keybindManager.userKeybinds[keyAction];
		if (CheckComboEvent(action.PrimaryCombo, keyEventType) || CheckComboEvent(action.SecondaryCombo, keyEventType))
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <summary>
	/// Checks if the player has pressed any movement keys
	/// </summary>
	/// <param name="keyEventType">Key event to check for like down, up or hold</param>
	public static bool IsMovementPressed(KeyEventType keyEventType = KeyEventType.Down)
	{
		if (Instance.CheckKeyAction(KeyAction.MoveUp, keyEventType)   || Instance.CheckKeyAction(KeyAction.MoveDown, keyEventType) ||
			Instance.CheckKeyAction(KeyAction.MoveLeft, keyEventType) || Instance.CheckKeyAction(KeyAction.MoveRight, keyEventType))
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <summary>
	/// Check if enter (the return or numpad enter keys) has been pressed
	/// </summary>
	public static bool IsEnterPressed()
	{
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <summary>
	/// Check if escape has been pressed
	/// </summary>
	public static bool IsEscapePressed()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <summary>
	/// Check if the left or right control or command keys have been pressed
	/// </summary>
	public static bool IsControlPressed()
	{
		if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftControl) ||
			Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.LeftCommand))
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <summary>
	/// Checks if the left or right alt key has been pressed (AltGr sends RightAlt)
	/// </summary>
	public static bool IsAltPressed()
	{
		if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	private bool CheckComboEvent(KeyCombo keyCombo, KeyEventType keyEventType = KeyEventType.Down)
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

	private Dictionary<KeyAction, System.Action> keyActionFunctions = new Dictionary<KeyAction, System.Action>
	{
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
		{ KeyAction.IntentGrab, 	() => { UIManager.Intent.SetIntent(Intent.Grab); }},
		{ KeyAction.IntentHarm, 	() => { UIManager.Intent.SetIntent(Intent.Harm); }},

		// TODO add other bindings once chat has been updated
		// Chat
		{ KeyAction.ChatLocal,		() => { ControlChat.Instance.OpenChatWindow(); }},
		// { KeyAction.ChatRadio,		() => { /* ControlChat.Instance.OpenChatWindow(Radio); */ }},
		// { KeyAction.ChatDept,		() => { /* ControlChat.Instance.OpenChatWindow(Department); */ }},

		// Body part selection
		{ KeyAction.TargetHead,		() => { UIManager.ZoneSelector.CycleHead(); }},
		{ KeyAction.TargetChest,	() => { UIManager.ZoneSelector.SelectAction(BodyPartType.Chest); }},
		{ KeyAction.TargetLeftArm,  () => { UIManager.ZoneSelector.SelectAction(BodyPartType.LeftArm); }},
		{ KeyAction.TargetRightArm, () => { UIManager.ZoneSelector.SelectAction(BodyPartType.RightArm); }},
		{ KeyAction.TargetLeftLeg,  () => { UIManager.ZoneSelector.SelectAction(BodyPartType.LeftLeg); }},
		{ KeyAction.TargetRightLeg, () => { UIManager.ZoneSelector.SelectAction(BodyPartType.RightLeg); }},
		{ KeyAction.TargetGroin, 	() => { UIManager.ZoneSelector.SelectAction(BodyPartType.Groin); }}
	};
}