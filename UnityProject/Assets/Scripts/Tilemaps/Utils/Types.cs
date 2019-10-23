

using System;

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

/// <summary>
/// Flags are only there for LayerTypeBitmask
/// </summary>
//[Flags]
public enum LayerType
{
	Walls,
	Windows,
	Objects,
	Floors,
	Base,
	Grills,
	Effects,
	None
} //Dictionary shits itself when order is changed! Clueless!
//None =  0,
//Walls =  1 << 0,
//Windows =  1 << 1,
//Objects =  1 << 2,
//Floors =  1 << 3,
//Base =  1 << 4,
//Grills =  1 << 5,
//Effects = 1 << 6,
