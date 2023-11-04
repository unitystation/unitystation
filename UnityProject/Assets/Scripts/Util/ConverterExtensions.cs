using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Random = System.Random;
using PlayerMoveDirection = MovementSynchronisation.PlayerMoveDirection;

public static class ConverterExtensions
{
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

	public static Vector3Int ToWorldInt(this Vector3Int worldPos, Matrix matrix)
	{
		return MatrixManager.LocalToWorldInt(worldPos, MatrixManager.Get(matrix));
	}

	public static bool IsDiagonal(this PlayerMoveDirection direction) =>
		direction switch
		{
			PlayerMoveDirection.Down => false,
			PlayerMoveDirection.Up => false,
			PlayerMoveDirection.Left => false,
			PlayerMoveDirection.Right => false,
			_ => true
		};

	/// <summary>
	/// Converts an ordinal direction into a cardinal direction.
	/// </summary>
	/// <param name="direction">The direction to convert.</param>
	/// <param name="vertical">Should it return the vertical direction.</param>
	public static PlayerMoveDirection ToNonDiagonal(this PlayerMoveDirection direction, bool vertical) =>
		direction switch
		{
			PlayerMoveDirection.Down_Left => vertical ? PlayerMoveDirection.Down : PlayerMoveDirection.Left,
			PlayerMoveDirection.Down_Right => vertical ? PlayerMoveDirection.Down : PlayerMoveDirection.Right,
			PlayerMoveDirection.Up_Left => vertical ? PlayerMoveDirection.Up : PlayerMoveDirection.Left,
			PlayerMoveDirection.Up_Right => vertical ? PlayerMoveDirection.Up : PlayerMoveDirection.Right,
			_ => PlayerMoveDirection.Down
		};

	/// <summary>
	/// Converts a direction enum to its Vector2Int equivalent.
	/// </summary>
	public static Vector2Int ToVector(this PlayerMoveDirection direction) =>
		direction switch
		{
			PlayerMoveDirection.Up_Left => new Vector2Int(-1, 1),
			PlayerMoveDirection.Up => Vector2Int.up,
			PlayerMoveDirection.Up_Right => Vector2Int.one,

			PlayerMoveDirection.Left => Vector2Int.left,
			PlayerMoveDirection.Right => Vector2Int.right,

			PlayerMoveDirection.Down_Left => new Vector2Int(-1, -1),
			PlayerMoveDirection.Down => Vector2Int.down,
			PlayerMoveDirection.Down_Right => new Vector2Int(1, -1),
			_ => Vector2Int.zero
		};

	//======== | Cool serialisation stuff | =========

	public static string PalletToString(List<Color> Pallet)
	{
		if (Pallet == null) return "";
		StringBuilder ToReturn = new StringBuilder();
		foreach (var Colour in Pallet)
		{
			ToReturn.Append('◉');
			ToReturn.Append(Colour.r.ToString());
			ToReturn.Append(',');
			ToReturn.Append(Colour.g.ToString());
			ToReturn.Append(',');
			ToReturn.Append(Colour.b.ToString());
			ToReturn.Append(',');
			ToReturn.Append(Colour.a.ToString());
		}

		return ToReturn.ToString();
	}

	public static List<Color> StringToPallet(string stringPallet)
	{

		List<Color> ToReturn = new List<Color>();
		if (string.IsNullOrEmpty(stringPallet)) return ToReturn;

		var Loop = stringPallet.Split('◉');
		foreach (var StringColour in Loop)
		{
			var colour = Color.white;
			var RGBA = StringColour.Split(',');
			for (int i = 0; i < RGBA.Length; i++)
			{
				switch (i)
				{
					case 0:
						colour.r = float.Parse(RGBA[i]);
						break;
					case 1:
						colour.g = float.Parse(RGBA[i]);
						break;
					case 2:
						colour.b = float.Parse(RGBA[i]);
						break;
					case 3:
						colour.a = float.Parse(RGBA[i]);
						break;
				}
			}
			ToReturn.Add(colour);
		}

		return ToReturn;
	}


	public static  string ToSerialiseString(this Vector3 Vector3data)
	{
		return $"{Vector3data.x},{Vector3data.y},{Vector3data.z}";
	}

	public static  Vector3 ToVector3(this string SerialiseData)
	{
		Vector3 TheColour = Vector3.zero;
		string[] XYZ = SerialiseData.Split(',');
		TheColour.x = float.Parse(XYZ[0]);
		TheColour.y = float.Parse(XYZ[1]);
		TheColour.z = float.Parse(XYZ[2]);

		return TheColour;
	}



	public static Color ToColour(this string SerialiseData)
	{
		Color TheColour = Color.white;
		string[] rgbaValues = SerialiseData.Split(',');
		TheColour.r = (int.Parse(rgbaValues[0]) / 255f) ;
		TheColour.g = (int.Parse(rgbaValues[1]) / 255f) ;
		TheColour.b = (int.Parse(rgbaValues[2]) / 255f) ;
		TheColour.a = (int.Parse(rgbaValues[3]) / 255f) ;
		return TheColour;
	}


	public static Color UncompresseToColour(this string SerialiseData)
	{
		if (string.IsNullOrEmpty(SerialiseData) ||  SerialiseData.Length != 4)
		{
			return Color.white;
		}
		else
		{
			Color TheColour = Color.white;
			TheColour.r = ((int) SerialiseData[0] / 255f);
			TheColour.g = ((int) SerialiseData[1] / 255f);
			TheColour.b = ((int) SerialiseData[2] / 255f);
			TheColour.a = ((int) SerialiseData[3] / 255f);
			return TheColour;
		}
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

	public static Vector2Int ToLocalVector2Int(this OrientationEnum @in)
	{
		return ToLocalVector3(@in).RoundTo2Int();
	}

	/// <summary>
	/// Takes an <see cref="OrientationEnum"/> and returns a unit <see cref="Vector3"/> direction.
	/// </summary>
	public static Vector3 ToLocalVector3(this OrientationEnum @in) =>
		@in switch
		{
			OrientationEnum.Up_By0 => Vector3.up,
			OrientationEnum.Right_By270 => Vector3.right,
			OrientationEnum.Down_By180 => Vector3.down,
			OrientationEnum.Left_By90 => Vector3.left,
			_ => Vector3.zero
		};



	/// <summary>
	/// Takes a unit <see cref="Vector2Int"/> direction with a single axis set to 1 or -1 and returns the equivalent
	/// <see cref="OrientationEnum"/>.
	/// </summary>
	public static OrientationEnum ToOrientationEnum(this Vector2Int direction) =>
		direction switch
		{
			{ y: -1 } => OrientationEnum.Down_By180,
			{ x: -1 } => OrientationEnum.Left_By90,
			{ y: 1 } => OrientationEnum.Up_By0,
			{ x: 1 } => OrientationEnum.Right_By270,
			_ => OrientationEnum.Down_By180
		};
}