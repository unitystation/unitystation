using UnityEngine;

namespace Util
{
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
	}
}