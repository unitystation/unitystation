using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class RandomUtils
{
	public static Quaternion RandomRotation2D()
	{
		var axis = new Vector3(0, 0, 1);
		var randomRotation = Quaternion.AngleAxis(UnityEngine.Random.Range(-180f, 180f), axis);

		return randomRotation;
	}

	// Courtesy of https://answers.unity.com/questions/856819/generate-a-random-point-with-an-anullus.html
	/// <summary>
	/// Gets a random point in an annulus within the given minimum and maximum radius.
	/// </summary>
	public static Vector3 RandomAnnulusPoint(float minRadius, float maxRadius)
	{
		Vector2 randomVector = UnityEngine.Random.insideUnitCircle;
		return randomVector.normalized * minRadius + randomVector * (maxRadius - minRadius);
	}

	/// <summary>
	/// Gets a random point on the main station matrix.
	/// </summary>
	public static Vector3Int GetRandomPointOnStation(bool avoidSpace = false, bool avoidImpassable = false)
	{
		var stationMatrix = MatrixManager.MainStationMatrix;
		var stationBounds = stationMatrix.LocalBounds;

		Vector3Int point = default;
		for (int i = 0; i < 10; i++)
		{
			point = stationBounds.allPositionsWithin().PickRandom();

			if (avoidSpace && MatrixManager.IsSpaceAt(point, CustomNetworkManager.IsServer, stationMatrix))
			{
				continue;
			}

			if (avoidImpassable && MatrixManager.IsPassableAtAllMatricesOneTile(point, CustomNetworkManager.IsServer) == false)
			{
				continue;
			}

			break;
		}

		return point;
	}

	public static string CreateRandomBrightColorString()
	{
		return ColorUtility.ToHtmlStringRGBA(CreateRandomBrightColor());
	}

	public static Color CreateRandomBrightColor()
	{
		float h = Random.Range(0f, 1f);
		float s = 1f;
		float v = 0.8f + ((1f - 0.8f) * Random.Range(0f, 1f));
		Color c = Color.HSVToRGB(h, s, v);
		return c;
	}
}
