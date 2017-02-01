using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Matrix {

    [ExecuteInEditMode]
    public class RegisterTile: MonoBehaviour {

        public TileType tileType;
        private TileType currentTileType;

        private int x = -1, y = -1;

        void Start() {
            currentTileType = tileType;

            UpdatePosition();
        }
        
        void OnValidate() {
            if(currentTileType != tileType) {
                UpdateTileType();
            }
        }

        void OnDestroy() {
            if(x >= 0) {
                Matrix.Remove(x, y, tileType);
            }
        }

        public void UpdatePosition() {
            if(x >= 0)
                Matrix.Remove(x, y, currentTileType);

            x = Mathf.RoundToInt(transform.position.x);
            y = Mathf.RoundToInt(transform.position.y);

            Matrix.Add(x, y, currentTileType);
        }

        private void UpdateTileType() {
            if(x >= 0) {
                Matrix.Remove(x, y, currentTileType);
                Matrix.Add(x, y, tileType);
            }

            currentTileType = tileType;
        }
    }
}