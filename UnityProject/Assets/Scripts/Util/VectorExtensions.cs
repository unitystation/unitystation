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
}
