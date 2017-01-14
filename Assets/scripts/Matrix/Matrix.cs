using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Events;
using UnityEngine.Events;


namespace Matrix {

    public enum TileType {
        Space, Floor, Table, Wall, Window, Door
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

        public static bool HasTypeAt(int x, int y, TileType tileType) {
            return Instance.map[y, x].HasTileType(tileType);
        }

        public static bool IsPassableAt(int x, int y) {
            return GetTypeAt(x, y) <= TileType.Floor;
        }

        public static void AddListener(int x, int y, UnityAction<TileType> listener) {
            if(Instance.map[y, x] == null) {
                Instance.map[y, x] = new MatrixNode();
            }
            Instance.map[y, x].AddListener(listener);
        }

        public static void RemoveListener(int x, int y, UnityAction<TileType> listener) {
            if(Instance && Instance.map[y, x] != null) {
                Instance.map[y, x].RemoveListener(listener);
            }
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
            if (!IsPassableAt((int)closestX, (int)closestY))
            {
                closestX = Mathf.Round(curPos.x);
                closestY = Mathf.Round(curPos.y);
            }

            return new Vector3(closestX, closestY, 0f);
        }
    }
}