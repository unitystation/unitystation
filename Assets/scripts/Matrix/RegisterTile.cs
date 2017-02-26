using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Matrix {

    [ExecuteInEditMode]
    public class RegisterTile: MonoBehaviour {

        public bool inSpace;

        [HideInInspector]
        public int tileTypeIndex;
        private int currentTileTypeIndex;
        public TileType TileType {
            get {
                return TileType.List[currentTileTypeIndex];
            }
        }

        private int x = -1, y = -1;

        void Start() {
            currentTileTypeIndex = tileTypeIndex;

            UpdatePosition();
        }

        void OnValidate() {
            if(currentTileTypeIndex != tileTypeIndex) {
                currentTileTypeIndex = tileTypeIndex;
                UpdateTileType(TileType.List[currentTileTypeIndex]);
            }
        }

        void OnDestroy() {
            if(x >= 0) {
                Matrix.At(x, y).TryRemoveTile(gameObject);
            }
        }

        public void UpdatePosition() {
            if(x >= 0)
                Matrix.At(x, y).TryRemoveTile(gameObject);

            x = Mathf.RoundToInt(transform.position.x);
            y = Mathf.RoundToInt(transform.position.y);

            AddTile();
        }

        public void UpdateTileType(TileType tileType) {
            if(x >= 0) {
                Matrix.At(x, y).TryRemoveTile(gameObject);
            }

            currentTileTypeIndex = TileType.List.IndexOf(tileType);
            tileTypeIndex = currentTileTypeIndex;

            if(x >= 0) {
                AddTile();
            }
        }

        private void AddTile() {

            Debug.Log("add tile " + x + " " + y);
            if(!Matrix.At(x, y).TryAddTile(gameObject)) {
                Debug.Log("Couldn't add tile at " + x + " " + y);
            }
        }
    }
}