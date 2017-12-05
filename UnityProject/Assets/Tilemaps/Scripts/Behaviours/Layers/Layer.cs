using Tilemaps.Scripts.Tiles;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tilemaps.Scripts.Behaviours.Layers
{
    [ExecuteInEditMode]
    public class Layer : MonoBehaviour
    {
        public LayerType LayerType;
        protected Tilemap tilemap;

        public void Awake()
        {
            tilemap = GetComponent<Tilemap>();
        }

        public virtual bool IsPassableAt(Vector3Int from, Vector3Int to)
        {
            var tileTo = tilemap.GetTile<BasicTile>(to);
            return !tileTo || tileTo.IsPassable();
        }

        public virtual bool IsPassableAt(Vector3Int position)
        {
            var tile = tilemap.GetTile<BasicTile>(position);
            return !tile || tile.IsPassable();
        }

        public virtual bool IsAtmosPassableAt(Vector3Int position)
        {
            var tile = tilemap.GetTile<BasicTile>(position);
            return !tile || tile.IsAtmosPassable();
        }

        public virtual bool IsSpaceAt(Vector3Int position)
        {
            var tile = tilemap.GetTile<BasicTile>(position);
            return !tile || tile.IsSpace();
        }

        public virtual void SetTile(Vector3Int position, GenericTile tile, Matrix4x4 transformMatrix)
        {
            tilemap.SetTile(position, tile);
            tilemap.SetTransformMatrix(position, transformMatrix);
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

        public virtual void ClearAllTiles()
        {
            tilemap.ClearAllTiles();
        }
    }
}