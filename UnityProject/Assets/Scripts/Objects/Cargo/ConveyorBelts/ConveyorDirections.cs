using System.Collections.Generic;
using UnityEngine;

namespace Construction.Conveyors
{
	public static class ConveyorDirections
	{
		public static Dictionary<ConveyorBelt.ConveyorDirection, Vector3> directionsForward =
				new Dictionary<ConveyorBelt.ConveyorDirection, Vector3>()
		{
			{ConveyorBelt.ConveyorDirection.Up, Vector3.up},
			{ConveyorBelt.ConveyorDirection.Right, Vector3.right},
			{ConveyorBelt.ConveyorDirection.Down, Vector3.down},
			{ConveyorBelt.ConveyorDirection.Left, Vector3.left},
			{ConveyorBelt.ConveyorDirection.LeftDown, Vector3.down},
			{ConveyorBelt.ConveyorDirection.LeftUp, Vector3.up},
			{ConveyorBelt.ConveyorDirection.RightDown, Vector3.down},
			{ConveyorBelt.ConveyorDirection.RightUp, Vector3.up},
			{ConveyorBelt.ConveyorDirection.DownLeft, Vector3.left},
			{ConveyorBelt.ConveyorDirection.UpLeft, Vector3.left},
			{ConveyorBelt.ConveyorDirection.DownRight, Vector3.right},
			{ConveyorBelt.ConveyorDirection.UpRight, Vector3.right}
		};

		public static Dictionary<ConveyorBelt.ConveyorDirection, Vector3> directionsBackward =
				new Dictionary<ConveyorBelt.ConveyorDirection, Vector3>()
		{
			{ConveyorBelt.ConveyorDirection.Up, Vector3.down},
			{ConveyorBelt.ConveyorDirection.Right, Vector3.left},
			{ConveyorBelt.ConveyorDirection.Down, Vector3.up},
			{ConveyorBelt.ConveyorDirection.Left, Vector3.right},
			{ConveyorBelt.ConveyorDirection.LeftDown, Vector3.left},
			{ConveyorBelt.ConveyorDirection.LeftUp, Vector3.left},
			{ConveyorBelt.ConveyorDirection.RightDown, Vector3.right},
			{ConveyorBelt.ConveyorDirection.RightUp, Vector3.right},
			{ConveyorBelt.ConveyorDirection.DownLeft, Vector3.down},
			{ConveyorBelt.ConveyorDirection.UpLeft, Vector3.up},
			{ConveyorBelt.ConveyorDirection.DownRight, Vector3.down},
			{ConveyorBelt.ConveyorDirection.UpRight, Vector3.up}
		};
	}
}
