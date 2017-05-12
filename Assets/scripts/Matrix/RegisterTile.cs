using System;
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
        public TileType TileType { get { return TileType.List[tileTypeIndex]; } }

        private Vector2 savedPosition = Vector2.zero;

        void Start() {
            UpdateTile();
        }

        void OnValidate() {
            if(!Application.isPlaying && gameObject.activeInHierarchy && currentTileTypeIndex != tileTypeIndex) {
                currentTileTypeIndex = tileTypeIndex;
                UpdateTile();
            }
        }

        void OnDestroy() {
            try {
                Matrix.At(savedPosition).TryRemoveTile(gameObject);
            }catch(Exception) {
                Debug.Log(savedPosition + " " + gameObject.name);
            }
        }

        public void UpdateTileType(TileType tileType) {
            currentTileTypeIndex = TileType.List.IndexOf(tileType);
            tileTypeIndex = currentTileTypeIndex;

            UpdateTile();
        }

        public void UpdateTile() {
            Matrix.At(savedPosition).TryRemoveTile(gameObject);

            savedPosition = transform.position;

            AddTile();
        }

        private void AddTile() {
            if(!Matrix.At(savedPosition).TryAddTile(gameObject)) {
                Debug.Log("Couldn't add tile at " + savedPosition);
            }
        }
    }
}