using System.Collections;
using Atmospherics;
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
		private MetaDataLayer metaDataLayer;

		public LayerType LayerType;
		protected Tilemap tilemap;
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

		public Vector3Int WorldToCell(Vector3 pos) => tilemap.WorldToCell(pos);
		public Vector3Int LocalToCell(Vector3 pos) => tilemap.LocalToCell(pos);
		public Vector3 LocalToWorld( Vector3 localPos ) => tilemap.LocalToWorld( localPos );
		public Vector3 CellToWorld( Vector3Int cellPos ) => tilemap.CellToWorld( cellPos );
		public Vector3 WorldToLocal( Vector3 worldPos ) => tilemap.WorldToLocal( worldPos );

		public void Awake()
		{
			tilemap = GetComponent<Tilemap>();
			TilemapDamage = GetComponent<TilemapDamage>();
			subsystemManager = GetComponentInParent<SubsystemManager>();
			metaDataLayer = GetComponentInParent<MetaDataLayer>();
			RecalculateBounds();


			OnTileChanged.AddListener( (pos, tile) => TryRecalculateBounds() );
			void TryRecalculateBounds()
			{
				if ( recalculateBoundsHandle == null )
				{
					this.RestartCoroutine( RecalculateBoundsNextFrame(), ref recalculateBoundsHandle );
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
			yield return WaitFor.Seconds( 0.015f );
			RecalculateBounds();
		}

		public void RecalculateBounds()
		{
			boundsCache = tilemap.cellBounds;
			this.TryStopCoroutine( ref recalculateBoundsHandle );
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

		public virtual bool IsPassableAt( Vector3Int from, Vector3Int to, bool isServer,
			CollisionType collisionType = CollisionType.Player, bool inclPlayers = true, GameObject context = null )
		{
			return !tilemap.HasTile(to) || tilemap.GetTile<BasicTile>(to).IsPassable(collisionType);
		}

		public virtual bool IsAtmosPassableAt(Vector3Int from, Vector3Int to, bool isServer)
		{
			return !tilemap.HasTile(to) || tilemap.GetTile<BasicTile>(to).IsAtmosPassable();
		}

		public virtual bool IsSpaceAt(Vector3Int position, bool isServer)
		{
			return !tilemap.HasTile(position) || tilemap.GetTile<BasicTile>(position).IsSpace();
		}

		public virtual void SetTile(Vector3Int position, GenericTile tile, Matrix4x4 transformMatrix)
		{
			InternalSetTile( position, tile );
			tilemap.SetTransformMatrix(position, transformMatrix);
			subsystemManager.UpdateAt(position);
		}

		/// <summary>
		/// Set tile and invoke tile changed event.
		/// </summary>
		protected void InternalSetTile( Vector3Int position, GenericTile tile )
		{
			tilemap.SetTile( position, tile );
			OnTileChanged.Invoke( position, tile );
		}

		public virtual LayerTile GetTile(Vector3Int position)
		{
			return tilemap.GetTile<LayerTile>(position);
		}

		public virtual bool HasTile(Vector3Int position, bool isServer)
		{
			return tilemap.HasTile( position );
		}

		public virtual void RemoveTile(Vector3Int position, bool removeAll=false)
		{
			//if we removed an impassable tile, we need to prevent
			//a sudden gust of wind due to pressure difference, so we'll set its
			//gasmix to the average of its neighbors. This is fine to do even if there are multiple impassable tiles
			//on different z levels in this layer since it won't have any effect until
			//the last impassable tile is removed. It would be more expensive to
			//check if it's the last impassable tile anyway.
			bool removedImpassable = false;
			if (removeAll)
			{
				position.z = 0;

				while (tilemap.HasTile(position))
				{
					if (!tilemap.GetTile<BasicTile>(position).IsAtmosPassable())
					{
						removedImpassable = true;
					}
					InternalSetTile(position, null);

					position.z--;
				}
			}
			else
			{
				if (!tilemap.GetTile<BasicTile>(position).IsAtmosPassable())
				{
					removedImpassable = true;
				}
				InternalSetTile(position, null);
			}

			position.z = 0;

			//if we removed an impassable tile, set its pressure to the
			//average of its atmos passable neighbors.
			//Logic here is slightly different from AtmosSimulation.Equalize
			//since it only updates this node's gas mix
			if (removedImpassable)
			{
				var metaDataNode = metaDataLayer.Get(position);
				var meanGasMix = new GasMix(GasMixes.Space);
				int neighborsCounted = 0;
				foreach (var neighbor in metaDataNode.Neighbors)
				{
					if (neighbor == null || neighbor.IsOccupied) continue;
					neighborsCounted++;
					for (int j = 0; j < Gas.Count; j++)
					{
						meanGasMix.Gases[j] += neighbor.GasMix.Gases[j];
					}

					meanGasMix.Pressure += neighbor.GasMix.Pressure;
				}
				if (neighborsCounted != 0)
				{
					for (int j = 0; j < Gas.Count; j++)
					{
						meanGasMix.Gases[j] /= neighborsCounted;
					}

					meanGasMix.Pressure /= neighborsCounted;
				}

				metaDataNode.GasMix = meanGasMix;
			}

			subsystemManager.UpdateAt(position);
		}

		public virtual void ClearAllTiles()
		{
			tilemap.ClearAllTiles();
			OnTileChanged.Invoke( TransformState.HiddenPos, null );
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
