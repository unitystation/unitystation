using System.Runtime.CompilerServices;
using UnityEngine;

public static class SharedConverterExtensions
{
	/// <summary>Convert <see cref="Vector3"/> to <see cref="Vector2"/>.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 To2(this Vector3 other) => other;

	/// <summary>Convert <see cref="Vector3Int"/> to <see cref="Vector2"/>.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 To2(this Vector3Int other) => new(other.x, other.y);

	/// <summary>Convert <see cref="Vector2"/> to <see cref="Vector3"/> with z-axis set to 0.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 To3(this Vector2 other) => other;

	/// <summary>Convert <see cref="Vector2Int"/> to <see cref="Vector3"/> with z-axis set to 0.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 To3(this Vector2Int other) => new(other.x, other.y, 0);

	/// <summary>Convert <see cref="Vector3Int"/> to <see cref="Vector3"/>.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 To3(this Vector3Int other) => other;

	/// <summary>Convert <see cref="Vector3Int"/> to <see cref="Vector2Int"/>.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2Int To2Int(this Vector3Int other) => new(other.x, other.y);

	/// <summary>Convert <see cref="Vector2Int"/> to <see cref="Vector3Int"/> with z-axis set to 0.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3Int To3Int(this Vector2Int other) => new(other.x, other.y, 0);

	/// <summary>Convert <see cref="Vector2"/> to <see cref="Vector3Int"/> with z-axis set to 0.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3Int To3Int(this Vector2 other) => RoundToInt(other);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 To2(this Vector2Int other) => new(other.x, other.y);

	/// <summary>Cast (Truncate) <see cref="Vector3"/> to <see cref="Vector3Int"/> while cutting z-axis</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3Int TruncateToInt(this Vector3 other) => new((int) other.x, (int) other.y, (int) other.z);

	/// <summary>Round <see cref="Vector2"/> to <see cref="Vector2Int"/>.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2Int RoundTo2Int(this Vector2 other) =>
		Vector2Int.RoundToInt(other);

	/// <summary>Round <see cref="Vector3"/> to <see cref="Vector2Int"/>.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2Int RoundTo2Int(this Vector3 other) =>
		Vector2Int.RoundToInt(other);

	/// <summary>Round <see cref="Vector3"/> to <see cref="Vector3Int"/>.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3Int RoundToInt(this Vector3 other) =>
		Vector3Int.RoundToInt(other);

	/// <summary>Round <see cref="Vector2"/> to <see cref="Vector3Int"/> with z-axis set to 0.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3Int RoundToInt(this Vector2 other) =>
		Vector3Int.RoundToInt(other);

	/// <summary>Round <see cref="Vector3"/> to <see cref="Vector3Int"/> while cutting z-axis.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3Int CutToInt(this Vector3 other) =>
		Vector3Int.RoundToInt((Vector2) other);

	/// <summary>Clamps a vector's components to within 1/-1.</summary>
	/// <remarks>This doesn't actually normalize a vector. Needs a better name.</remarks>
	public static Vector2Int Normalize(this Vector2Int other) =>
		new(other.x.ClampToOne(), other.y.ClampToOne());

	/// <inheritdoc cref="Normalize(Vector2Int)"/>
	public static Vector2Int NormalizeToInt(this Vector2 other) =>
		new(other.x.ClampToOneInt(), other.y.ClampToOneInt());

	/// <inheritdoc cref="Normalize(Vector2Int)"/>
	public static Vector2Int NormalizeTo2Int(this Vector3Int other) =>
		new(other.x.ClampToOne(), other.y.ClampToOne());

	/// <inheritdoc cref="Normalize(Vector2Int)"/>
	public static Vector2Int NormalizeTo2Int(this Vector3 other) =>
		new(other.x.ClampToOneInt(), other.y.ClampToOneInt());

	/// <summary>Clamps a vector's components to within 1/-1 while cutting out the z-axis.</summary>
	/// <returns>A clamped Vector3Int with z set to 0.</returns>
	/// <remarks>This doesn't actually normalize a vector. Needs a better name.</remarks>
	public static Vector3Int Normalize(this Vector3Int other) =>
		new(other.x.ClampToOne(), other.y.ClampToOne(), 0);

	/// <inheritdoc cref="Normalize(Vector3Int)"/>
	public static Vector3Int NormalizeToInt(this Vector3 other) =>
		new(other.x.ClampToOneInt(), other.y.ClampToOneInt(), 0);

	/// <inheritdoc cref="Normalize(Vector3Int)"/>
	public static Vector3Int NormalizeTo3Int(this Vector2 other) =>
		new(other.x.ClampToOneInt(), other.y.ClampToOneInt(), 0);

	/// <inheritdoc cref="Normalize(Vector3Int)"/>
	public static Vector3Int NormalizeTo3Int(this Vector2Int other) =>
		new(other.x.ClampToOne(), other.y.ClampToOne(), 0);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ClampToOne(this int num) => Mathf.Clamp(num, -1, 1);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ClampToOneInt(this float num) => Mathf.RoundToInt(num).ClampToOne();
}
