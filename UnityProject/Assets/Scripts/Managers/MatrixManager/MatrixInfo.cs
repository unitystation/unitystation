using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// Struct that helps identify matrices
public struct MatrixInfo
{
	public int Id;
	public Matrix Matrix;
	public MetaTileMap MetaTileMap;
	public MetaDataLayer MetaDataLayer;
	public SubsystemManager SubsystemManager;
	public GameObject GameObject;
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

	private NetworkInstanceId netId;

	public NetworkInstanceId NetId
	{
		get
		{
			//late init, because server is not yet up during InitMatrices()
			if (netId.IsEmpty() || netId == NetworkInstanceId.Invalid)
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
			: $"[({Id}){GameObject.name},offset={Offset},pivot={MatrixMove?.Pivot},state={MatrixMove?.State},netId={NetId}]";
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
	private static NetworkInstanceId getNetId(Matrix matrix)
	{
		var netId = NetworkInstanceId.Invalid;
		if (!matrix)
		{
			return netId;
		}

		NetworkIdentity component = matrix.gameObject.GetComponentInParent<NetworkIdentity>();
		NetworkIdentity componentInParent = matrix.gameObject.GetComponentInParent<NetworkIdentity>();
		if (component && component.netId != NetworkInstanceId.Invalid && !component.netId.IsEmpty())
		{
			netId = component.netId;
		}

		if (componentInParent && componentInParent.netId != NetworkInstanceId.Invalid && !componentInParent.netId.IsEmpty())
		{
			netId = componentInParent.netId;
		}

		if (netId == NetworkInstanceId.Invalid)
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