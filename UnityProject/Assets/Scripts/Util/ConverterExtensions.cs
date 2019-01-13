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
		/// Round to int
		public static Vector2Int To2Int( this Vector2 other ) {
			return Vector2Int.RoundToInt( other );
		}
		/// Round to int while cutting z-axis
		public static Vector2Int To2Int( this Vector3 other ) {
			return Vector2Int.RoundToInt( other );
		}
		/// Convert V3Int to V2Int
		public static Vector2Int To2Int( this Vector3Int other ) {
			return Vector2Int.RoundToInt( (Vector3)other );
		}
		/// Convert V2Int to V3Int
		public static Vector3Int To3Int( this Vector2Int other ) {
			return Vector3Int.RoundToInt( (Vector2)other );
		}

		public static Vector3 ToLocal( this Vector3 worldPos, Matrix matrix ) {
			return MatrixManager.WorldToLocal(worldPos, MatrixManager.Get( matrix ));
		}

		public static Vector3 ToWorld( this Vector3 localPos, Matrix matrix ) {
			return MatrixManager.LocalToWorld( localPos, MatrixManager.Get( matrix ));
		}

		public static Vector3Int ToLocalInt( this Vector3 worldPos, Matrix matrix ) {
			return MatrixManager.WorldToLocalInt(worldPos, MatrixManager.Get( matrix ));
		}

		public static Vector3Int ToWorldInt( this Vector3 localPos, Matrix matrix ) {
			return MatrixManager.LocalToWorldInt( localPos, MatrixManager.Get( matrix ));
		}


	}
