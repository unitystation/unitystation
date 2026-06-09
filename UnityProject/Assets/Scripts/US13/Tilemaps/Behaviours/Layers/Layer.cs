using System;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using US13.Core.Initialisation;
using US13.Core.Lifecycle;
using US13.Core.Lighting;
using US13.Core.Transform;
using US13.Managers.MatrixManager;
using US13.Managers.NetworkManagement;
using US13.Shuttles;
using US13.Tilemaps.Behaviours.Meta;
using US13.Tilemaps.Tiles;
using US13.Tilemaps.Utils;
using Util;

namespace US13.Tilemaps.Behaviours.Layers
{
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

		/// <summary>
		/// Current offset from our initially mapped orientation. This is used by tiles within the tilemap
		/// to determine what sprite to display. This could be retrieved directly from MatrixMove but
		/// it's faster to cache it here and update when rotation happens.
		/// </summary>
		public Quaternion RotationOffset { get; private set; }

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

		[NonSerialized] public MetaTileMap metaTileMap;

		public void Awake()
		{
			Matrix = GetComponentInParent<Matrix>();
			tilemap = GetComponent<Tilemap>();
			TilemapDamage = GetComponent<TilemapDamage>();
			SubsystemManager = GetComponentInParent<MatrixSystemManager>();
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
				Loggy.Error("Matrix Manager is missing from the scene", Category.Matrix);
			}

			InitFromMatrix();
		}

		public void InitFromMatrix()
		{
			RotationOffset = Quaternion.identity;

			if (this == null) return;
			matrixMove = transform?.root?.GetComponent<MatrixMove>();


			if (matrixMove != null)
			{
				Loggy.Trace().Format("{0} layer initializing from matrix", Category.Matrix, matrixMove);
				matrixMove.NetworkedMatrixMove.OnRotate90 += (OnRotate90);
				//initialize from current rotation
				OnRotate90(matrixMove.NetworkedMatrixMove.previousDirectionFacing);
			}
		}

		private void OnDestroy()
		{
			if (matrixMove.OrNull()?.NetworkedMatrixMove.OrNull() != null)
			{
				matrixMove.NetworkedMatrixMove.OnRotate90 -= (OnRotate90);
			}

		}

		private void OnRotate90(OrientationEnum OrientationEnum)
		{
			return;
			//TODO look in to why tables don't Rotate for properly, Otherwise this doesn't seem to do anything and costs performance
			// if (CustomNetworkManager.IsHeadless) return;
			// RotationOffset = matrixMove.NetworkedMatrixMove.TargetTransform.rotation;
			// if (tilemap != null)
			// {
			// 	tilemap.RefreshAllTiles();
			// }
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
			if (c.CanBeHighlightedThroughScanners == false || c.HighlightObject == null) return;

			HighlightScanManager.Instance.UninitialisedHighlightScans.Add(
				new HighlightScanManager.UninitialisedHighlightScan()
				{
					LocalPOS = position,
					Layer = this,
					Tile = (SimpleTile)tile
				});

		}

		public void AddHighlight(HighlightScanManager.UninitialisedHighlightScan Entry)
		{
			var spawnHighlight = Spawn.ClientPrefab(Entry.Tile.HighlightObject, MatrixManager.LocalToWorld(Entry.LocalPOS, Matrix),
				this.transform); //Spawn highlight object ontop of tile
			if (spawnHighlight.Successful == false ||
			    spawnHighlight.GameObject.TryGetComponent<HighlightScan>(out var scan) == false)
				return; //If this fails for whatever reason, return

			scan.Tile = Entry.Tile;
			scan.LocalPOS = Entry.LocalPOS;
			scan.Layer = Entry.Layer;

			scan.Setup(Entry.Tile.sprite); //setup the highlight sprite rendere
		}

		public bool RemoveTile(Vector3Int position)
		{
			var tileRemoved = tilemap.GetTile(position);
			tilemap.SetTile(position, null);
			if (tileRemoved is SimpleTile c)
			{
				if (c.CanBeHighlightedThroughScanners && c.HighlightObject)
				{
					HighlightScanManager.RemoveHighlight(position, c, this);
				}
			}

			onTileMapChanges.Invoke();
			return tileRemoved;
		}

		/// <summary>
		/// Set tile and invoke tile changed event.
		/// </summary>
		protected void InternalSetTile(Vector3Int position, GenericTile tile)
		{
			tilemap.SetTile(position, tile);
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
}