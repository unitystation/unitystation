

using System;
using UnityEngine.Serialization;

public enum TileType
{
	None,
	Wall,
	Window,
	Floor,
	Table,
	Object,
	Grill,
	Base,
	WindowDamaged,
	Effects
}

//If you change numbers, scene layers will mess up
public enum LayerType
{
	Walls 	= 0,
	Windows = 1,
	Objects = 2,
	Floors 	= 3,
	Base 	= 4,
	Grills 	= 5,
	Effects = 6,
	None 	= 7
}