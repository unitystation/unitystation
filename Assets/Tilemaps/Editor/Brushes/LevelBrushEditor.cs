using System.Linq;
using Tilemaps.Editor.Brushes.Utils;
using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Tiles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tilemaps.Editor.Brushes
{
    [CustomEditor(typeof(LevelBrush))]
    public class LevelBrushEditor : GridBrushEditor
    {
        private MetaTileMap _currentPreviewTilemap;
        private TileBase _currentPreviewTile;

        private PreviewTile previewTile; // Preview Wrapper for ObjectTiles

        public override GameObject[] validTargets
        {
            get
            {
                Grid[] grids = FindObjectsOfType<Grid>();

                return grids?.Select(x => x.gameObject).ToArray();
            }
        }
        
        public override void RegisterUndo(GameObject layer, GridBrushBase.Tool tool)
        {
            foreach (var tilemap in layer.GetComponentsInChildren<Tilemap>())
            {
                Undo.RegisterCompleteObjectUndo(tilemap, "Paint");
            }
        }

        public override void PaintPreview(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            if (brushTarget == null)
                return;
            
            var metaTilemap = brushTarget.GetComponent<MetaTileMap>();

            if (!metaTilemap)
                return;
            
            var tile = brush.cells[0].tile;
            
            if (tile is LayerTile)
            {
                if (tile != _currentPreviewTile)
                {
                    var layerTile = tile as LayerTile;

                    var objectTile = tile as ObjectTile;
                    if (objectTile && objectTile.Offset)
                    {
                        brush.cells[0].matrix = Matrix4x4.TRS(Vector3.up, Quaternion.identity, Vector3.one);
                    }
                    
                    _currentPreviewTile = tile;
                }
                
                SetPreviewTile(metaTilemap, position, (LayerTile) tile);
            }
            else if (tile is MetaTile)
            {
                foreach (var layerTile in ((MetaTile)tile).GetTiles())
                {
                    SetPreviewTile(metaTilemap, position, layerTile);
                }
            }
            
            _currentPreviewTilemap = metaTilemap;
        }

        public override void ClearPreview()
        {
            if (_currentPreviewTilemap)
            {
                _currentPreviewTilemap.ClearPreview();
            }
        }

        private void SetPreviewTile(MetaTileMap metaTilemap, Vector3Int position, LayerTile tile)
        {
            if (tile is ObjectTile)
            {
                if (previewTile == null)
                {
                    previewTile = CreateInstance<PreviewTile>();
                }

                previewTile.ReferenceTile = tile;
                previewTile.LayerType = LayerType.Structures;
                tile = previewTile;
                
                position.z++; // to draw the object over already existing stuff
            }
            
            metaTilemap.SetPreviewTile(position, tile, brush.cells[0].matrix);
        }
    }
}