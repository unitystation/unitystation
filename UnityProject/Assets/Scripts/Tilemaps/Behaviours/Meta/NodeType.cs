using System;

public enum NodeType : byte
{
	None,
	/// <summary>
	/// Node out in space
	/// </summary>
	Space,
	/// <summary>
	/// Node in a room on a tile that is not occupied.
	/// </summary>
	Room,
	/// <summary>
	/// Node occupied by something such that is not passable or atmos passable (e.g closed door, walls)
	/// </summary>
	Occupied,

	/// <summary>
	/// Node occupied by something such that is not fully passable or atmos passable (e.g windoor, directional window...)
	/// </summary>
	SemiOccupied
}

[Flags]
public enum NodeOccupiedType : byte
{
	None = 0,
	Up = 1 << 0,
	Right = 1 << 1,
	Down = 1 << 2,
	Left = 1 << 3,
}

public static class NodeOccupiedUtil
{
	public static NodeOccupiedType DirectionEnumToOccupied(OrientationEnum orientationEnum)
	{
		switch (orientationEnum)
		{
			case OrientationEnum.Down_By180:
				return NodeOccupiedType.Down;
			case OrientationEnum.Left_By90:
				return NodeOccupiedType.Left;
			case OrientationEnum.Up_By0:
				return NodeOccupiedType.Up;
			case OrientationEnum.Right_By270:
				return NodeOccupiedType.Right;
			default:
				return NodeOccupiedType.None;
		}
	}
}