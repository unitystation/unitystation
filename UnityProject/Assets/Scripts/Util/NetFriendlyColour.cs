using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetFriendlyColour
{
	public float R = 1;
	public float G = 1;
	public float B = 1;
	public float A = 1;

	private Color Colour;

	public void SetColour(Color InColour)
	{
		Colour = InColour;
		R = Colour.r;
		G = Colour.g;
		B = Colour.b;
		A = Colour.a;
	}


	public Color GetColor()
	{
		Colour.a = A;
		Colour.g = G;
		Colour.b = B;
		Colour.r = R;
		return Colour;
	}
}
