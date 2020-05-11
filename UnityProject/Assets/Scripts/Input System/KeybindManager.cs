using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

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
	ToggleAHelp,

	// Body Part Targeting
	TargetHead,
	TargetChest,
	TargetLeftArm,
	TargetRightArm,
	TargetLeftLeg,
	TargetRightLeg,
	TargetGroin,

	//Right click stuff
	ShowAdminOptions

}

/// <summary>
/// A subset of KeyAction which only describes move actions
/// </summary>
public enum MoveAction
{
	MoveUp = KeyAction.MoveUp,
	MoveLeft = KeyAction.MoveLeft,
	MoveDown = KeyAction.MoveDown,
	MoveRight = KeyAction.MoveRight
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

		{  KeyAction.HandSwap, 		new KeybindMetadata("Swap Hands", ActionType.Hand)},
		{  KeyAction.HandActivate,	new KeybindMetadata("Activate Item", ActionType.Hand)},
		{  KeyAction.HandEquip, 	new KeybindMetadata("Equip Item", ActionType.Hand)},

		// Intents
		{ KeyAction.IntentLeft,		new KeybindMetadata("Cycle Intent Left", ActionType.Intent)},
		{ KeyAction.IntentRight, 	new KeybindMetadata("Cycle Intent Right", ActionType.Intent)},
		{ KeyAction.IntentHelp, 	new KeybindMetadata("Help Intent", ActionType.Intent)},
		{ KeyAction.IntentDisarm,	new KeybindMetadata("Disarm Intent", ActionType.Intent)},
		{ KeyAction.IntentGrab, 	new KeybindMetadata("Grab Intent", ActionType.Intent)},
		{ KeyAction.IntentHarm, 	new KeybindMetadata("Harm Intent", ActionType.Intent)},

		// Chat
		{ KeyAction.ChatLocal,   new KeybindMetadata("Chat", ActionType.Chat)},
		{ KeyAction.ChatRadio,   new KeybindMetadata("Radio Chat", ActionType.Chat)},
		{ KeyAction.ChatOOC,     new KeybindMetadata("OOC Chat", ActionType.Chat)},
		{ KeyAction.ToggleAHelp, new KeybindMetadata("Toggle AHelp", ActionType.Chat)},

		// Body part selection
		{ KeyAction.TargetHead, 	new KeybindMetadata("Target Head, Eyes and Mouth", ActionType.Targeting)},
		{ KeyAction.TargetChest,	new KeybindMetadata("Target Chest", ActionType.Targeting)},
		{ KeyAction.TargetLeftArm,  new KeybindMetadata("Target Left Arm", ActionType.Targeting)},
		{ KeyAction.TargetRightArm, new KeybindMetadata("Target Right Arm", ActionType.Targeting)},
		{ KeyAction.TargetLeftLeg,  new KeybindMetadata("Target Left Leg", ActionType.Targeting)},
		{ KeyAction.TargetRightLeg, new KeybindMetadata("Target Right Leg", ActionType.Targeting)},
		{ KeyAction.TargetGroin, 	new KeybindMetadata("Target Groin", ActionType.Targeting)},

		//Right click stuff
		{ KeyAction.ShowAdminOptions, 	new KeybindMetadata("Show Admin Options", ActionType.RightClick)}

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
		{ KeyAction.ChatOOC,   		new DualKeyCombo(new KeyCombo(KeyCode.U), null)},
		{ KeyAction.ToggleAHelp,    new DualKeyCombo(new KeyCombo(KeyCode.F1), null)},

		// Body part selection
		{ KeyAction.TargetHead, 	new DualKeyCombo(new KeyCombo(KeyCode.Keypad8), null)},
		{ KeyAction.TargetChest,	new DualKeyCombo(new KeyCombo(KeyCode.Keypad5), null)},
		{ KeyAction.TargetLeftArm,  new DualKeyCombo(new KeyCombo(KeyCode.Keypad6), null)},
		{ KeyAction.TargetRightArm, new DualKeyCombo(new KeyCombo(KeyCode.Keypad4), null)},
		{ KeyAction.TargetLeftLeg,  new DualKeyCombo(new KeyCombo(KeyCode.Keypad3), null)},
		{ KeyAction.TargetRightLeg, new DualKeyCombo(new KeyCombo(KeyCode.Keypad1), null)},
		{ KeyAction.TargetGroin, 	new DualKeyCombo(new KeyCombo(KeyCode.Keypad2), null)},

		//Right click stuff
		{ KeyAction.ShowAdminOptions, new DualKeyCombo(new KeyCombo(KeyCode.LeftControl), null)}
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
			Logger.Log("Setting " + (isPrimary ? "primary" : "secondary") + "keybind for " + keyAction + " to " + keyCombo, Category.Keybindings);
			if (isPrimary)
			{
				this[keyAction].PrimaryCombo = keyCombo;
			}
			else
			{
				this[keyAction].SecondaryCombo = keyCombo;
			}
		}
		public void Remove(KeyAction keyAction, bool isPrimary)
		{
			Logger.Log("Removing " + (isPrimary ? "primary" : "secondary") + " keybind from " + keyAction, Category.Keybindings);
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
					Logger.Log("Conflict found with primary key for " + entry.Key, Category.Keybindings);
					return entry;
				}
				else if (keyCombo == entry.Value.SecondaryCombo)
				{
					isPrimary = false;
					Logger.Log("Conflict found with secondary key for " + entry.Key, Category.Keybindings);
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
		Logger.Log("Saving user keybinds", Category.Keybindings);
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
		Logger.Log("Resetting user keybinds", Category.Keybindings);
		// Save a copy of the default keybinds as the user's keybinds
		SaveKeybinds(defaultKeybinds.Clone());
	}
	public void LoadKeybinds()
	{
		Logger.Log("Loading user keybinds", Category.Keybindings);
		// Get the user's saved keybinds from PlayerPrefs
		string jsonKeybinds = PlayerPrefs.GetString("userKeybinds");
		if (jsonKeybinds != "")
		{
			// Check if user has any saved keybinds and deserialize it from JSON
			// If there are any problems then just reset controls to default
			try
			{
				userKeybinds = JsonConvert.DeserializeObject<KeybindDict>(jsonKeybinds);
			}
			catch (Exception e)
			{
				Logger.LogWarning("Couldn't deserialize userKeybind JSON: " + e, Category.Keybindings);
				ResetKeybinds();
				ModalPanelManager.Instance.Inform("Unable to read saved keybinds.\nThey were either corrupt or outdated, so they have been reset.");
			}

			// Properly updating user keybinds when we add or remove one
			var newHotkeys        = defaultKeybinds.Keys.Except(userKeybinds.Keys);
			var deprecatedHotKeys = userKeybinds.Keys.Except(defaultKeybinds.Keys);

			foreach (KeyAction entry in newHotkeys) userKeybinds.Add(entry, defaultKeybinds[entry]);
			foreach (KeyAction entry in deprecatedHotKeys) userKeybinds.Remove(entry);
		}
		else
		{
			// Make a new copy of defaultKeybinds and make userKeybinds reference it
			Logger.Log("No saved keybinds found. Using default.", Category.Keybindings);
			ResetKeybinds();
		}
	}
}
