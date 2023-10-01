using System;
using System.Collections;
using System.Collections.Generic;
using Core.Lighting;
using Initialisation;
using Logs;
using TileManagement;
using Tiles;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;


[ExecuteInEditMode]
public class Layer : MonoBehaviour
{
	public MatrixSystemManager SubsystemManager { get; private set; }

	[HideInInspector] public UnityEvent onTileMapChanges;

	public LayerType LayerType;
	protected Tilemap tilemap;

	public Tilemap Tilemap
	{
		get
		{
			if (tilemap == null)
			{
				tilemap = GetComponent<Tilemap>();
			}

			return tilemap;
		}
	}

	public TilemapDamage TilemapDamage { get; private set; }

	public BoundsInt Bounds => boundsCache;
	private BoundsInt boundsCache;

	private Coroutine recalculateBoundsHandle;

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

	public Matrix Matrix { get; private set; }

	public Vector3Int WorldToCell(Vector3 pos) => tilemap.WorldToCell(pos);
	public Vector3Int LocalToCell(Vector3 pos) => tilemap.LocalToCell(pos);
	public Vector3 LocalToWorld(Vector3 localPos) => tilemap.LocalToWorld(localPos);
	public Vector3 CellToWorld(Vector3Int cellPos) => tilemap.CellToWorld(cellPos);
	public Vector3 WorldToLocal(Vector3 worldPos) => tilemap.WorldToLocal(worldPos);

	//Used to make sure two overlays dont conflict before being set, cleared on the update
	public HashSet<Vector3> overlayStore = new HashSet<Vector3>();

	[NonSerialized]
	public MetaTileMap metaTileMap;

	public void Awake()
	{
		Matrix = GetComponentInParent<Matrix>();
		tilemap = GetComponent<Tilemap>();
		TilemapDamage = GetComponent<TilemapDamage>();
		SubsystemManager = GetComponentInParent<MatrixSystemManager>();
		RecalculateBounds();
	}

	/// <summary>
	/// In case there are lots of sudden changes, recalculate bounds once a frame
	/// instead of doing it for every changed tile
	/// </summary>
	private IEnumerator RecalculateBoundsNextFrame()
	{
		//apparently waiting for next frame doesn't work when looking at Scene view! //TODO use editor routines for this functionality
		yield return WaitFor.Seconds(0.015f);
		RecalculateBounds();
	}

	public virtual void RecalculateBounds()
	{
		boundsCache = tilemap.cellBounds;
		this.TryStopCoroutine(ref recalculateBoundsHandle);
	}

	private void Start()
	{
		LoadManager.RegisterAction(Init);
	}

	void Init()
	{
		if (!Application.isPlaying)
		{
			return;
		}

		if (MatrixManager.Instance == null)
		{
			Loggy.LogError("Matrix Manager is missing from the scene", Category.Matrix);
		}

		InitFromMatrix();
	}

	public void InitFromMatrix()
	{
		RotationOffset = RotationOffset.Same;

		if (this == null) return;
		matrixMove = transform?.root?.GetComponent<MatrixMove>();


		if (matrixMove != null)
		{
			Loggy.LogTraceFormat("{0} layer initializing from matrix", Category.Matrix, matrixMove);
			matrixMove.MatrixMoveEvents.OnRotate.AddListener(OnRotate);
			//initialize from current rotation
			OnRotate(MatrixRotationInfo.FromInitialRotation(matrixMove, NetworkSide.Client, RotationEvent.Register));
		}
	}

	private void OnDestroy()
	{
		{
			matrixMove?.MatrixMoveEvents?.OnRotate?.RemoveListener(OnRotate);
		}
	}
	private void OnRotate(MatrixRotationInfo info)
	{
		if (info.IsEnding || info.IsObjectBeingRegistered)
		{
			RotationOffset = info.RotationOffsetFromInitial;
			Loggy.LogTraceFormat("{0} layer redrawing with offset {1}", Category.Matrix, info.MatrixMove,
				RotationOffset);
			if (tilemap != null)
			{
				tilemap.RefreshAllTiles();
			}
		}
	}

	public virtual void SetTile(Vector3Int position, GenericTile tile, Matrix4x4 transformMatrix, Color color)
	{
		InternalSetTile(position, tile);
		tilemap.SetColor(position, color);
		tilemap.SetTransformMatrix(position, transformMatrix);

		onTileMapChanges.Invoke();



		//Client stuff, never spawn this on the server. (IsServer is technically a client in some cases so only return this on headless)
		if (CustomNetworkManager.IsHeadless) return;
		if (tile is not SimpleTile c) return; //Not a tile that has the data we need
		if(c.CanBeHighlightedThroughScanners == false || c.HighlightObject == null) return;
		var spawnHighlight = Spawn.ClientPrefab(c.HighlightObject, MatrixManager.LocalToWorld(position, Matrix), this.transform); //Spawn highlight object ontop of tile
		if(spawnHighlight.Successful == false || spawnHighlight.GameObject.TryGetComponent<HighlightScan>(out var scan) == false) return; //If this fails for whatever reason, return
		c.AssoicatedSpawnedObjects.Add(spawnHighlight.GameObject); //Add it to a list that the tile will keep track off for when OnDestroy() happens
		scan.Setup(c.sprite); //setup the highlight sprite rendere
	}

	public bool RemoveTile(Vector3Int position)
	{
		var tileRemoved = false;
		tileRemoved = InternalSetTile(position, null);
		onTileMapChanges.Invoke();
		return tileRemoved;
	}

	/// <summary>
	/// Set tile and invoke tile changed event.
	/// </summary>
	protected bool InternalSetTile(Vector3Int position, GenericTile tile)
	{
		var hasTile = tilemap.HasTile(position);
		tilemap.SetTile(position, tile);
		if (recalculateBoundsHandle == null)
		{
			this.RestartCoroutine(RecalculateBoundsNextFrame(), ref recalculateBoundsHandle);
		}
		return hasTile;
	}

	public LayerTile GetTile(Vector3Int position)
	{
		return tilemap.GetTile<LayerTile>(position);
	}

	public bool HasTile(Vector3Int position)
	{
		if (tilemap == null) return false;
		return tilemap.HasTile(position);
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
