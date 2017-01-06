using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Events;
using UnityEngine.Events;


namespace Matrix {

    public enum TileType {
        Space, Floor, Wall, Window, Table, Door
    }

    [ExecuteInEditMode]
    public class Matrix: MonoBehaviour {

        public Vector3 offset;

        private MatrixNode[,] map = new MatrixNode[2000, 2000];

        private static Matrix wallMap;

        public static Matrix Instance {
            get {
                if(!wallMap) {
                    wallMap = FindObjectOfType<Matrix>();
                }

                return wallMap;
            }
        }

        public static void Add(int x, int y, TileType tileType) {
            if(Instance.map[y, x] == null) {
                Instance.map[y, x] = new MatrixNode();
            }
            Instance.map[y, x].AddTileType(tileType);
        }

        public static void Remove(int x, int y, TileType tileType) {
            if(Instance && Instance.map[y, x] != null) {
                Instance.map[y, x].RemoveTileType(tileType);
            }
        }

        public static TileType GetTypeAt(int x, int y) {
            if(Instance.map[y, x] == null) {
                return TileType.Space;
            } else {
                return Instance.map[y, x].Type;
            }
        }

        public static bool IsPassableAt(int x, int y) {
            return GetTypeAt(x, y) < TileType.Wall;
        }

        public static void AddListener(int x, int y, UnityAction<TileType> listener) {
            if(Instance.map[y, x] == null) {
                Instance.map[y, x] = new MatrixNode();
            }
            Instance.map[y, x].AddListener(listener);
        }

        public static void RemoveListener(int x, int y, UnityAction<TileType> listener) {
            if(Instance.map[y, x] != null) {
                Instance.map[y, x].RemoveListener(listener);
            }
        }
    }
}