using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Matrix {

    [ExecuteInEditMode]
    public class ConnectTrigger: MonoBehaviour {

        public TileType tileType;
        private TileType currentTileType;

        private Vector3 currentPosition;
        private TileConnect[] corners;

        private int x = -1, y = -1;

        void Start() {
            corners = GetComponentsInChildren<TileConnect>();

            UpdateTileType();

            UpdatePosition();
        }

        void LateUpdate() {
            if(currentPosition != transform.position) {
                UpdatePosition();
            }

            if(!Application.isPlaying) {
                if(currentTileType != tileType) {
                    UpdateTileType();
                }
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

            currentPosition = transform.position;

            x = (int) currentPosition.x;
            y = (int) currentPosition.y;

            Matrix.Add(x, y, currentTileType);

            foreach(var c in corners) {
                c.UpdatePosition(x, y);
            }
        }

        private void UpdateTileType() {
            if(x >= 0) {
                Matrix.Remove(x, y, currentTileType);
                Matrix.Add(x, y, tileType);
            }

            currentTileType = tileType;

            foreach(var c in corners) {
                c.TileType = currentTileType;
            }
        }
    }
}