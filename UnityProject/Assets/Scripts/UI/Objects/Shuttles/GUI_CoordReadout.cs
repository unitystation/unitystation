﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Updates X and Y text refrences from the MatrixMove in the ShuttleControlScript
/// </summary>
public class GUI_CoordReadout : MonoBehaviour
{
	[Header("References")]
	public Text xText;
	public Text yText;

	private int valueX = 0;
	private int valueY = 0;

	void Start()
	{
		if (xText == null || yText == null)
		{
			Logger.LogError("Coord Readout not setup correctly!", Category.Shuttles);
			this.enabled = false;
			return;
		}
	}

	/// <summary>
	/// Sets text values using Vector3, uses X and Y
	/// </summary>
	/// <param name="position"></param>
	public void SetCoords(Vector3 position)
	{
		SetCoords((int)position.x, (int)position.y);
	}

	/// <summary>
	/// Sets both the X and Y text values
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	public void SetCoords(int x, int y)
	{
		valueX = x;
		valueY = y;
		xText.text = valueX.ToString();
		yText.text = valueY.ToString();
	}

	/// <summary>
	/// Returns the currently set X value
	/// </summary>
	/// <returns>X value</returns>
	public int GetValueX()
	{
		return valueX;
	}

	/// <summary>
	/// Returns the currently set Y value
	/// </summary>
	/// <returns>Y value</returns>
	public int GetValueY()
	{
		return valueY;
	}
}
