using System;
using UnityEngine;

[Serializable]
public class XYCoord
{
	[SerializeField] private int x;
	[SerializeField] private int y;

	public XYCoord(int x, int y)
	{
		this.x = x;
		this.y = y;
	}

	public int X => x;

	public int Y => y;

	public override string ToString()
	{
		return $"{x},{y}";
	}
}