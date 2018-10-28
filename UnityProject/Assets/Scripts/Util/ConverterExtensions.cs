using UnityEngine;


	public static class ConverterExtensions
	{
		public static Vector3Int RoundToInt(this Vector3 other)
		{
			return Vector3Int.RoundToInt(other);

		}

		public static Vector3Int RoundToInt(this Vector2 other)
		{
			return Vector3Int.RoundToInt(other);

		}

		/// Round to int while cutting z-axis
		public static Vector3Int CutToInt( this Vector3 other ) {
			return Vector3Int.RoundToInt( ( Vector2 ) other );
		}
	}
