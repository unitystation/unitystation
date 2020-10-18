using System;

/// <summary>
/// NOTE: Deprecated, only used for importing info from
/// DM config. You must always use the Traits system (ItemTrait and subclasses)
/// for these purposes. This will eventually be removed.
/// </summary>
[Serializable]
public enum ItemType
{
	None = 0,
	Glasses = 1,
	Hat = 2,
	Neck = 3,
	Mask = 4,
	Ear = 5,
	Suit = 6,
	Uniform = 7,
	Gloves = 8,
	Shoes = 9,
	Belt = 10,
	Back = 11,
	ID = 12,
	PDA = 13,
	Food = 14,
	Medical = 15,
	Knife = 16,
	Gun = 17,
	All = 18,

}