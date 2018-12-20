﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

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

		// Make the keycode strings more generic, eg left control => ctrl, alpha1 => 1
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
	/// Describes all possible actions which can be mapped to a key
	/// </summary>
	public enum KeyAction 
	{
		// No action
		None = 0,

		// Movement
		MoveUp,
		MoveLeft,
		MoveDown,
		MoveRight,

		// Actions
		ActionThrow,
		ActionDrop,
		ActionResist,

		// Hands
		HandSwap,
		HandActivate,
		HandEquip,

		// Intents
		IntentLeft,
		IntentRight,
		IntentHelp,
		IntentDisarm,
		IntentHarm,
		IntentGrab,
		
		// Chat
		ChatLocal,
		ChatRadio,
		ChatDept,

		// Body Part Targeting
		TargetHead,
		TargetChest,
		TargetLeftArm,
		TargetRightArm,
		TargetLeftLeg,
		TargetRightLeg,
		TargetGroin
	}
	/// <summary>
	/// The type of keybind, for future cyborg/AI only keybinds
	/// </summary>
	public enum KeyType 
	{
		Default = 0,
		Cyborg,
		AI
	}

	public class KeybindObject
	{
		public string Name;
		public KeyType Type;
		public KeyCombo PrimaryCombo;
		public KeyCombo SecondaryCombo;
		public static readonly KeybindObject None = new KeybindObject();
		public KeybindObject(string name = "None", KeyCombo primaryCombo = null, KeyCombo secondaryCombo = null, KeyType type = KeyType.Default)
		{
			Name = name;
			// If primary or secondary combos are null, assign KeyCombo.None as their value
			PrimaryCombo = primaryCombo ?? KeyCombo.None;
			SecondaryCombo = secondaryCombo ?? KeyCombo.None;
			Type = type;
		}
		public KeybindObject Clone()
		{
			return new KeybindObject(this.Name, this.PrimaryCombo.Clone(), this.SecondaryCombo.Clone(), this.Type);
		}
	}

	public readonly KeybindDict defaultKeybinds = new KeybindDict
	{
		// Movement
		{ KeyAction.MoveUp, 	new KeybindObject("Move Up",	new KeyCombo(KeyCode.W), new KeyCombo(KeyCode.UpArrow))},
		{ KeyAction.MoveLeft, 	new KeybindObject("Move Left", 	new KeyCombo(KeyCode.A), new KeyCombo(KeyCode.LeftArrow))},
		{ KeyAction.MoveDown, 	new KeybindObject("Move Down", 	new KeyCombo(KeyCode.S), new KeyCombo(KeyCode.DownArrow))},
		{ KeyAction.MoveRight, 	new KeybindObject("Move Right", new KeyCombo(KeyCode.D), new KeyCombo(KeyCode.RightArrow))},

		// Actions	  
		{ KeyAction.ActionThrow,	new KeybindObject("Throw", 	new KeyCombo(KeyCode.R),	new KeyCombo(KeyCode.End))},
		{ KeyAction.ActionDrop,		new KeybindObject("Drop", 	new KeyCombo(KeyCode.Q), 	new KeyCombo(KeyCode.Home))},
		{ KeyAction.ActionResist,	new KeybindObject("Resist",	new KeyCombo(KeyCode.V) 	)},

		{  KeyAction.HandSwap, 		new KeybindObject("Swap Hands", 	new KeyCombo(KeyCode.X),	new KeyCombo(KeyCode.PageUp))},
		{  KeyAction.HandActivate,	new KeybindObject("Activate Item",  new KeyCombo(KeyCode.Z),	new KeyCombo(KeyCode.PageDown))},
		{  KeyAction.HandEquip, 	new KeybindObject("Equip Item", 	new KeyCombo(KeyCode.E) 	)},

		// Intents 
		{ KeyAction.IntentLeft,		new KeybindObject("Cycle Left", 	new KeyCombo(KeyCode.F),	 new KeyCombo(KeyCode.Insert))},
		{ KeyAction.IntentRight, 	new KeybindObject("Cycle Right", 	new KeyCombo(KeyCode.G),	 new KeyCombo(KeyCode.Keypad0))},
		{ KeyAction.IntentHelp, 	new KeybindObject("Help Intent", 	new KeyCombo(KeyCode.Alpha1) )},
		{ KeyAction.IntentDisarm,	new KeybindObject("Disarm Intent", 	new KeyCombo(KeyCode.Alpha2) )},
		{ KeyAction.IntentHarm, 	new KeybindObject("Harm Intent", 	new KeyCombo(KeyCode.Alpha3) )},
		{ KeyAction.IntentGrab, 	new KeybindObject("Grab Intent", 	new KeyCombo(KeyCode.Alpha4) )},

		// Chat 
		{ KeyAction.ChatLocal, new KeybindObject("Local Chat", 		new KeyCombo(KeyCode.T), new KeyCombo(KeyCode.Return))},
		{ KeyAction.ChatRadio, new KeybindObject("Radio Chat", 	 	new KeyCombo(KeyCode.Y) )},
		{ KeyAction.ChatDept,  new KeybindObject("Department Chat", new KeyCombo(KeyCode.U) )},

		// Body part selection
		{ KeyAction.TargetHead, 	new KeybindObject("Target Head", 	  new KeyCombo(KeyCode.Keypad8) )},
		{ KeyAction.TargetChest,	new KeybindObject("Target Chest", 	  new KeyCombo(KeyCode.Keypad5) )},
		{ KeyAction.TargetLeftArm,  new KeybindObject("Target Left Arm",  new KeyCombo(KeyCode.Keypad4) )},
		{ KeyAction.TargetRightArm, new KeybindObject("Target Right Arm", new KeyCombo(KeyCode.Keypad6) )},
		{ KeyAction.TargetLeftLeg,  new KeybindObject("Target Left Leg",  new KeyCombo(KeyCode.Keypad1) )},
		{ KeyAction.TargetRightLeg, new KeybindObject("Target Right Leg", new KeyCombo(KeyCode.Keypad3) )},
		{ KeyAction.TargetGroin, 	new KeybindObject("Target Groin",	  new KeyCombo(KeyCode.Keypad2) )}
	};
	public KeybindDict userKeybinds = new KeybindDict();

	public class KeybindDict : Dictionary<KeyAction, KeybindObject>
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
			foreach (KeyValuePair<KeyAction, KeybindObject> entry in this)
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
			Logger.Log("Removing " + (isPrimary ? "primary" : "secondary") + "keybind from " + keyAction, Category.Keybindings);
			if (isPrimary)
			{
				this[keyAction].PrimaryCombo = KeyCombo.None;
			}
			else
			{
				this[keyAction].SecondaryCombo = KeyCombo.None;
			}
		}
		public KeyValuePair<KeyAction, KeybindObject> CheckConflict(KeyCombo keyCombo, ref bool isPrimary)
		{
			foreach (KeyValuePair<KeyAction, KeybindObject> entry in this)
			{
				if (keyCombo == entry.Value.PrimaryCombo)
				{
					isPrimary = true;
					Logger.Log("Conflict found with primary key for " + entry.Value.Name, Category.Keybindings);
					return entry;
				}
				else if (keyCombo == entry.Value.SecondaryCombo)
				{
					isPrimary = false;
					Logger.Log("Conflict found with secondary key for " + entry.Value.Name, Category.Keybindings);
					return entry;
				}
			}
			// No match found, return none
			return new KeyValuePair<KeyAction, KeybindObject>(KeyAction.None, KeybindObject.None);
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
			else if (Input.GetKeyDown(key))
			{
				if (key == KeyCode.Escape)
				{
					return null;
				}

				// Keep the key for later so we can return it
				newKey = key;

				// Check if any modifiers are pressed too
				foreach (KeyCode modKey in ModKeys)
				{
					if (Input.GetKey(modKey))
					{
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
		// Make userKeybinds reference the new keybinds (since KeybinbdDict is reference type)
		userKeybinds = newKeybinds;
		// Turn the user's keybinds into JSON
		string jsonKeybinds = JsonConvert.SerializeObject(userKeybinds);
		// Save the user's keybinds to PlayerPrefs as a JSON string
		PlayerPrefs.SetString("userKeybinds", jsonKeybinds);
		// PlayerPrefs.Save();
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
			// Check if user has any saved keybinds and deserialise it from JSON
			userKeybinds = JsonConvert.DeserializeObject<KeybindDict>(jsonKeybinds);
		}
		else
		{
			// Make a new copy of defaultKeybinds and make userKeybinds reference it
			userKeybinds = defaultKeybinds.Clone();
		}
	}
}
