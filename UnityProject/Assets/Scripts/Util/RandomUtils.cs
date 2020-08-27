using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomUtils
{
	public static Quaternion RandomRotatation2D()
	{
		var axis = new Vector3(0, 0, 1);
		var randomRotation = Quaternion.AngleAxis(Random.Range(-180f, 180f), axis);

		return randomRotation;
	}
}
