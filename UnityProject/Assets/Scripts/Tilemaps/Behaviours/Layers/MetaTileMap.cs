using System.Collections.Generic;
using Tilemaps.Scripts.Tiles;
using UnityEngine;

namespace Tilemaps.Scripts.Behaviours.Layers
{
	[ExecuteInEditMode]
	public class MetaTileMap : MonoBehaviour
	{
		public Dictionary<LayerType, Layer> Layers { get; private set; }

		private void OnEnable()
		{
			Layers = new Dictionary<LayerType, Layer>();

			foreach (Layer layer in GetComponentsInChildren<Layer>(true))
			{
				Layers[layer.LayerType] = layer;
			}
		}


		public bool IsPassableAt(Vector3Int origin, Vector3Int to)
		{
			foreach (Layer layer in Layers.Values)
			{
				if (!layer.IsPassableAt(origin, to))
				{
					return false;
				}
			}
			return true;
		}

		public bool IsPassableAt(Vector3Int position)
		{
			foreach (Layer layer in Layers.Values)
			{
				if (!layer.IsPassableAt(position))
				{
					return false;
				}
			}
			return true;
		}

		public bool IsAtmosPassableAt(Vector3Int position)
		{
			foreach (Layer layer in Layers.Values)
			{
				if (!layer.IsAtmosPassableAt(position))
				{
					return false;
				}
			}
			return true;
		}

		public bool IsSpaceAt(Vector3Int position)
		{
			foreach (Layer layer in Layers.Values)
			{
				if (!layer.IsSpaceAt(position))
				{
					return false;
				}
			}
			return true;
		}

		public void SetTile(Vector3Int position, LayerTile tile, Matrix4x4 transformMatrix)
		{
			Layers[tile.LayerType].SetTile(position, tile, transformMatrix);
		}

		public LayerTile GetTile(Vector3Int position, LayerType layerType)
		{
			Layer layer = null;
			Layers.TryGetValue(layerType, out layer);
			if (layer)
			{
				return Layers[layerType].GetTile(position);
			}
			return null;
		}

		public LayerTile GetTile(Vector3Int position)
		{
			foreach (Layer layer in Layers.Values)
			{
				LayerTile tile = layer.GetTile(position);
				if (tile != null)
				{
					return tile;
				}
			}

			return null;
		}

		public bool IsEmptyAt(Vector3Int position)
		{
			foreach (LayerType layer in Layers.Keys)
			{
				if (layer != LayerType.Objects && HasTile(position, layer))
				{
					return false;
				}
			}
			return true;
		}

		public bool HasTile(Vector3Int position, LayerType layerType)
		{
			return Layers[layerType].HasTile(position);
		}

		public void RemoveTile(Vector3Int position, LayerType refLayer)
		{
			foreach (Layer layer in Layers.Values)
			{
				if (layer.LayerType < refLayer &&
				    !(refLayer == LayerType.Objects && layer.LayerType == LayerType.Floors))
				{
					layer.RemoveTile(position);
				}
			}
		}

		public void ClearAllTiles()
		{
			foreach (Layer layer in Layers.Values)
			{
				layer.ClearAllTiles();
			}
		}
		

#if UNITY_EDITOR
		public void SetPreviewTile(Vector3Int position, LayerTile tile, Matrix4x4 transformMatrix)
		{
			foreach (Layer layer in Layers.Values)
			{
				if (layer.LayerType < tile.LayerType)
				{
					Layers[layer.LayerType].SetPreviewTile(position, LayerTile.EmptyTile, Matrix4x4.identity);
				}
			}

			Layers[tile.LayerType].SetPreviewTile(position, tile, transformMatrix);
		}

		public void ClearPreview()
		{
			foreach (Layer layer in Layers.Values)
			{
				layer.ClearPreview();
			}
		}
#endif
	}
}