using System;
using System.Text;
using UnityEngine;


public static class ConverterExtensions
{
	public static Vector2 To2(this Vector3 other)
	{
		return new Vector2(other.x, other.y);
	}

	public static Vector3Int RoundToInt(this Vector3 other)
	{
		return Vector3Int.RoundToInt(other);
	}

	public static Vector3Int RoundToInt(this Vector2 other)
	{
		return Vector3Int.RoundToInt(other);
	}

	/// <summary>Round to int while cutting z-axis</summary>
	public static Vector3Int CutToInt(this Vector3 other)
	{
		return Vector3Int.RoundToInt((Vector2)other);
	}

	/// <summary>Round to int</summary>
	public static Vector2Int To2Int(this Vector2 other)
	{
		return Vector2Int.RoundToInt(other);
	}

	/// <summary>Round to int while cutting z-axis</summary>
	public static Vector2Int To2Int(this Vector3 other)
	{
		return Vector2Int.RoundToInt(other);
	}

	/// <summary>Convert V3Int to V2Int</summary>
	public static Vector2Int To2Int(this Vector3Int other)
	{
		return Vector2Int.RoundToInt((Vector3)other);
	}

	/// <summary>Convert V2Int to V3Int</summary>
	public static Vector3Int To3Int(this Vector2Int other)
	{
		return Vector3Int.RoundToInt((Vector2)other);
	}

	/// <summary>
	/// Clamp vector so it's either -1, 0, or 1 on X and Y axes.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public static Vector2Int Normalize(this Vector2Int other)
	{
		return new Vector2Int(Mathf.Clamp(other.x, -1, 1), Mathf.Clamp(other.y, -1, 1));
	}

	/// <summary>
	/// Clamp vector so it's either -1, 0, or 1 on X and Y axes.
	/// Z is always 0!
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public static Vector2Int NormalizeToInt(this Vector2 other)
	{
		return new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(other.x), -1, 1),
			Mathf.Clamp(Mathf.RoundToInt(other.y), -1, 1));
	}

	/// <summary>
	/// Clamp vector so it's either -1, 0, or 1 on X and Y axes.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public static Vector2Int NormalizeTo2Int(this Vector3Int other)
	{
		return new Vector2Int(Mathf.Clamp(other.x, -1, 1), Mathf.Clamp(other.y, -1, 1));
	}

	/// <summary>
	/// Clamp vector so it's either -1, 0, or 1 on X and Y axes.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public static Vector2Int NormalizeTo2Int(this Vector3 other)
	{
		return new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(other.x), -1, 1),
			Mathf.Clamp(Mathf.RoundToInt(other.y), -1, 1));
	}

	/// <summary>
	/// Clamp vector so it's either -1, 0, or 1 on X and Y axes.
	/// Z is always 0!
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public static Vector3Int Normalize(this Vector3Int other)
	{
		return new Vector3Int(Mathf.Clamp(other.x, -1, 1), Mathf.Clamp(other.y, -1, 1), 0);
	}

	/// <summary>
	/// Clamp vector so it's either -1, 0, or 1 on X and Y axes.
	/// Z is always 0!
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public static Vector3Int NormalizeToInt(this Vector3 other)
	{
		return new Vector3Int(Mathf.Clamp(Mathf.RoundToInt(other.x), -1, 1),
			Mathf.Clamp(Mathf.RoundToInt(other.y), -1, 1),
			0);
	}

	/// <summary>
	/// Clamp vector so it's either -1, 0, or 1 on X and Y axes.
	/// Z is always 0!
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public static Vector3Int NormalizeTo3Int(this Vector2 other)
	{
		return new Vector3Int(Mathf.Clamp(Mathf.RoundToInt(other.x), -1, 1),
			Mathf.Clamp(Mathf.RoundToInt(other.y), -1, 1),
			0);
	}

	/// <summary>
	/// Clamp vector so it's either -1, 0, or 1 on X and Y axes.
	/// Z is always 0!
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public static Vector3Int NormalizeTo3Int(this Vector2Int other)
	{
		return new Vector3Int(Mathf.Clamp(other.x, -1, 1), Mathf.Clamp(other.y, -1, 1), 0);
	}

	public static Vector3 ToLocal(this Vector3 worldPos, Matrix matrix)
	{
		return MatrixManager.WorldToLocal(worldPos, MatrixManager.Get(matrix));
	}

	public static Vector3 ToLocal(this Vector3 worldPos)
	{
		return MatrixManager.WorldToLocal(worldPos,
			MatrixManager.AtPoint(Vector3Int.RoundToInt(worldPos), CustomNetworkManager.Instance._isServer));
	}

	public static Vector3 ToWorld(this Vector3 localPos)
	{
		return MatrixManager.LocalToWorld(localPos,
			MatrixManager.AtPoint(Vector3Int.RoundToInt(localPos), CustomNetworkManager.Instance._isServer));
	}


	public static Vector3 ToWorld(this Vector3 localPos, Matrix matrix)
	{
		return MatrixManager.LocalToWorld(localPos, MatrixManager.Get(matrix));
	}

	public static Vector3Int ToLocalInt(this Vector3 worldPos, Matrix matrix)
	{
		return MatrixManager.WorldToLocalInt(worldPos, MatrixManager.Get(matrix));
	}

	public static Vector3Int ToWorldInt(this Vector3 localPos, Matrix matrix)
	{
		return MatrixManager.LocalToWorldInt(localPos, MatrixManager.Get(matrix));
	}

	public static Vector3 ToLocal(this Vector3 worldPos, MatrixInfo matrix)
	{
		return MatrixManager.WorldToLocal(worldPos, matrix);
	}

	public static Vector3 ToWorld(this Vector3 localPos, MatrixInfo matrix)
	{
		return MatrixManager.LocalToWorld(localPos, matrix);
	}

	public static Vector3Int ToLocalInt(this Vector3 worldPos, MatrixInfo matrix)
	{
		return MatrixManager.WorldToLocalInt(worldPos, matrix);
	}

	public static Vector3Int ToWorldInt(this Vector3 localPos, MatrixInfo matrix)
	{
		return MatrixManager.LocalToWorldInt(localPos, matrix);
	}

	//======== | Cool serialisation stuff | =========

	public static Color UncompresseToColour(this string SerialiseData)
	{
		Color TheColour = Color.white;
		TheColour.r = ((int)SerialiseData[0] / 255f);
		TheColour.g = ((int)SerialiseData[1] / 255f);
		TheColour.b = ((int)SerialiseData[2] / 255f);
		TheColour.a = ((int)SerialiseData[3] / 255f);
		return TheColour;
	}

	public static string ToStringCompressed(this Color SetColour)
	{
		StringBuilder ToReturn = new StringBuilder(4);
		ToReturn.Append(Convert.ToChar(Mathf.RoundToInt(SetColour.r * 255)));
		ToReturn.Append(Convert.ToChar(Mathf.RoundToInt(SetColour.g * 255)));
		ToReturn.Append(Convert.ToChar(Mathf.RoundToInt(SetColour.b * 255)));
		ToReturn.Append(Convert.ToChar(Mathf.RoundToInt(SetColour.a * 255)));
		return ToReturn.ToString();
	}

}
