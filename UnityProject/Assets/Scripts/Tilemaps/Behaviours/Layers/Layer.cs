using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


[ExecuteInEditMode]
public class Layer : MonoBehaviour
{
	/// <summary>
	/// When true, tiles will rotate to their new orientation at the end of matrix rotation. When false
	/// they will rotate to the new orientation at the start of matrix rotation.
	/// </summary>
	private const bool ROTATE_AT_END = true;

	private SubsystemManager subsystemManager;

	public LayerType LayerType;
	protected Tilemap tilemap;

	public Tilemap Tilemap => tilemap;

	public TilemapDamage TilemapDamage { get; private set; }

	public BoundsInt Bounds => boundsCache;
	private BoundsInt boundsCache;

	private Coroutine recalculateBoundsHandle;

	public TileChangeEvent OnTileChanged = new TileChangeEvent();
	/// <summary>
	/// Current offset from our initially mapped orientation. This is used by tiles within the tilemap
	/// to determine what sprite to display. This could be retrieved directly from MatrixMove but
	/// it's faster to cache it here and update when rotation happens.
	/// </summary>
	public RotationOffset RotationOffset { get; private set; }

	/// <summary>
	/// Cached matrixmove that we exist in, null if we don't have one
	/// </summary>
	private MatrixMove matrixMove;

	public Matrix matrix;

	public Vector3Int WorldToCell(Vector3 pos) => tilemap.WorldToCell(pos);
	public Vector3Int LocalToCell(Vector3 pos) => tilemap.LocalToCell(pos);
	public Vector3 LocalToWorld(Vector3 localPos) => tilemap.LocalToWorld(localPos);
	public Vector3 CellToWorld(Vector3Int cellPos) => tilemap.CellToWorld(cellPos);
	public Vector3 WorldToLocal(Vector3 worldPos) => tilemap.WorldToLocal(worldPos);

	public void Awake()
	{
		matrix = GetComponentInParent<Matrix>();
		tilemap = GetComponent<Tilemap>();
		TilemapDamage = GetComponent<TilemapDamage>();
		subsystemManager = GetComponentInParent<SubsystemManager>();
		RecalculateBounds();
		OnTileChanged.AddListener((pos, tile) => TryRecalculateBounds());
		void TryRecalculateBounds()
			{
			if (recalculateBoundsHandle == null)
			{
				this.RestartCoroutine(RecalculateBoundsNextFrame(), ref recalculateBoundsHandle);
			}
		}
	}

	/// <summary>
	/// In case there are lots of sudden changes, recalculate bounds once a frame
	/// instead of doing it for every changed tile
	/// </summary>
	private IEnumerator RecalculateBoundsNextFrame()
	{
		//apparently waiting for next frame doesn't work when looking at Scene view!
		yield return WaitFor.Seconds(0.015f);
		RecalculateBounds();
	}

	public void RecalculateBounds()
	{
		boundsCache = tilemap.cellBounds;
		this.TryStopCoroutine(ref recalculateBoundsHandle);
	}

	public void Start()
	{
		if (!Application.isPlaying)
		{
			return;
		}

		if (MatrixManager.Instance == null)
		{
			Logger.LogError("Matrix Manager is missing from the scene", Category.Matrix);
		}
		else
		{
			// TODO Clean up

			if (LayerType == LayerType.Walls)
			{
				MatrixManager.Instance.wallsTileMaps.Add(GetComponent<TilemapCollider2D>(), tilemap);
			}

		}

		InitFromMatrix();
	}

	public void InitFromMatrix()
	{
		RotationOffset = RotationOffset.Same;
		matrixMove = transform.root.GetComponent<MatrixMove>();
		if (matrixMove != null)
		{
			Logger.LogTraceFormat("{0} layer initializing from matrix", Category.Matrix, matrixMove);
			matrixMove.MatrixMoveEvents.OnRotate.AddListener(OnRotate);
			//initialize from current rotation
			OnRotate(MatrixRotationInfo.FromInitialRotation(matrixMove, NetworkSide.Client, RotationEvent.Register));
		}
	}


	private void OnRotate(MatrixRotationInfo info)
	{
		if (info.IsEnding || info.IsObjectBeingRegistered)
		{
			RotationOffset = info.RotationOffsetFromInitial;
			Logger.LogTraceFormat("{0} layer redrawing with offset {1}", Category.Matrix, info.MatrixMove, RotationOffset);
			tilemap.RefreshAllTiles();
		}
	}

	public virtual bool IsPassableAt(Vector3Int from, Vector3Int to, bool isServer,
		CollisionType collisionType = CollisionType.Player, bool inclPlayers = true, GameObject context = null, List<TileType> excludeTiles = null)
	{
		// There's not tile here so its passable.
		if (!tilemap.HasTile(to))
			return true;

		var tile = tilemap.GetTile<BasicTile>(to);

		// Return passable if the tile type is being excluded from checks.
		if (excludeTiles != null && excludeTiles.Contains(tile.TileType))
			return true;

		return tile.IsPassable(collisionType);
		//return !tilemap.HasTile(to) || tilemap.GetTile<BasicTile>(to).IsPassable(collisionType);
	}


	public virtual bool IsAtmosPassableAt(Vector3Int from, Vector3Int to, bool isServer)
		{
			return !tilemap.HasTile(to) || tilemap.GetTile<BasicTile>(to).IsAtmosPassable();
		}

	public virtual bool IsSpaceAt(Vector3Int position, bool isServer)
	{
		return !tilemap.HasTile(position) || tilemap.GetTile<BasicTile>(position).IsSpace();
	}

	public virtual void SetTile(Vector3Int position, GenericTile tile, Matrix4x4 transformMatrix, Color color)
	{
		InternalSetTile(position, tile);

		tilemap.SetColor(position, color);
		tilemap.SetTransformMatrix(position, transformMatrix);
		subsystemManager?.UpdateAt(position);
	}

	/// <summary>
	/// Set tile and invoke tile changed event.
	/// </summary>
	protected void InternalSetTile(Vector3Int position, GenericTile tile)
	{
		tilemap.SetTile(position, tile);
		OnTileChanged.Invoke(position, tile);
	}

	public virtual LayerTile GetTile(Vector3Int position)
	{
		return tilemap.GetTile<LayerTile>(position);
	}

	public virtual bool IsDifferent(Vector3Int cellPosition, LayerTile layerTile, Matrix4x4? transformMatrix = null,
		Color? color = null)
	{
		if (tilemap.GetTile<LayerTile>(cellPosition) != layerTile) return true;

		if (tilemap.GetColor(cellPosition) != color.GetValueOrDefault(Color.white)) return true;
		if (tilemap.GetTransformMatrix(cellPosition) != transformMatrix.GetValueOrDefault(Matrix4x4.identity)) return true;
		return false;
	}


	public virtual bool HasTile(Vector3Int position, bool isServer)
	{
		return tilemap.HasTile(position);
	}

	public virtual void RemoveTile(Vector3Int position, bool removeAll = false)
	{
		if (removeAll)
		{
			//TODO: OVERLAYS - This wouldn't work reliably if there is something at level -3 but nothing at -1 and -2.
			position.z = 0;
			while (tilemap.HasTile(position))
			{
				InternalSetTile(position, null);
				position.z--;
			}
		}
		else
		{
			InternalSetTile(position, null);
		}

		position.z = 0;
		subsystemManager.UpdateAt(position);
	}

	public virtual void ClearAllTiles()
	{
		tilemap.ClearAllTiles();
		OnTileChanged.Invoke(TransformState.HiddenPos, null);
	}

#if UNITY_EDITOR
	public void SetPreviewTile(Vector3Int position, LayerTile tile, Matrix4x4 transformMatrix)
	{
		tilemap.SetEditorPreviewTile(position, tile);
		tilemap.SetEditorPreviewTransformMatrix(position, transformMatrix);
	}

	public void ClearPreview()
	{
		tilemap.ClearAllEditorPreviewTiles();
	}
#endif
}
