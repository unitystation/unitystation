using System;

[Serializable]
public enum ItemType
{
	//NOTE: To ensure safety of things, like scriptable objects, that are referencing this enum, you must NOT change
	//the ordinals and any new value you add must specify a new ordinal value

	//no item has this type, just used for defining what's allowed to go in a slot / null value
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
	//no item has this type, just used for defining what's allowed to go in a slot
	All = 18,

}