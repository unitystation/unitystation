using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Matrix {

    [Serializable]
    public class Matrix: ScriptableObject {
        private static string activeSceneName;

        private static Matrix matrix;
        public static Matrix Instance {
            get {
                Scene scene = SceneManager.GetActiveScene();
                if(!matrix || !activeSceneName.Equals(scene.name)) {
                    LoadMatrix();
                }
                return matrix;
            }
        }

        private static void LoadMatrix() {
            Scene scene = SceneManager.GetActiveScene();
            activeSceneName = scene.name;
#if UNITY_EDITOR
            string assetPath = "Assets/Data/" + activeSceneName + "_Matrix.asset";
            matrix = AssetDatabase.LoadAssetAtPath<Matrix>(assetPath);
#endif

            if(!matrix) {
                matrix = CreateInstance<Matrix>();
#if UNITY_EDITOR
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                AssetDatabase.CreateAsset(matrix, assetPath);
#endif
            }
        }

        private Matrix() { }

        [SerializeField]
        private NodeDictionary map;

        public void OnEnable() {
            if(map == null) {
                map = new NodeDictionary();
            }
        }

        public static MatrixNode At(Vector2 position, bool createIfNull = true) {
            return At(position.x, position.y);
        }

        public static MatrixNode At(float x, float y, bool createIfNull = true) {
            return At(Mathf.RoundToInt(x), Mathf.RoundToInt(y), createIfNull);
        }

        public static MatrixNode At(int x, int y, bool createIfNull = true) {
            if(Instance.map.ContainsKey(x, y)) {
                return Instance.map[x, y];
            } else if(createIfNull) {
                Instance.map[x, y] = new MatrixNode();
                return Instance.map[x, y];
            }

            return null;
        }

        public static NodeDictionary Nodes { get { return Instance.map; } }

        public static Vector3 GetClosestNode(Vector2 curPos, Vector2 vel) {

            float closestX;
            float closestY;

            //determine direction heading via velocity
            if(vel.x > 0.1f) {
                closestX = Mathf.Ceil(curPos.x);
                closestY = Mathf.Round(curPos.y);
            } else if(vel.x < -0.1f) {
                closestX = Mathf.Floor(curPos.x);
                closestY = Mathf.Round(curPos.y);
            } else if(vel.y > 0.1f) {
                closestY = Mathf.Ceil(curPos.y);
                closestX = Mathf.Round(curPos.x);

            } else if(vel.y < -0.1f) {
                closestY = Mathf.Floor(curPos.y);
                closestX = Mathf.Round(curPos.x);
            } else {
                closestX = Mathf.Round(curPos.x);
                closestY = Mathf.Round(curPos.y);
            }
            // If target is not passable then target cur tile
            if(!At((int) closestX, (int) closestY).IsPassable()) {
                closestX = Mathf.Round(curPos.x);
                closestY = Mathf.Round(curPos.y);
            }

            return new Vector3(closestX, closestY, 0f);
        }
    }
}