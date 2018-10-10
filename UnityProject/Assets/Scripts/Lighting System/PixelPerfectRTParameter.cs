using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct PixelPerfectRTParameter : IEquatable<PixelPerfectRTParameter>
{
	public readonly Vector2Int units;
	public readonly int pixelPerUnit;

	public PixelPerfectRTParameter(Vector2Int iUnits, int iPixelPerUnit)
	{
		units = iUnits;

		if (units.x < 1)
		{
			units.x = 1;
		}

		if (units.y < 1)
		{
			units.y = 1;
		}

		pixelPerUnit = Mathf.Clamp(iPixelPerUnit, 1, int.MaxValue);
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

	public static bool operator ==(PixelPerfectRTParameter iLeftHand, PixelPerfectRTParameter iRightHand)
	{
		// Equals handles case of null on right side.
		return iLeftHand.Equals(iRightHand);
	}

	public static bool operator !=(PixelPerfectRTParameter iLeftHand, PixelPerfectRTParameter iRightHand)
	{
		return !(iLeftHand == iRightHand);
	}

	public bool Equals(PixelPerfectRTParameter iParameter)
	{
		return this.units == iParameter.units &&
		       this.pixelPerUnit == iParameter.pixelPerUnit;
	}
}