using Matrix;
using System;
using System.Collections.Generic;
using UI;
using UnityEditor;
using UnityEngine;

namespace MapEditor {

    public class MapEditorControl {
        public static Dictionary<TileType, int> TileTypeLevels = new Dictionary<TileType, int>() {
        { TileType.Space, 0 },
        { TileType.Floor, 1 },
        { TileType.Table, 1 },
        { TileType.Wall, 2 },
        { TileType.Window, 2 },
        { TileType.Door, 2 }};

        private static SceneView currentSceneView;

        public static int HashCode { get; set; }        

        static MapEditorControl() {
            PreviewObject.ShowPreview = true;
        }

        public static bool Build(Event e) {
            if(!PreviewObject.Prefab)
                return false;

            Ray r = Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x, -e.mousePosition.y + Camera.current.pixelHeight));

            int x = Mathf.RoundToInt(r.origin.x);
            int y = Mathf.RoundToInt(r.origin.y);

            var registerTile = PreviewObject.Prefab.GetComponent<RegisterTile>();
            if(registerTile) { // it's something constructable
                if(!Matrix.Matrix.HasTypeAt(x, y, registerTile.tileType) && TileTypeLevels[registerTile.tileType] >= TileTypeLevels[Matrix.Matrix.GetTypeAt(x, y)]) {

                    CreateGameObject(r.origin);

                    return true;
                }
            } else {
                var itemAttributes = PreviewObject.Prefab.GetComponent<ItemAttributes>();
                if(itemAttributes) { // it's an item
                    // TODO
                    return true;
                }
            }
            return false;
        }

        public static void CreateGameObject(Vector3 position) {
            var gameObject = PreviewObject.CreateGameObject();

            gameObject.transform.position = position;
            gameObject.transform.parent = MapEditorMap.CurrentSubSection;

            Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
        }
    }
}
