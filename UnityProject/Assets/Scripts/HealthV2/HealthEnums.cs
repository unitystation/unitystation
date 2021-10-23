using System;

public enum BodyPartType
{
	//these weird ints are for the UI
	None = -1,
	Head = 0,

	Eyes = 7,
	Mouth = 8,
	Chest = 1,
	LeftArm = 3,
	LeftHand = 9,
	RightArm = 2,
	RightHand = 10,

	//    LEFT_HAND,
	//    RIGHT_HAND,
	Groin = 6,
	LeftLeg = 5,
	LeftFoot = 11,

	RightLeg = 4,
	RightFoot = 12,

	//    LEFT_FOOT,
	//    RIGHT_FOOT

	//Used for extra body parts, note please don't set up a Damage selection system for this, just inherit from what Chest, LeftArm n stuff
	Custom = 99
}

public enum ConsciousState
{
	CONSCIOUS = 0, 			// alive and well
	BARELY_CONSCIOUS = 3, 	// in crit, can crawl
	UNCONSCIOUS = 1, 		// unconscious, can't crawl
	DEAD = 2 				// really dead
}

public enum DamageSeverity
{
	None = 0,
	Light = 25,
	LightModerate = 50,
	Moderate = 75,
	Bad = 100,
	Critical = 125,
	Max = 150
}

public enum DamageType
{
	Brute = 0,
	Burn = 1,
	Tox  = 2,
	Oxy = 3,
	Clone = 4,
	Stamina = 5,
	Radiation = 6
}

public enum TraumaDamageLevel
{
	NONE,
	SMALL,
	SERIOUS,
	CRITICAL
}

[Flags]
public enum TraumaticDamageTypes
{
	NONE = 0,
	SLASH = 1 << 0,
	PIERCE = 1 << 1,
	BURN = 1 << 2,
	BLUNT = 1 << 3
}
