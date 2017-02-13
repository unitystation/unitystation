using Matrix;
using System;
using System.Collections.Generic;
using UI;
using UnityEditor;
using UnityEngine;

namespace MapEditor {

    public class BuildControl {
        private static SceneView currentSceneView;

        public static int HashCode { get; set; }
        public static bool CheckTileFit { get; set; }

        public static bool Build(Event e) {
            if(!PreviewObject.Prefab)
                return false;

            Ray r = Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x, -e.mousePosition.y + Camera.current.pixelHeight));

            int x = Mathf.RoundToInt(r.origin.x);
            int y = Mathf.RoundToInt(r.origin.y);

            var registerTile = PreviewObject.Prefab.GetComponent<RegisterTile>();
            if(registerTile) { // it's something constructable
                if(!CheckTileFit || Matrix.Matrix.At(x, y).FitsTile(PreviewObject.Prefab)) {

                    CreateGameObject(r.origin);

                    return true;
                }
            } else {

                CreateGameObject(r.origin);

                return true;
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
