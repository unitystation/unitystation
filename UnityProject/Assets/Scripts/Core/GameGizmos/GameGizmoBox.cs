using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameGizmoBox : GameGizmoTracked
{
	public LineRenderer LineRenderer;

	public void SetUp(GameObject TrackingFrom, Vector3 Position, Color Colour, float BoxSize = 0.99f)
	{
		SetUp(Position, TrackingFrom);
		SetColour(Colour);
		LineRenderer.endWidth = BoxSize;
		LineRenderer.startWidth = BoxSize;
	}

	public void SetColour( Color Colour)
	{
		LineRenderer.endColor = Colour;
		LineRenderer.startColor = Colour;
	}
}
