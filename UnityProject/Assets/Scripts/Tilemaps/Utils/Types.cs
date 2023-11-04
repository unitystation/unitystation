using System;
using System.Linq;
using Logs;
using UnityEngine.Serialization;

public enum TileType
{ //Don't change the order, map saving uses this order
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
	Electrical,
	Pipe,
	Disposals
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
	[Order(5)] Tables = 9,
	[Order(6)] Objects = 2,
	[Order(7)] Floors = 3,
	[Order(8)] Underfloor = 8,
	[Order(9)] Electrical = 10,
	[Order(10)] Pipe = 11,
	[Order(11)] Disposals = 12,
	[Order(12)] Base = 4
}

public static class LayerUtil
{
	public static bool IsUnderFloor(this LayerType layerType)
	{
		return layerType is LayerType.Underfloor or LayerType.Electrical or LayerType.Pipe or LayerType.Disposals;
	}
}

[Flags]
public enum LayerTypeSelection
{
	None = 0,
	Effects = 1 << 0,
	Walls = 1 << 1,
	Windows =1 << 2,
	Objects = 1 << 3,
	Grills = 1 << 4,
	Floors = 1 << 5,
	Underfloor = 1 << 6,
	Base = 1 << 7,
	Tables = 1 << 8,
	Electrical = 1 << 9,
	Pipe = 1 << 10,
	Disposals = 1 << 11,
	AllUnderFloor = Underfloor | Electrical | Pipe | Disposals,
	All = ~None
}

/// <summary>
/// Used for converting between LayerType and LayerTypeSelection
/// </summary>
public static class LTSUtil
{
	public static bool IsLayerIn(LayerTypeSelection SpecifyLayers, LayerType Layer)
	{
		LayerTypeSelection LayerCon = LayerType2LayerTypeSelection(Layer);
		//Bits are set in SpecifyLayers, doing a logical AND with the layer will return either 0 if it doesn't contain it or the layer bit itself.
		return (SpecifyLayers & LayerCon) > 0;
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
			case LayerType.Tables:
				return LayerTypeSelection.Tables;
			case LayerType.Electrical:
				return LayerTypeSelection.Electrical;
			case LayerType.Pipe:
				return LayerTypeSelection.Pipe;
			case LayerType.Disposals:
				return LayerTypeSelection.Disposals;
			default:
				Loggy.LogError($"Failed to have case for: {Layer}");
				break;
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
