using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// Struct that helps identify matrices
public struct MatrixInfo
{
	public int Id;
	public Matrix Matrix;
	public MetaTileMap MetaTileMap;
	public MetaDataLayer MetaDataLayer;
	public SubsystemManager SubsystemManager;
	public GameObject GameObject;
	public BoundsInt Bounds => MetaTileMap.GetBounds();
	public BoundsInt WorldBounds => MetaTileMap.GetWorldBounds();
	public Vector3Int CachedOffset;
	// position we were at when offset was last cached, to check if it is invalid
	private Vector3 cachedPosition;

	/// <summary>
	/// Transform containing all the physical objects on the map
	public Transform Objects;

	private Vector3Int initialOffset;

	public Vector3Int InitialOffset
	{
		get { return initialOffset; }
		set
		{
			initialOffset = value;
		}
	}

	public Vector3Int Offset => GetOffset();

	public Vector3Int GetOffset(MatrixState state = default(MatrixState))
	{
		if (!MatrixMove)
		{
			return InitialOffset;
		}

		if (state.Equals(default(MatrixState)))
		{
			state = MatrixMove.ClientState;
		}

		if (cachedPosition != state.Position)
		{
			//if we moved, update cached offset
			cachedPosition = state.Position;
			CachedOffset = initialOffset + (state.Position.RoundToInt() - MatrixMove.InitialPos);
		}

		return CachedOffset;
	}

	public MatrixMove MatrixMove;

	private uint netId;

	public uint NetID
	{
		get
		{
			//late init, because server is not yet up during InitMatrices()
			if (netId == NetId.Invalid || netId == NetId.Empty)
			{
				netId = getNetId(Matrix);
			}

			return netId;
		}
	}

	/// Null object
	public static readonly MatrixInfo Invalid = new MatrixInfo {Id = -1};

	public override string ToString()
	{
		return Equals(Invalid)
			? "[Invalid matrix]"
			: $"[({Id}){GameObject.name},offset={Offset},pivot={MatrixMove?.Pivot},state={MatrixMove?.State},netId={NetID}]";
	}

	public bool Equals(MatrixInfo other)
	{
		return Id == other.Id;
	}

	public override bool Equals(object obj)
	{
		return obj is MatrixInfo other && Equals( other );
	}

	public override int GetHashCode()
	{
		return Id;
	}

	///Figuring out netId. NetworkIdentity is located on the pivot (parent) gameObject for MatrixMove-equipped matrices
	private static uint getNetId(Matrix matrix)
	{
		var netId = NetId.Invalid;
		if (!matrix)
		{
			return netId;
		}

		NetworkIdentity component = matrix.gameObject.GetComponentInParent<NetworkIdentity>();
		NetworkIdentity componentInParent = matrix.gameObject.GetComponentInParent<NetworkIdentity>();
		if (component && component.netId != NetId.Invalid && component.netId != NetId.Empty)
		{
			netId = component.netId;
		}

		if (componentInParent && componentInParent.netId != NetId.Invalid && component.netId != NetId.Empty)
		{
			netId = componentInParent.netId;
		}

		if (netId == NetId.Invalid)
		{
			Logger.LogWarning($"Invalid NetID for matrix {matrix.gameObject.name}!", Category.Matrix);
		}

		return netId;
	}

	private sealed class IdEqualityComparer : IEqualityComparer<MatrixInfo>
	{
		public bool Equals( MatrixInfo x, MatrixInfo y )
		{
			return x.Id == y.Id;
		}

		public int GetHashCode( MatrixInfo obj )
		{
			return obj.Id;
		}
	}

	public static IEqualityComparer<MatrixInfo> IdComparer { get; } = new IdEqualityComparer();

	public static bool operator ==( MatrixInfo left, MatrixInfo right )
	{
		return left.Equals( right );
	}

	public static bool operator !=( MatrixInfo left, MatrixInfo right )
	{
		return !left.Equals( right );
	}
}

public static class MatrixExtensions
{
	public static bool BoundsIntersect( this MatrixInfo matrix, MatrixInfo otherMatrix )
	{
		if ( matrix == otherMatrix )
		{
			return false;
		}

		var rect = matrix.WorldBounds.Extend( 1 ).ToRect();

		return rect.Overlaps( otherMatrix.WorldBounds.Extend( 1 ).ToRect() );
	}

	public static Rect ToRect( this BoundsInt bounds )
	{
		return new Rect( (Vector3)bounds.position, (Vector3)bounds.size );
	}
	public static RectInt ToRectInt( this BoundsInt bounds )
	{
		return new RectInt( bounds.position.To2Int(), bounds.size.To2Int() );
	}

	/// <summary>
	/// Extend/shrink bounds on all sides by integer amount
	/// </summary>
	public static BoundsInt Extend( this BoundsInt bounds, int amount )
	{
		var min = bounds.min - ( Vector3Int.one * amount );
		var max = bounds.max + ( Vector3Int.one * amount );
		return new BoundsInt(min, max - min);
	}
}