using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

namespace Matrix
{

    [ExecuteInEditMode]
    public class RegisterTile : MonoBehaviour
    {

        public bool inSpace;

        [HideInInspector]
        public int tileTypeIndex;
        private int currentTileTypeIndex;
        public TileType TileType { get { return TileType.List[tileTypeIndex]; } }

        [HideInInspector]
        public Vector3 savedPosition = Vector3.zero;

        void Start()
        {
            UpdateTile();
        }

        void OnValidate()
        {
            if (!Application.isPlaying && gameObject.activeInHierarchy && currentTileTypeIndex != tileTypeIndex)
            {
                currentTileTypeIndex = tileTypeIndex;
                UpdateTile();
            }
        }

        void OnDestroy()
        {
            try
            {
                Matrix.At(savedPosition).TryRemoveTile(gameObject);
            }
            catch (Exception)
            {
                Debug.Log(savedPosition + " " + gameObject.name);
            }
        }

        public void UpdateTileType(TileType tileType)
        {
            currentTileTypeIndex = TileType.List.IndexOf(tileType);
            tileTypeIndex = currentTileTypeIndex;

            UpdateTile();
        }

        public void UpdateTile()
        {
            Matrix.At(savedPosition).TryRemoveTile(gameObject);

            savedPosition = transform.position;

            AddTile();
        }
        /// <summary>
        /// Updates the tile with a position for moving objects
        /// </summary>
        /// <param name="newPos">The target position if it is in motion</param>
        public void UpdateTile(Vector3 newPos)
        {
            Matrix.At(savedPosition).TryRemoveTile(gameObject);

            savedPosition = newPos;

            AddTile();
        }

        private void AddTile()
        {
            if (!Matrix.At(savedPosition).TryAddTile(gameObject))
            {
                Debug.Log("Couldn't add tile at " + savedPosition);
            }
        }

        public void OnMouseEnter()
        {
            UIManager.SetToolTip = this.gameObject.name;
        }

        public void OnMouseExit()
        {
            UIManager.SetToolTip = "";
        }
    }
}