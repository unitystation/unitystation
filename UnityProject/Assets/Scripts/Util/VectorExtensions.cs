using System;
using UnityEngine;

public static class VectorExtensions
{
	public static bool IsInRadius(this Vector2 position, float outerRadius, float innerRadius)
	{
		var sqrMag = position.sqrMagnitude;
		return sqrMag <= outerRadius * outerRadius && (innerRadius <= 0 || sqrMag >= innerRadius * innerRadius);
	}

	public static Vector3 RotateAround(this Vector3 position, Vector3 pivot, Vector3 axis, float angle) =>
		Quaternion.AngleAxis(angle, axis) * (position - pivot) + pivot;

	public static Vector3 RotateAroundZ(this Vector3 position, Vector3 pivot, float angle) =>
		position.RotateAround(pivot, Vector3.forward, angle);

	public static Vector2 RotateAround(this Vector2 position, Vector2 pivot, Vector3 axis, float angle)
	{
		Vector3 pos3D = position;
		Vector3 pivot3D = pivot;
		return Quaternion.AngleAxis(angle, axis) * (pos3D - pivot3D) + pivot3D;
	}

	public static Vector2 RotateAroundZ(this Vector2 position, Vector2 pivot, float angle) =>
		position.RotateAround(pivot, Vector3.forward, angle);

	public static Vector3 Clamp(this Vector3 v, Vector3 min, Vector3 max) =>
		new Vector3(
			Mathf.Clamp(v.x, min.x, max.x),
			Mathf.Clamp(v.y, min.y, max.y),
			Mathf.Clamp(v.z, min.z, max.z));

	public static Vector2 RadianToVector2(float radian)
	{
		return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
	}
	public static Vector2 DegreeToVector2(float degree)
	{
		return RadianToVector2(degree * Mathf.Deg2Rad);
	}
}
