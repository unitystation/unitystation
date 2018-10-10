using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct PixelPerfectRTParameter 
{
	/// <summary>
	/// Units to match against.
	/// </summary>
	public readonly Vector2 matchUnits;

	public Vector2Int units;
	public int pixelPerUnit;

	public PixelPerfectRTParameter(Vector2Int iUnits, int iPixelPerUnit, Vector2 iMatchUnits)
	{
		units = iUnits;
		pixelPerUnit = iPixelPerUnit;
		matchUnits = iMatchUnits;
	}

	public Vector2Int resolution => units * pixelPerUnit;

	public float orthographicSize => units.y * 0.5f;

	public Vector2 GetRendererPosition(Vector3 iPositionToMatch)
	{
		float _occlusionUnitPerPixel = (float)units.x / (pixelPerUnit * units.x);

		float _x = _occlusionUnitPerPixel * (int)(iPositionToMatch.x / _occlusionUnitPerPixel);
		float _y = _occlusionUnitPerPixel * (int)(iPositionToMatch.y / _occlusionUnitPerPixel);

		return new Vector2(_x, _y);
	}

	public Vector2 GetRendererViewport()
	{
		Vector2 _viewportPerUnit = new Vector2(1f / matchUnits.x, 1f / matchUnits.y);

		return new Vector2(_viewportPerUnit.x * matchUnits.x, units.y);
	}
}