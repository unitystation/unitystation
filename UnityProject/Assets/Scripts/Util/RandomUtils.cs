using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomUtils
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
}
