

using System;
using System.Linq;
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
	//None is the same as empty space
	[Order(0)]	None 	= 7,
	[Order(1)]	Effects = 6,
	[Order(2)]	Walls 	= 0,
	[Order(3)]	Windows = 1,
	[Order(4)]	Grills 	= 5,
	[Order(5)]	Objects = 2,
	[Order(6)]	Floors 	= 3,
	[Order(7)]	Base 	= 4,
}

public class OrderAttribute : Attribute
{
	public readonly int Order;

	public OrderAttribute(int order)
	{
		Order = order;
	}
}