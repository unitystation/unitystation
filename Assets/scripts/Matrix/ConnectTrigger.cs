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

            currentPosition = transform.position;
            UpdatePosition((int) currentPosition.x, (int) currentPosition.y);
        }

        void LateUpdate() {
            if(currentPosition != transform.position) {
                currentPosition = transform.position;
                UpdatePosition((int) currentPosition.x, (int) currentPosition.y);
            }

            if(!Application.isPlaying) {
                if(currentTileType != tileType) {
                    currentTileType = tileType;
                    UpdateTileType();
                }
            }
        }

        void OnDestroy() {
            if(x >= 0) {
                Matrix.Remove(x, y, tileType);
            }
        }

        private void UpdatePosition(int x_new, int y_new) {
            if(x >= 0)
                Matrix.Remove(x, y, tileType);

            Matrix.Add(x_new, y_new, tileType);

            x = x_new;
            y = y_new;

            foreach(var c in corners) {
                c.UpdatePosition(x, y);
            }
        }

        private void UpdateTileType() {

            foreach(var c in corners) {
                c.TileType = tileType;
            }
        }
    }
}