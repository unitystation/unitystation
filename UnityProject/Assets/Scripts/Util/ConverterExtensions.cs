using System;
using System.Text;
using UnityEngine;
using Random = System.Random;


public static class ConverterExtensions
{
	public static Vector2 To2(this Vector3 other)
	{
		return new Vector2(other.x, other.y);
	}

	public static Vector3 To3(this Vector2 other)
	{
		return new Vector3(other.x, other.y, 0);
	}


	public static Vector3 ToNonInt3(this Vector3Int other)
	{
		return new Vector3(other.x, other.y, other.z);
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
		return Vector3Int.RoundToInt((Vector2) other);
	}

	/// <summary>Round to int</summary>
	public static Vector2Int To2Int(this Vector2 other)
	{
		return Vector2Int.RoundToInt(other);
	}

	public static Vector3Int To3Int(this Vector2 other)
	{
		return new Vector3Int(Mathf.RoundToInt(other.x) , Mathf.RoundToInt(other.y), 0);
	}

	/// <summary>Round to int while cutting z-axis</summary>
	public static Vector2Int To2Int(this Vector3 other)
	{
		return Vector2Int.RoundToInt(other);
	}

	/// <summary>Convert V3Int to V2Int</summary>
	public static Vector2Int To2Int(this Vector3Int other)
	{
		return Vector2Int.RoundToInt((Vector3) other);
	}

	/// <summary>Convert V2Int to V3Int</summary>
	public static Vector3Int To3Int(this Vector2Int other)
	{
		return Vector3Int.RoundToInt((Vector2) other);
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


	public static Vector3 ToWorld(this Vector3 localPos, Matrix matrix)
	{
		return MatrixManager.LocalToWorld(localPos, MatrixManager.Get(matrix));
	}

	public static Vector3 ToWorld(this Vector3Int localPos, Matrix matrix)
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

	public static Vector3 ToLocal(this Vector3 worldPos, MatrixInfo matrixInfo)
	{
		return MatrixManager.WorldToLocal(worldPos, matrixInfo);
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

	public static Vector3 ToLocal(this Vector3Int worldPos, Matrix matrix)
	{
		return MatrixManager.WorldToLocal(worldPos, MatrixManager.Get(matrix));
	}

	public static Vector3 ToLocal(this Vector3Int worldPos)
	{
		return MatrixManager.WorldToLocal(worldPos,
			MatrixManager.AtPoint(Vector3Int.RoundToInt(worldPos), CustomNetworkManager.Instance._isServer));
	}

	public static bool IsDiagonal(this MovementSynchronisation.PlayerMoveDirection Direction)
	{
		switch (Direction)
		{
			case MovementSynchronisation.PlayerMoveDirection.Down:
			case MovementSynchronisation.PlayerMoveDirection.Up:
			case MovementSynchronisation.PlayerMoveDirection.Left:
			case MovementSynchronisation.PlayerMoveDirection.Right:
				return false;
			default:
				return true;
		}
	}

	public static MovementSynchronisation.PlayerMoveDirection ToNonDiagonal(this MovementSynchronisation.PlayerMoveDirection Direction, bool First)
	{
		switch (Direction)
		{
			case MovementSynchronisation.PlayerMoveDirection.Down_Left:
				return First ? MovementSynchronisation.PlayerMoveDirection.Down : MovementSynchronisation.PlayerMoveDirection.Left;
			case MovementSynchronisation.PlayerMoveDirection.Down_Right:
				return First ? MovementSynchronisation.PlayerMoveDirection.Down : MovementSynchronisation.PlayerMoveDirection.Right;
			case MovementSynchronisation.PlayerMoveDirection.Up_Left:
				return First ? MovementSynchronisation.PlayerMoveDirection.Up : MovementSynchronisation.PlayerMoveDirection.Left;
			case MovementSynchronisation.PlayerMoveDirection.Up_Right:
				return First ? MovementSynchronisation.PlayerMoveDirection.Up : MovementSynchronisation.PlayerMoveDirection.Right;
			default:
				return MovementSynchronisation.PlayerMoveDirection.Down;

		}
	}

	public static Vector2 TVectoro(this MovementSynchronisation.PlayerMoveDirection Direction)
	{
		switch (Direction)
		{
			case MovementSynchronisation.PlayerMoveDirection.Up_Left:
				return new Vector2(-1, 1);
			case MovementSynchronisation.PlayerMoveDirection.Up:
				return new Vector2(0, 1);
			case MovementSynchronisation.PlayerMoveDirection.Up_Right:
				return new Vector2(1, 1);

			case MovementSynchronisation.PlayerMoveDirection.Left:
				return new Vector2(-1, 0);
			case MovementSynchronisation.PlayerMoveDirection.Right:
				return new Vector2(1, 0);

			case MovementSynchronisation.PlayerMoveDirection.Down_Left:
				return new Vector2(-1, -1);
			case MovementSynchronisation.PlayerMoveDirection.Down:
				return new Vector2(0, -1);
			case MovementSynchronisation.PlayerMoveDirection.Down_Right:
				return new Vector2(1, -1);
		}
		return Vector2.zero;
	}

	//======== | Cool serialisation stuff | =========

	public static Color UncompresseToColour(this string SerialiseData)
	{
		Color TheColour = Color.white;
		TheColour.r = ((int) SerialiseData[0] / 255f);
		TheColour.g = ((int) SerialiseData[1] / 255f);
		TheColour.b = ((int) SerialiseData[2] / 255f);
		TheColour.a = ((int) SerialiseData[3] / 255f);
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

	public static Random random = new Random();

	public static float GetRandomNumber(float minimum, float maximum)
	{
		return (float) random.NextDouble() * (maximum - minimum) + minimum;
	}

	public static Vector2 GetRandomRotatedVector2(float minimum, float maximum)
	{
		return Vector2.one.Rotate(GetRandomNumber(-90, 90)) *
		       GetRandomNumber(minimum, maximum); //the * Minus number will do the other side Making it full 360
	}

	public static Vector2Int ToLocalVector2Int(this OrientationEnum In)
	{
		return ToLocalVector3(In).To2Int();
	}

	public static Vector3 ToLocalVector3(this OrientationEnum In)
	{
		switch (In)
		{
			case OrientationEnum.Up_By0:
				return Vector3.up;
			case OrientationEnum.Right_By270:
				return Vector3.right;
			case OrientationEnum.Down_By180:
				return Vector3.down;
			case OrientationEnum.Left_By90:
				return Vector3.left;

		}
		return Vector3.zero;
	}


	public static OrientationEnum ToOrientationEnum(this Vector2Int direction)
	{
		if (direction == Vector2Int.down)
		{
			return OrientationEnum.Down_By180;
		}
		else if (direction == Vector2Int.left)
		{
			return OrientationEnum.Left_By90;
		}
		else if (direction == Vector2Int.up)
		{
			return OrientationEnum.Up_By0;
		}
		else if (direction == Vector2Int.right)
		{
			return OrientationEnum.Right_By270;
		}
		else if (direction.y == -1)
		{
			return OrientationEnum.Down_By180;
		}
		else if (direction.y == 1)
		{
			return OrientationEnum.Up_By0;
		}

		return OrientationEnum.Down_By180;
	}


}