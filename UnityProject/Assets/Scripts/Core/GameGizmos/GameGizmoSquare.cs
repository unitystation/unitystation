using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameGizmoSquare : GameGizmoTracked
{
	public List<LineRenderer> Lines = new List<LineRenderer>();


	public void SetUp(GameObject TrackingFrom, Vector3 Position, Color Colour, float LineThickness , float BoxSize = 1)
	{
		SetUp(Position, TrackingFrom);
		foreach (var Line in Lines)
		{
			Line.endWidth = LineThickness;
			Line.startWidth = LineThickness;
			Line.transform.localScale = new(BoxSize, BoxSize, BoxSize);
			Line.startColor = Colour;
			Line.endColor = Colour;
		}
	}

	public void SetColour(Color Colour)
	{
		foreach (var Line in Lines)
		{
			Line.startColor = Colour;
			Line.endColor = Colour;
		}
	}

	public void SetLineThickness(float LineThickness)
	{
		foreach (var Line in Lines)
		{
			Line.endWidth = LineThickness;
			Line.startWidth = LineThickness;
		}
	}

}
