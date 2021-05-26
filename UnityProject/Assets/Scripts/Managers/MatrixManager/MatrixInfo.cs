using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Atmospherics;
using TileManagement;
using Tilemaps.Behaviours.Layers;

/// Class that helps identify matrices
public class MatrixInfo : IEquatable<MatrixInfo>
{
	public readonly int Id;
	public Matrix Matrix;
	public MetaTileMap MetaTileMap;
	public MetaDataLayer MetaDataLayer;
	public SubsystemManager SubsystemManager;
	public TileChangeManager TileChangeManager;
	public ReactionManager ReactionManager;
	public GameObject GameObject;
	public Vector3Int CachedOffset;

	// position we were at when offset was last cached, to check if it is invalid
	private Vector3 cachedPosition;

	// Transform containing all the physical objects on the map
	public Transform Objects;
	public MatrixMove MatrixMove;
	private Vector3Int initialOffset;
	private uint netId;

	public BoundsInt Bounds => MetaTileMap.GetBounds();

	public BoundsInt WorldBounds => MetaTileMap.GetWorldBounds();

	public Transform ObjectParent => MetaTileMap.ObjectLayer.transform;

	public Color Color => Matrix ? Matrix.Color : Color.red;

	public float Speed => MatrixMove ? MatrixMove.ServerState.Speed : 0f;

	public string Name => Matrix.gameObject.name;

	//todo: placeholder, should depend on solid tiles count instead (and use caching)
	public float Mass => Bounds.size.sqrMagnitude/1000f;

	public bool IsMovable => MatrixMove != null;

	public Vector2Int MovementVector => ( IsMovable && MatrixMove.IsMovingServer ) ? MatrixMove.ServerState.FlyingDirection.VectorInt : Vector2Int.zero;

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
			CachedOffset = initialOffset + (state.Position.RoundToInt() - MatrixMove.InitialPosition);
		}

		return CachedOffset;
	}

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
	public static readonly MatrixInfo Invalid = new MatrixInfo(-1);

	public MatrixInfo(int id)
	{
		Id = id;
	}

	public override string ToString()
	{
		if(Equals(Invalid))
		{
			return "[Invalid matrix]";
		}
		else
		{
			string objectName = "MatrixInfo";
			if(GameObject != null)
				objectName = GameObject.name;

			string pivot = "pivot";
			string state = "state";
			if(MatrixMove != null)
			{
				pivot = MatrixMove.Pivot.ToString();
				state = MatrixMove.ServerState.ToString();
			}

			return $"[({Id}){objectName},offset={Offset},pivot={pivot},state={state},netId={NetID}]";
		}
	}

	public bool Equals(MatrixInfo other) => Id == other?.Id;

	public override bool Equals(object obj) => obj is MatrixInfo other && Equals(other);

	public override int GetHashCode() => Id;

	///Figuring out netId. NetworkIdentity is located on the pivot (parent) gameObject for MatrixMove-equipped matrices
	private static uint getNetId(Matrix matrix)
	{
		var netId = NetId.Invalid;
		if (!matrix)
		{
			return netId;
		}

		NetworkedMatrix networkedMatrix = matrix.gameObject.GetComponentInParent<NetworkedMatrix>();

		if (networkedMatrix.OrNull()?.MatrixSync == null)
		{
			//Try to find if null
			networkedMatrix.BackUpSetMatrixSync();
		}

		NetworkIdentity component = networkedMatrix.OrNull()?.MatrixSync.OrNull()?.GetComponent<NetworkIdentity>();

		if (component && component.netId != NetId.Invalid && component.netId != NetId.Empty)
		{
			netId = component.netId;
		}

		if (netId == NetId.Invalid)
		{
			Logger.LogWarning($"Invalid NetID for matrix {matrix.gameObject.name}!", Category.Matrix);
		}

		return netId;
	}

	private sealed class IdEqualityComparer : IEqualityComparer<MatrixInfo>
	{
		public bool Equals(MatrixInfo x, MatrixInfo y) => x?.Id == y?.Id;

		public int GetHashCode(MatrixInfo obj) => obj.Id;
	}

	public static IEqualityComparer<MatrixInfo> IdComparer { get; } = new IdEqualityComparer();

	public static bool operator ==(MatrixInfo left, MatrixInfo right) =>
		ReferenceEquals(left, null) == false && left.Equals(right);

	public static bool operator !=(MatrixInfo left, MatrixInfo right) =>
		ReferenceEquals(left, null) == false && left.Equals(right) == false;
}