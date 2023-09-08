using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using Logs;

/// <summary>
/// Describes all possible actions which can be mapped to a key
/// </summary>
public enum KeyAction
{
	// No action
	None = 0,

	// Special actions TODO
	Examine,
	Pull,
	MenuTab,
	MenuWheel,

	// Movement
	MoveUp,
	MoveLeft,
	MoveDown,
	MoveRight,
	ToggleWalkRun,

	// Actions
	ActionThrow,
	ActionDrop,
	ActionResist,
	ActionStopPull,

	// Hands
	HandSwap,
	HandActivate,
	HandEquip,

	// Intents
	IntentLeft,
	IntentRight,
	IntentHelp,
	IntentDisarm,
	IntentGrab,
	IntentHarm,

	// Chat
	ChatLocal,
	ChatRadio,
	ChatOOC,
	ToggleHelp,
	ToggleAHelp,
	ToggleMHelp,

	// Body Part Targeting
	TargetHead,
	TargetChest,
	TargetLeftArm,
	TargetRightArm,
	TargetLeftLeg,
	TargetRightLeg,
	InteractionModifier,

	//Right click stuff
	ShowAdminOptions,

	Point,
	// UI
	ResetWindowPosition,
	OpenBackpack,
	OpenPDA,
	OpenBelt,

	PocketOne,
	PocketTwo,
	PocketThree,

	EmoteWindowUI,

	//Interactions that only happen when this key is pressed
	RadialScrollBackward,
	RadialScrollForward,

	HideUi,
	PreventRadialQuickSelectOpen
}

/// <summary>
/// A subset of KeyAction which only describes move actions
/// </summary>
public enum MoveAction
{
	MoveUp = KeyAction.MoveUp,
	MoveLeft = KeyAction.MoveLeft,
	MoveDown = KeyAction.MoveDown,
	MoveRight = KeyAction.MoveRight,
	NoMove = KeyAction.None
}

public class KeybindManager : MonoBehaviour {
	public static KeybindManager Instance;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}

		LoadKeybinds();
	}

	/// <summary>
	/// All valid modfier keys
	/// </summary>
	public static readonly List<KeyCode> ModKeys = new List<KeyCode>
	{
		KeyCode.AltGr, // AltGr is a strange key which sends AltGr, LeftControl and RightAlt at the same time!
		KeyCode.LeftControl,
		KeyCode.LeftCommand,
		KeyCode.LeftShift,
		KeyCode.LeftAlt,

		KeyCode.RightControl,
		KeyCode.RightCommand,
		KeyCode.RightShift,
		KeyCode.RightAlt
	};
	/// <summary>
	/// Keys which can't be rebound
	/// </summary>
	public static readonly List<KeyCode> IgnoreKeys = new List<KeyCode>
	{
		// These probably shouldn't be changed for now
		KeyCode.Mouse0,
		KeyCode.Mouse1
	};
	/// <summary>
	/// A way to store a key combination consisting of one main key and upto two modifier keys
	/// </summary>
	public class KeyCombo
	{
		public KeyCode MainKey;
		public KeyCode ModKey1;
		public KeyCode ModKey2;
		public static readonly KeyCombo None = new KeyCombo();
		public KeyCombo(KeyCode mainKey = KeyCode.None, KeyCode modKey1 = KeyCode.None, KeyCode modKey2 = KeyCode.None)
		{
			MainKey = mainKey;
			ModKey1 = modKey1;
			ModKey2 = modKey2;
		}
		// Define all equality and hash code operators

		public static int TotalKeys(KeyCombo keyCombo)
		{
			var result = 0;
			if (keyCombo.MainKey != KeyCode.None)
				result++;
			if(keyCombo.ModKey1 != KeyCode.None)
				result++;
			if(keyCombo.ModKey2 != KeyCode.None)
				result++;
			return result;
		}

		public static bool ShareKey(KeyCombo a, KeyCombo b)
		{
			if (a.MainKey == b.MainKey || a.MainKey == b.ModKey1 || a.MainKey == b.ModKey2)
			{
				return true;
			}
			if (a.ModKey1 == b.MainKey || a.ModKey1 == b.ModKey1 || a.ModKey1 == b.ModKey2)
			{
				return true;
			}
			if (a.ModKey2 == b.MainKey || a.ModKey2 == b.ModKey1 || a.ModKey2 == b.ModKey2)
			{
				return true;
			}
			return false;
		}

		public static bool operator == (KeyCombo a, KeyCombo b)
		{
			if (object.ReferenceEquals(a, b))
			{
				return true;
			}

			if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null))
			{
				return false;
			}
			return (a.MainKey == b.MainKey) && (a.ModKey1 == b.ModKey1) && (a.ModKey2 == b.ModKey2);
		}
		public static bool operator != (KeyCombo a, KeyCombo b)
		{
			return !(a == b);
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as KeyCombo);
		}
		public bool Equals(KeyCombo other)
		{
			return other == this;
		}
		public override int GetHashCode()
		{
			// Probably not the best way to implement a hash code but it should work
			return string.Format("{0}-{1}-{2}", MainKey, ModKey1, ModKey2).GetHashCode();
		}

		// Custom ToString method
		public override string ToString()
		{
			if (ModKey1 == KeyCode.None)
			{
				return ReplaceString(MainKey);
			}
			else if (ModKey2 == KeyCode.None)
			{
				return ReplaceString(ModKey1) + " + " + ReplaceString(MainKey);
			}
			else
			{
				return ReplaceString(ModKey1) + " + " + ReplaceString(ModKey2) + " + " + ReplaceString(MainKey);
			}
		}

		//
		/// <summary>
		/// Make the keycode strings more generic, eg LeftControl => Ctrl, Alpha1 => 1
		/// </summary>
		/// <param name="keyCode">The keycode to use</param>
		private string ReplaceString(KeyCode keyCode)
		{
			string keyCodeString = keyCode.ToString();
			if (ModKeys.Contains(keyCode))
			{
				// Replacements for mod keys
				return keyCodeString.Replace("Left", "").Replace("Right", "").Replace("Control", "Ctrl").Replace("Command", "Cmd");
			}
			else
			{
				// Replacement for all other keys
				return keyCodeString.Replace("Alpha", "").Replace("Numpad", "Numpad ");
			}
		}
		// Custom clone method
		public KeyCombo Clone()
		{
			return new KeyCombo(this.MainKey, this.ModKey1, this.ModKey2);
		}
	}

	/// <summary>
	/// The type of keybind, for separating keybinds into sections
	/// </summary>
	public enum ActionType
	{
		Default = 0,
		Movement,
		Action,
		Hand,
		Chat,
		Intent,
		Targeting,
		RightClick,
		Point,
		UI
	}

	/// <summary>
	/// Class to hold all information about a keybinding, such as the name, if it can be rebound and the type
	/// </summary>
	public class KeybindMetadata
	{
		public string Name;
		public bool Rebindable;
		public ActionType Type;

		public static readonly KeybindMetadata None = new KeybindMetadata();
		public KeybindMetadata(string name = "None", ActionType type = ActionType.Default, bool rebindable = true)
		{
			Name = name;
			Rebindable = rebindable;
			Type = type;
		}
		public KeybindMetadata Clone()
		{
			return new KeybindMetadata(this.Name, this.Type, this.Rebindable);
		}
	}

	/// <summary>
	/// Class to hold a primary and secondary KeyCombo, defaults both to KeyCombo.None
	/// </summary>
	public class DualKeyCombo
	{
		public KeyCombo PrimaryCombo;
		public KeyCombo SecondaryCombo;

		public DualKeyCombo(KeyCombo primaryCombo = null, KeyCombo secondaryCombo = null)
		{
			PrimaryCombo   = primaryCombo ?? KeyCombo.None;
			SecondaryCombo = secondaryCombo ?? KeyCombo.None;
		}
		public DualKeyCombo Clone()
		{
			return new DualKeyCombo(this.PrimaryCombo, this.SecondaryCombo);
		}
	}

	public readonly Dictionary<KeyAction, KeybindMetadata> keyActionMetadata = new Dictionary<KeyAction, KeybindMetadata>
	{
		// Movement
		{ KeyAction.MoveUp, 	new KeybindMetadata("Move Up", ActionType.Movement)},
		{ KeyAction.MoveLeft, 	new KeybindMetadata("Move Left", ActionType.Movement)},
		{ KeyAction.MoveDown, 	new KeybindMetadata("Move Down", ActionType.Movement)},
		{ KeyAction.MoveRight, 	new KeybindMetadata("Move Right", ActionType.Movement)},
		{ KeyAction.ToggleWalkRun, new KeybindMetadata("Toggle Walk/Run", ActionType.Movement)},

		// Actions
		{ KeyAction.ActionThrow,	new KeybindMetadata("Throw", ActionType.Action)},
		{ KeyAction.ActionDrop,		new KeybindMetadata("Drop", ActionType.Action)},
		{ KeyAction.ActionResist,	new KeybindMetadata("Resist", ActionType.Action)},
		{ KeyAction.ActionStopPull,	new KeybindMetadata("Stop Pulling", ActionType.Action)},

		{  KeyAction.Point, 		new KeybindMetadata("Point", ActionType.Point)},
		{  KeyAction.HandSwap, 		new KeybindMetadata("Swap Hands", ActionType.Hand)},
		{  KeyAction.HandActivate,	new KeybindMetadata("Activate Item", ActionType.Hand)},
		{  KeyAction.HandEquip, 	new KeybindMetadata("Equip Item", ActionType.Hand)},

		// Intents
		{ KeyAction.IntentLeft,		new KeybindMetadata("Cycle Intent Left", ActionType.Intent)},
		{ KeyAction.IntentRight,	new KeybindMetadata("Cycle Intent Right", ActionType.Intent)},
		{ KeyAction.IntentHelp,		new KeybindMetadata("Help Intent", ActionType.Intent)},
		{ KeyAction.IntentDisarm,	new KeybindMetadata("Disarm Intent", ActionType.Intent)},
		{ KeyAction.IntentGrab,		new KeybindMetadata("Grab Intent", ActionType.Intent)},
		{ KeyAction.IntentHarm,		new KeybindMetadata("Harm Intent", ActionType.Intent)},

		// Chat
		{ KeyAction.ChatLocal,	new KeybindMetadata("Chat", ActionType.Chat)},
		{ KeyAction.ChatRadio,	new KeybindMetadata("Radio Chat", ActionType.Chat)},
		{ KeyAction.ChatOOC,	new KeybindMetadata("OOC Chat", ActionType.Chat)},
		{ KeyAction.ToggleHelp,	new KeybindMetadata("Toggle Help Window", ActionType.Chat)},
		{ KeyAction.ToggleAHelp,	new KeybindMetadata("Toggle Admin Help", ActionType.Chat)},
		{ KeyAction.ToggleMHelp,	new KeybindMetadata("Toggle Mentor Help", ActionType.Chat)},

		// Body part selection
		{ KeyAction.TargetHead,		new KeybindMetadata("Target Head, Eyes and Mouth", ActionType.Targeting)},
		{ KeyAction.TargetChest,	new KeybindMetadata("Target Chest", ActionType.Targeting)},
		{ KeyAction.TargetLeftArm,	new KeybindMetadata("Target Left Arm", ActionType.Targeting)},
		{ KeyAction.TargetRightArm,	new KeybindMetadata("Target Right Arm", ActionType.Targeting)},
		{ KeyAction.TargetLeftLeg,	new KeybindMetadata("Target Left Leg", ActionType.Targeting)},
		{ KeyAction.TargetRightLeg, new KeybindMetadata("Target Right Leg", ActionType.Targeting)},
		{ KeyAction.InteractionModifier,	new KeybindMetadata("Interaction Modifer", ActionType.Targeting)},

		//Right click stuff
		{ KeyAction.ShowAdminOptions, 	new KeybindMetadata("Show Admin Options", ActionType.RightClick)},

		// UI
		// TODO: change ActionType
		{ KeyAction.ResetWindowPosition,  new KeybindMetadata("Reset window position", ActionType.UI)},
		{ KeyAction.OpenBackpack, 	new KeybindMetadata("Open Backpack", ActionType.UI)},
		{ KeyAction.OpenPDA, 		new KeybindMetadata("Open PDA", ActionType.UI)},
		{ KeyAction.OpenBelt, 		new KeybindMetadata("Open Belt", ActionType.UI)},

		{ KeyAction.PocketOne, 		new KeybindMetadata("Open Pocket 1", ActionType.UI)},
		{ KeyAction.PocketTwo, 		new KeybindMetadata("Open Pocket 2", ActionType.UI)},
		{ KeyAction.PocketThree, 	new KeybindMetadata("Open Pocket 3", ActionType.UI)},

		{ KeyAction.RadialScrollForward, new KeybindMetadata("Radial Scroll Forward", ActionType.UI)},
		{ KeyAction.RadialScrollBackward, new KeybindMetadata("Radial Scroll Backward", ActionType.UI)},
		{ KeyAction.EmoteWindowUI,	new KeybindMetadata("Open Emote Window.", ActionType.UI)},
		{ KeyAction.HideUi, new KeybindMetadata("Hide UI", ActionType.UI) },
		{ KeyAction.PreventRadialQuickSelectOpen, new KeybindMetadata("Prevent Quick Radial Open", ActionType.UI) },
	};

	private readonly KeybindDict defaultKeybinds = new KeybindDict
	{
		// Movement
		{ KeyAction.MoveUp, 		new DualKeyCombo(new KeyCombo(KeyCode.W), new KeyCombo(KeyCode.UpArrow))},
		{ KeyAction.MoveLeft, 		new DualKeyCombo(new KeyCombo(KeyCode.A), new KeyCombo(KeyCode.LeftArrow))},
		{ KeyAction.MoveDown, 		new DualKeyCombo(new KeyCombo(KeyCode.S), new KeyCombo(KeyCode.DownArrow))},
		{ KeyAction.MoveRight, 		new DualKeyCombo(new KeyCombo(KeyCode.D), new KeyCombo(KeyCode.RightArrow))},
		{ KeyAction.ToggleWalkRun,   new DualKeyCombo(new KeyCombo(KeyCode.C), null)},

		// Actions
		{ KeyAction.ActionThrow,	new DualKeyCombo(new KeyCombo(KeyCode.R),	new KeyCombo(KeyCode.End))},
		{ KeyAction.ActionDrop,		new DualKeyCombo(new KeyCombo(KeyCode.Q), 	new KeyCombo(KeyCode.Home))},
		{ KeyAction.ActionResist,	new DualKeyCombo(new KeyCombo(KeyCode.V), 	null)},
		{ KeyAction.ActionStopPull, new DualKeyCombo(new KeyCombo(KeyCode.H), new KeyCombo(KeyCode.Delete))},

		{  KeyAction.Point,			new DualKeyCombo(new KeyCombo(KeyCode.Mouse2, KeyCode.LeftShift), null)},
		{  KeyAction.HandSwap, 		new DualKeyCombo(new KeyCombo(KeyCode.X),	new KeyCombo(KeyCode.Mouse2))},
		{  KeyAction.HandActivate,	new DualKeyCombo(new KeyCombo(KeyCode.Z),	new KeyCombo(KeyCode.PageDown))},
		{  KeyAction.HandEquip, 	new DualKeyCombo(new KeyCombo(KeyCode.E),	null)},

		// Intents
		{ KeyAction.IntentLeft,		new DualKeyCombo(new KeyCombo(KeyCode.F),		new KeyCombo(KeyCode.Insert))},
		{ KeyAction.IntentRight, 	new DualKeyCombo(new KeyCombo(KeyCode.G),		new KeyCombo(KeyCode.Keypad0))},
		{ KeyAction.IntentHelp, 	new DualKeyCombo(new KeyCombo(KeyCode.Alpha1), null)},
		{ KeyAction.IntentDisarm,	new DualKeyCombo(new KeyCombo(KeyCode.Alpha2), null)},
		{ KeyAction.IntentGrab, 	new DualKeyCombo(new KeyCombo(KeyCode.Alpha3), null)},
		{ KeyAction.IntentHarm, 	new DualKeyCombo(new KeyCombo(KeyCode.Alpha4), null)},

		// Chat
		{ KeyAction.ChatLocal, 		new DualKeyCombo(new KeyCombo(KeyCode.T), new KeyCombo(KeyCode.Return))},
		{ KeyAction.ChatRadio,		new DualKeyCombo(new KeyCombo(KeyCode.Y), null)},
		{ KeyAction.ChatOOC,   		new DualKeyCombo(new KeyCombo(KeyCode.O), null)},
		{ KeyAction.ToggleHelp,    new DualKeyCombo(new KeyCombo(KeyCode.F1), null)},
		{ KeyAction.ToggleAHelp,    new DualKeyCombo(new KeyCombo(KeyCode.F2), null)},
		{ KeyAction.ToggleMHelp,    new DualKeyCombo(new KeyCombo(KeyCode.F3), null)},

		// Body part selection
		{ KeyAction.TargetHead, 	new DualKeyCombo(new KeyCombo(KeyCode.Keypad8), null)},
		{ KeyAction.TargetChest,	new DualKeyCombo(new KeyCombo(KeyCode.Keypad5), null)},
		{ KeyAction.TargetLeftArm,  new DualKeyCombo(new KeyCombo(KeyCode.Keypad6), null)},
		{ KeyAction.TargetRightArm, new DualKeyCombo(new KeyCombo(KeyCode.Keypad4), null)},
		{ KeyAction.TargetLeftLeg,  new DualKeyCombo(new KeyCombo(KeyCode.Keypad3), null)},
		{ KeyAction.TargetRightLeg, new DualKeyCombo(new KeyCombo(KeyCode.Keypad1), null)},
		{ KeyAction.InteractionModifier,	new DualKeyCombo(new KeyCombo(KeyCode.LeftAlt), null)},

		//Right click stuff
		{ KeyAction.ShowAdminOptions, new DualKeyCombo(new KeyCombo(KeyCode.LeftControl), null)},

		// UI
		{ KeyAction.ResetWindowPosition,  new DualKeyCombo(new KeyCombo(KeyCode.BackQuote), null)},
		{ KeyAction.OpenBackpack, 	new DualKeyCombo(new KeyCombo(KeyCode.I), null)},
		{ KeyAction.OpenPDA, 		new DualKeyCombo(new KeyCombo(KeyCode.P), null)},
		{ KeyAction.OpenBelt, 		new DualKeyCombo(new KeyCombo(KeyCode.J), null)},

		{ KeyAction.PocketOne, 		new DualKeyCombo(new KeyCombo(KeyCode.Alpha1 ,KeyCode.LeftShift), null)},
		{ KeyAction.PocketTwo, 		new DualKeyCombo(new KeyCombo(KeyCode.Alpha2 ,KeyCode.LeftShift), null)},
		{ KeyAction.PocketThree, 	new DualKeyCombo(new KeyCombo(KeyCode.Alpha3 ,KeyCode.LeftShift), null)},

		{ KeyAction.RadialScrollForward,	new DualKeyCombo(new KeyCombo(KeyCode.E, KeyCode.LeftShift), null)},
		{ KeyAction.RadialScrollBackward,	new DualKeyCombo(new KeyCombo(KeyCode.Q, KeyCode.LeftShift), null)},
		{ KeyAction.EmoteWindowUI,	new DualKeyCombo(new KeyCombo(KeyCode.Backslash), null)},
		{ KeyAction.HideUi, new DualKeyCombo(new KeyCombo(KeyCode.F11), null) },
		{ KeyAction.PreventRadialQuickSelectOpen, new DualKeyCombo(new KeyCombo(KeyCode.LeftShift), null) },

	};
	public KeybindDict userKeybinds = new KeybindDict();

	public class KeybindDict : Dictionary<KeyAction, DualKeyCombo>
	{
		public KeybindDict()
		{
			// Constructor with no parameter
		}
		public KeybindDict(KeybindDict initialDict) : base (initialDict)
		{
			// Constructor with a parameter which can use the base class constructor
		}

		public KeybindDict Clone()
		{
			KeybindDict newCopy = new KeybindDict();
			foreach (KeyValuePair<KeyAction, DualKeyCombo> entry in this)
			{
				newCopy.Add(entry.Key, entry.Value.Clone());
			}
			return newCopy;
		}

		public void Set(KeyAction keyAction, KeyCombo keyCombo, bool isPrimary)
		{
			Loggy.Log("Setting " + (isPrimary ? "primary" : "secondary") + "keybind for " + keyAction + " to " + keyCombo, Category.Keybindings);
			if (isPrimary)
			{
				this[keyAction].PrimaryCombo = keyCombo;
				// Set keybind texts when changing keybind
				UIManager.UpdateKeybindText(keyAction, keyCombo);
			}
			else
			{
				this[keyAction].SecondaryCombo = keyCombo;
			}
		}
		public void Remove(KeyAction keyAction, bool isPrimary)
		{
			Loggy.Log("Removing " + (isPrimary ? "primary" : "secondary") + " keybind from " + keyAction, Category.Keybindings);
			if (isPrimary)
			{
				this[keyAction].PrimaryCombo = KeyCombo.None;
			}
			else
			{
				this[keyAction].SecondaryCombo = KeyCombo.None;
			}
		}
		public KeyValuePair<KeyAction, DualKeyCombo> CheckConflict(KeyCombo keyCombo, ref bool isPrimary)
		{
			foreach (KeyValuePair<KeyAction, DualKeyCombo> entry in this)
			{
				if (keyCombo == entry.Value.PrimaryCombo)
				{
					isPrimary = true;
					Loggy.Log("Conflict found with primary key for " + entry.Key, Category.Keybindings);
					return entry;
				}
				else if (keyCombo == entry.Value.SecondaryCombo)
				{
					isPrimary = false;
					Loggy.Log("Conflict found with secondary key for " + entry.Key, Category.Keybindings);
					return entry;
				}
			}
			// No match found, return none
			return new KeyValuePair<KeyAction, DualKeyCombo>(KeyAction.None, null);
		}
	}
	public KeyCombo CaptureKeyCombo()
	{
		KeyCode newKey = KeyCode.None;
		KeyCode newModKey1 = KeyCode.None;
		KeyCode newModKey2 = KeyCode.None;

		// Iterate through all possible keys
		foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
		{
			if (ModKeys.Contains(key) || IgnoreKeys.Contains(key))
			{
				// If it's a modifier/ignored key we can skip it for now
				continue;
			}
			else if (CommonInput.GetKeyDown(key))
			{
				// Stop capturing if user presses escape
				if (key == KeyCode.Escape)
				{
					return null;
				}

				// Keep the key for later so we can return it
				newKey = key;

				// Check if any modifiers are pressed too
				foreach (KeyCode modKey in ModKeys)
				{
					if (!CommonInput.GetKey(modKey)) continue;
					// A modifier key is pressed, assign it to the first available modkey
					if (newModKey1 == KeyCode.None)
					{
						// ModKey1 hasn't been assigned yet
						newModKey1 = validateModKey(modKey);
						if (newModKey1 == KeyCode.AltGr)
						{
							// Since AltGr is a strange key which sends AltGr, LeftControl and RightAlt at the same time
							// we will only allow it to be a modifier key on its own, so we can stop checking now
							break;
						}
					}
					else if (newModKey2 == KeyCode.None)
					{
						// ModKey2 hasn't been assigned yet
						// Assign it then stop checking since all modkeys assigned
						newModKey2 = validateModKey(modKey);
						break;
					}
				}
			}
		}

		// Return the new key combination
		return new KeyCombo(newKey, newModKey1, newModKey2);
	}
	private KeyCode validateModKey(KeyCode modKey)
	{
		// Will treat left and right mod keys the same so just store as left version
		switch (modKey)
		{
			case KeyCode.RightControl:
				return KeyCode.LeftControl;
			case KeyCode.RightCommand:
				return KeyCode.LeftCommand;
			case KeyCode.RightShift:
				return KeyCode.LeftShift;
			case KeyCode.RightAlt:
				return KeyCode.LeftAlt;
			default:
				return modKey;
		}
	}

	public void SaveKeybinds(KeybindDict newKeybinds)
	{
		Loggy.Log("Saving user keybinds", Category.Keybindings);
		// Make userKeybinds reference the new keybinds (since KeybindDict is reference type)
		userKeybinds = newKeybinds;
		// Turn the user's keybinds into JSON
		string jsonKeybinds = JsonConvert.SerializeObject(userKeybinds);
		// Save the user's keybinds to PlayerPrefs as a JSON string
		PlayerPrefs.SetString("userKeybinds", jsonKeybinds);
		PlayerPrefs.Save();
	}

	public void ResetKeybinds()
	{
		Loggy.Log("Resetting user keybinds", Category.Keybindings);
		// Save a copy of the default keybinds as the user's keybinds
		SaveKeybinds(defaultKeybinds.Clone());
	}
	public void LoadKeybinds()
	{
		Loggy.Log("Loading user keybinds", Category.Keybindings);
		// Get the user's saved keybinds from PlayerPrefs
		string jsonKeybinds = PlayerPrefs.GetString("userKeybinds");
		if (jsonKeybinds != "")
		{
			// Check if user has any saved keybinds and deserialize it from JSON
			// If there are any problems then just reset controls to default
			try
			{
				userKeybinds = JsonConvert.DeserializeObject<KeybindDict>(jsonKeybinds);
				// Set keybind texts when loading keybinds
				foreach (var keyValuePair in userKeybinds)
				{
					UIManager.UpdateKeybindText(keyValuePair.Key, keyValuePair.Value.PrimaryCombo);
				}
			}
			catch (Exception e)
			{
				Loggy.LogError("Couldn't deserialize userKeybind JSON: " + e, Category.Keybindings);
				ResetKeybinds();
				ModalPanelManager.Instance.Inform("Unable to read saved keybinds.\nThey were either corrupt or outdated, so they have been reset.");
			}

			// Properly updating user keybinds when we add or remove one
			var newHotkeys        = defaultKeybinds.Keys.Except(userKeybinds.Keys);
			var deprecatedHotKeys = userKeybinds.Keys.Except(defaultKeybinds.Keys);

			try
			{
				foreach (KeyAction entry in newHotkeys) userKeybinds.Add(entry, defaultKeybinds[entry]);
			}
			catch (Exception e)
			{
				Loggy.LogError("Unable to add new keybind entries" + e, Category.Keybindings);
				ResetKeybinds();
				ModalPanelManager.Instance.Inform("Unable to read saved keybinds.\nThey were either corrupt or outdated, so they have been reset.");
			}
			try
			{
				foreach (KeyAction entry in deprecatedHotKeys) userKeybinds.Remove(entry);
			}
			catch (Exception e)
			{
				Loggy.LogError("Unable to remove old keybind entries" + e, Category.Keybindings);
				ResetKeybinds();
				ModalPanelManager.Instance.Inform("Unable to read saved keybinds.\nThey were either corrupt or outdated, so they have been reset.");
			}

		}
		else
		{
			// Make a new copy of defaultKeybinds and make userKeybinds reference it
			Loggy.Log("No saved keybinds found. Using default.", Category.Keybindings);
			ResetKeybinds();
		}
	}
}