using System.Collections.Generic;
using System.Linq;
using Tilemaps.Scripts.Tiles;
using UnityEngine;

namespace Tilemaps.Scripts.Behaviours.Layers
{
    [ExecuteInEditMode]
    public class MetaTileMap : MonoBehaviour
    {
        private Dictionary<LayerType, Layer> layers;

        private void OnEnable()
        {
            layers = new Dictionary<LayerType, Layer>();

            foreach (var layer in GetComponentsInChildren<Layer>(true))
            {
                layers[layer.LayerType] = layer;
            }
        }

        public bool IsPassableAt(Vector3Int from, Vector3Int to)
        {
            foreach (var layer in layers.Values)
            {
                if (!layer.IsPassableAt(from, to))
                    return false;
            }
            return true;
        }

        public bool IsPassableAt(Vector3Int position)
        {
            foreach (var layer in layers.Values)
            {
                if (!layer.IsPassableAt(position))
                    return false;
            }
            return true;
        }

        public bool IsAtmosPassableAt(Vector3Int position)
        {
            foreach (var layer in layers.Values)
            {
                if (!layer.IsAtmosPassableAt(position))
                    return false;
            }
            return true;
        }

        public bool IsSpaceAt(Vector3Int position)
        {
            foreach (var layer in layers.Values)
            {
                if (!layer.IsSpaceAt(position))
                    return false;
            }
            return true;
        }

        public void SetTile(Vector3Int position, LayerTile tile, Matrix4x4 transformMatrix)
        {
            layers[tile.LayerType].SetTile(position, tile, transformMatrix);
        }

        public void SetPreviewTile(Vector3Int position, LayerTile tile, Matrix4x4 transformMatrix)
        {
            foreach (var layer in layers.Values)
            {
                if (layer.LayerType < tile.LayerType)
                {
                    layers[layer.LayerType].SetPreviewTile(position, LayerTile.EmptyTile, Matrix4x4.identity);
                }
            }
            
            layers[tile.LayerType].SetPreviewTile(position, tile, transformMatrix);
        }

        public void ClearPreview()
        {
            foreach (var layer in layers.Values)
            {
                layer.ClearPreview();
            }
        }

        public void RemoveTile(Vector3Int position, LayerType refLayer)
        {
            foreach (var layer in layers.Values)
            {
                if (layer.LayerType < refLayer && !(refLayer == LayerType.Objects && layer.LayerType == LayerType.Floors))
                {
                    layer.RemoveTile(position);
                }
            }
        }
    }
}