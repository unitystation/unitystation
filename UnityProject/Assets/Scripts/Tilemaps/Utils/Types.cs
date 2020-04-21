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
	Effects,
	UnderFloor,
	ElectricalCable
}

//If you change numbers, scene layers will mess up
public enum LayerType
{
	//None is the same as empty space
	[Order(0)] None = 7,
	[Order(1)] Effects = 6,
	[Order(2)] Walls = 0,
	[Order(3)] Windows = 1,
	[Order(4)] Grills = 5,
	[Order(5)] Objects = 2,
	[Order(6)] Floors = 3,
	[Order(7)] Underfloor = 8,
	[Order(8)] Base = 4,
}

[Flags]
public enum LayerTypeSelection
{
	Effects = 1,
	Walls = 2,
	Windows = 4,
	Objects = 8,
	Grills = 16,
	Floors = 32,
	Underfloor = 64,
	Base = 128,
}

/// <summary>
/// Used for converting between LayerType and LayerTypeSelection
/// </summary>
public static class LTSUtil
{
	public static bool IsLayerIn(LayerTypeSelection SpecifyLayers, LayerType Layer)
	{
		LayerTypeSelection LayerCon = LayerType2LayerTypeSelection(Layer);
		return (SpecifyLayers.HasFlag(LayerCon));
	}

	public static LayerTypeSelection LayerType2LayerTypeSelection(LayerType Layer)
	{
		switch (Layer)
		{
			case LayerType.Effects:
				return LayerTypeSelection.Effects;
			case LayerType.Walls:
				return LayerTypeSelection.Walls;
			case LayerType.Windows:
				return LayerTypeSelection.Windows;
			case LayerType.Grills:
				return LayerTypeSelection.Grills;
			case LayerType.Objects:
				return LayerTypeSelection.Objects;
			case LayerType.Floors:
				return LayerTypeSelection.Floors;
			case LayerType.Underfloor:
				return LayerTypeSelection.Underfloor;
			case LayerType.Base:
				return LayerTypeSelection.Base;
		}
		return LayerTypeSelection.Base;
	}
}



public class OrderAttribute : Attribute
{
	public readonly int Order;

	public OrderAttribute(int order)
	{
		Order = order;
	}
}