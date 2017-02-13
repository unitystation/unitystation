using UnityEngine;

namespace Matrix {
    
    public class Matrix {

        private static Matrix Instance = new Matrix();

        private Matrix() { }

        private MatrixNode[,] map = new MatrixNode[2500, 2500];

        public static MatrixNode At(int x, int y, bool createIfNull=true) {
            if(createIfNull && Instance.map[y, x] == null) {
                Instance.map[y, x] = new MatrixNode();
            }
            return Instance.map[y, x];
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