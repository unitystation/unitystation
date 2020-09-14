
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
	Outerwear = 1,
	Belt = 2,
	Head = 4,
	Feet = 8,
	Mask = 32,
	Uniform = 64,
	LeftHand = 128,
	RightHand = 256,
	Eyes = 512,
	Back = 1024,
	Hands = 2048,
	Ear = 4096,
	Neck = 8192,
	Handcuffs = 16384,
	ID = 32768,
	Storage01 = 65536,
	storage02 = 131072,
	SuitStorage = 262144
}
