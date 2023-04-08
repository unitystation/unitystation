using System;

/// <summary>
/// All possible item slots. You MUST specify the ordinal otherwise it will break everything.
/// </summary>
[Serializable]
public enum NamedSlot
{
	//NOTE: To ensure safety of things, like scriptable objects, that are referencing this enum, you must NOT change
	//the ordinals and any new value you add must specify a new ordinal value

	//player inventory stuff
	outerwear = 0,
	belt = 1,
	head = 2,
	feet = 3,

	//NOTE: I don't think this is used, and mask is used instead face = 4,
	mask = 5,
	uniform = 6,
	leftHand = 7,
	rightHand = 8,
	eyes = 9,
	back = 10,
	hands = 11,
	ear = 12,
	neck = 13,
	handcuffs = 14,
	id = 15,
	storage01 = 16,
	storage02 = 17,
	suitStorage = 18,
	storage03 = 19,
	storage04 = 20,
	storage05 = 21,
	storage06 = 22,
	storage07 = 23,
	storage08 = 24,
	storage09 = 25,
	storage10 = 26,
	storage11 = 27,
	storage12 = 28,
	storage13 = 29,
	storage14 = 30,
	storage15 = 31,
	storage16 = 32,
	storage17 = 33,
	storage18 = 34,
	storage19 = 35,
	storage20 = 36,

	//special
	ghostStorage01 = 50,
	ghostStorage02 = 51,
	ghostStorage03 = 52,


	//alternative for non-nullable null value
	none = 2048,
}

// NOTE: Ensure that NamedSlotFlagged == 2^(NamedSlot).
/// <summary>
/// All possible item slots as a flagged enum.
/// </summary>
[Serializable, Flags]
public enum NamedSlotFlagged
{
	None = 0,
	Outerwear = 1 << 0,
	Belt = 1 << 1,
	Head = 1 << 2,
	Feet = 1 << 3,

	// Face = 1 << 4, // Not used; see NamedSlot.
	Mask = 1 << 5,
	Uniform = 1 << 6,
	LeftHand = 1 << 7,
	RightHand = 1 << 8,
	Eyes = 1 << 9,
	Back = 1 << 10,
	Hands = 1 << 11,
	Ear = 1 << 12,
	Neck = 1 << 13,
	Handcuffs = 1 << 14,
	ID = 1 << 15,
	Storage01 = 1 << 16,
	storage02 = 1 << 17,
	SuitStorage = 1 << 18,
}