using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

namespace Matrix {

    [ExecuteInEditMode]
    public class RegisterTile: MonoBehaviour {

        public bool inSpace;

        [HideInInspector]
        public int tileTypeIndex;
        private int currentTileTypeIndex;
        public TileType TileType { get { return TileType.List[tileTypeIndex]; } }
        [HideInInspector]
        public Vector3 savedPosition = Vector3.zero;

        void Start() {
            UpdateTile();
            currentTileTypeIndex = TileType.List.IndexOf(TileType);
            tileTypeIndex = currentTileTypeIndex;
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
            } catch(Exception) {
                Debug.Log(savedPosition + " " + gameObject.name);
            }
        }

        void OnEnable() {
            UpdateTile(transform.position);
        }

        public void UpdateTileType(TileType tileType) {
            currentTileTypeIndex = TileType.List.IndexOf(tileType);
            tileTypeIndex = currentTileTypeIndex;

            UpdateTile();
        }

        public void UpdateTile() {
            //NOTE: Because all floors were prefabbed without a FloorTile component attached
            //it is now easier to add the FloorTile from this component if it is a TileType.Floor
            if(currentTileTypeIndex == TileType.List.IndexOf(TileType.Floor)) {
                FloorTile fT = GetComponent<FloorTile>();
                if(fT == null) {
                    gameObject.AddComponent<FloorTile>();
                }
            }

            if(currentTileTypeIndex == TileType.List.IndexOf(TileType.Item)) {
                if(Matrix.At(transform.position).ContainsItem(gameObject)) {
                    //Don't do anything
                    return;
                }
            }

            Matrix.At(savedPosition).TryRemoveTile(gameObject);

            savedPosition = transform.position;

            AddTile();
        }

        /// <summary>
        /// Updates the tile with a position for moving objects
        /// </summary>
        /// <param name="newPos">The target position if it is in motion</param>
        public void UpdateTile(Vector3 newPos) {
            if(currentTileTypeIndex == TileType.List.IndexOf(TileType.Item)) {
                if(Matrix.At(transform.position).ContainsItem(gameObject)) {
					//Don't do anything
                    return;
                }
            }

            Matrix.At(savedPosition).TryRemoveTile(gameObject);

            savedPosition = newPos;

            AddTile();
        }

        private void AddTile() {
            if(!Matrix.At(savedPosition).TryAddTile(gameObject)) {
                Debug.Log("Couldn't add tile at " + savedPosition);
            }
        }

        public void RemoveTile() {
            if(!Matrix.At(savedPosition).TryRemoveTile(gameObject)) {
                Debug.Log("Couldn't remove tile at " + savedPosition);
			}
        }

        public void OnMouseEnter() {
            UIManager.SetToolTip = this.gameObject.name;
        }

        public void OnMouseExit() {
            UIManager.SetToolTip = "";
        }

        public void OnRemoveFromPool() {
            UpdateTile();
        }

        public void Unregister() {
            Matrix.At(savedPosition).TryRemoveTile(gameObject);
        }
    }
}