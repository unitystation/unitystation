
using System.Collections.Generic;
using UnityEngine;

public static class ConveyorDirections
{
	public static ConveyorBelt.ConveyorDirection GetDirection(int input, int output)
	{
		int val = input + output;
		switch (val)
		{
			case -2:
			case -1:
				return ConveyorBelt.ConveyorDirection.Right;
			case 0:
				return ConveyorBelt.ConveyorDirection.Down;
			case 1:
				return ConveyorBelt.ConveyorDirection.LeftUp;
			case 2:
				if (output == 3) return ConveyorBelt.ConveyorDirection.Down;
				return ConveyorBelt.ConveyorDirection.Right;
				case 3:
					if(output == 3) return ConveyorBelt.ConveyorDirection.LeftDown;
					return ConveyorBelt.ConveyorDirection.RightUp;
				case 4:
					if (input == 1) return ConveyorBelt.ConveyorDirection.Down;
					return ConveyorBelt.ConveyorDirection.RightDown;
				case 5:
					return ConveyorBelt.ConveyorDirection.RightDown;
		}

		return ConveyorBelt.ConveyorDirection.Right;
	}

	public static Dictionary<ConveyorBelt.ConveyorDirection, Vector3> directionsForward = new Dictionary<ConveyorBelt.ConveyorDirection, Vector3>()
	{
		{ConveyorBelt.ConveyorDirection.Up, Vector3.up},
		{ConveyorBelt.ConveyorDirection.Right, Vector3.right},
		{ConveyorBelt.ConveyorDirection.Down, Vector3.down},
		{ConveyorBelt.ConveyorDirection.Left, Vector3.left},
		{ConveyorBelt.ConveyorDirection.LeftDown, Vector3.down},
		{ConveyorBelt.ConveyorDirection.LeftUp, Vector3.up},
		{ConveyorBelt.ConveyorDirection.RightDown, Vector3.right},
		{ConveyorBelt.ConveyorDirection.RightUp, Vector3.right}
	};

	public static Dictionary<ConveyorBelt.ConveyorDirection, Vector3> directionsBackward = new Dictionary<ConveyorBelt.ConveyorDirection, Vector3>()
	{
		{ConveyorBelt.ConveyorDirection.Up, Vector3.down},
		{ConveyorBelt.ConveyorDirection.Right, Vector3.left},
		{ConveyorBelt.ConveyorDirection.Down, Vector3.up},
		{ConveyorBelt.ConveyorDirection.Left, Vector3.right},
		{ConveyorBelt.ConveyorDirection.LeftDown, Vector3.left},
		{ConveyorBelt.ConveyorDirection.LeftUp, Vector3.left},
		{ConveyorBelt.ConveyorDirection.RightDown, Vector3.down},
		{ConveyorBelt.ConveyorDirection.RightUp, Vector3.up}
	};
}
