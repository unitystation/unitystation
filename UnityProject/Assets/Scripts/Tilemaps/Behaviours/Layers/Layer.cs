using UnityEngine;
using UnityEngine.Tilemaps;


	[ExecuteInEditMode]
	public class Layer : MonoBehaviour
	{
		private SystemManager systemManager;

		public LayerType LayerType;
		protected Tilemap tilemap;

		public BoundsInt Bounds => tilemap.cellBounds;

		public void Awake()
		{
			tilemap = GetComponent<Tilemap>();
			systemManager = GetComponentInParent<SystemManager>();
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
		}

		public virtual bool IsPassableAt( Vector3Int from, Vector3Int to, bool inclPlayers = true, GameObject context = null )
		{
			return TileUtils.IsPassable(tilemap.GetTile<BasicTile>(to));
		}

		public virtual bool IsAtmosPassableAt(Vector3Int from, Vector3Int to)
		{
			return TileUtils.IsAtmosPassable(tilemap.GetTile<BasicTile>(to));
		}

		public virtual bool IsSpaceAt(Vector3Int position)
		{
			return TileUtils.IsSpace(tilemap.GetTile<BasicTile>(position));
		}

		public virtual void SetTile(Vector3Int position, GenericTile tile, Matrix4x4 transformMatrix)
		{
			tilemap.SetTile(position, tile);
			tilemap.SetTransformMatrix(position, transformMatrix);
			systemManager.UpdateAt(position);
		}

		public virtual LayerTile GetTile(Vector3Int position)
		{
			return tilemap.GetTile<LayerTile>(position);
		}

		public virtual bool HasTile(Vector3Int position)
		{
			return GetTile(position);
		}

		public virtual void RemoveTile(Vector3Int position)
		{
			tilemap.SetTile(position, null);
			systemManager.UpdateAt(position);
		}

		public virtual void ClearAllTiles()
		{
			tilemap.ClearAllTiles();
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
