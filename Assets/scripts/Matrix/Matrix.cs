using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Matrix {
    
    [Serializable]
    public class Matrix: ScriptableObject {

        public static Matrix matrix;
        public static Matrix Instance {
            get {
                if(!matrix) {
                    LoadMatrix();
                }
                return matrix;
            }
        }

        private static void LoadMatrix() {
			#if UNITY_EDITOR
            matrix = AssetDatabase.LoadAssetAtPath<Matrix>("Assets/Data/Matrix.asset");
			#endif
            if(!matrix) {
                matrix = CreateInstance<Matrix>();
                Directory.CreateDirectory("Assets/Data");
				#if UNITY_EDITOR
                AssetDatabase.CreateAsset(matrix, "Assets/Data/Matrix.asset");
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

        public static MatrixNode At(int x, int y, bool createIfNull = true) {
            if(Instance.map.ContainsKey(x, y)) {
                return Instance.map[x, y];
            }else if(createIfNull) {
                Instance.map[x, y] = new MatrixNode();
                return Instance.map[x, y];
            }

            return null;
        }

        //This is for the InputRelease method on physics move (to snap player to grid)
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