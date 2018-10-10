using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct PixelPerfectRTParameter 
{
	public readonly Vector2 matchAgainstUnits;

	public Vector2Int units;
	public int pixelPerUnit;

	public PixelPerfectRTParameter(Vector2Int iUnits, int iPixelPerUnit, Vector2 iMatchAgainstUnits)
	{
		units = iUnits;
		pixelPerUnit = iPixelPerUnit;
		matchAgainstUnits = iMatchAgainstUnits;
	}

	public Vector2Int resolution => units * pixelPerUnit;

	public Vector2 GetRendererPosition(Vector3 iPositionToMatch)
	{
		float _occlusionUnitPerPixel = (float)units.x / (pixelPerUnit * units.x);

		float _x = _occlusionUnitPerPixel * (int)(iPositionToMatch.x / _occlusionUnitPerPixel);
		float _y = _occlusionUnitPerPixel * (int)(iPositionToMatch.y / _occlusionUnitPerPixel);

		return new Vector2(_x, _y);
	}

	public Vector2 GetRendererViewport()
	{
		Vector2 _viewportPerUnit = new Vector2(1f / matchAgainstUnits.x, 1f / matchAgainstUnits.y);

		return new Vector2(_viewportPerUnit.x * matchAgainstUnits.x, units.y);
	}
}