using UnityEngine;
using System.Collections;


namespace Game {

    public class GameManager: MonoBehaviour {

        public int gridSizeX = 100;
        public int gridSizeY = 100;

        public GameObject playerCamera;
        public float panSpeed = 10.0f;

        public enum Direction { Up, Left, Down, Right };
        public enum TileType { Space, Floor, Wall };
        public enum ItemTile { Tile, Item };

        private GameObject[,] grid;
        private TextAsset map;
        private bool mapLoaded = false;

        private int[] currentGrid;
        
        void Start() {
        }
        
        void Update() {
        }

        private bool HasGridLoaded() {
            bool gridLoaded = true;
            int count = 0;
            foreach(GameObject gridObj in grid) {
                count++;
                if(!gridObj.activeSelf) {
                    gridLoaded = false;
                    break;
                }
            }
            return gridLoaded;
        }

        // Obsolete as objects are 1 x 1 via transform.position (if not then they should be normalized to find grid pos)
        public Vector3 GetGridCoords(int gridX, int gridY) {
            return grid[gridX, gridY].transform.position;
        }

        public Vector3 GetClosestNode(Vector2 curPos, Vector2 vel) {

            float closestX;
            float closestY;

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

            return new Vector3(closestX, closestY, 0f);
        }
    }
}