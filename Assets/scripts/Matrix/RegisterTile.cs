using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Matrix {

    [ExecuteInEditMode]
    public class RegisterTile: MonoBehaviour {

        public TileType tileType;
        private TileType currentTileType;

        private Vector3 currentPosition;

        private int x = -1, y = -1;

        void Start() {
            currentTileType = tileType;
            currentPosition = transform.position;

            UpdatePosition();
        }

        void Update() {
            if(currentPosition != transform.position) {
                currentPosition = transform.position;
                UpdatePosition();
            }
            
            if(currentTileType != tileType) {
                UpdateTileType();
            }
        }

        void OnDestroy() {
            if(x >= 0) {
                Matrix.Remove(x, y, tileType);
            }
        }

        private void UpdatePosition() {
            if(x >= 0)
                Matrix.Remove(x, y, currentTileType);

            x = (int) currentPosition.x;
            y = (int) currentPosition.y;

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