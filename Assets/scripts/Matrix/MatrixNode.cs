using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Matrix {   

    public class MatrixNode {
        private int tileValue = 0;
        private int connectValue = 0;

        private UnityEvent connectEvent = new UnityEvent();

        private List<GameObject> tiles = new List<GameObject>();

        public bool TryAddTile(GameObject gameObject) {
            var registerTile = gameObject.GetComponent<RegisterTile>();
            if(!registerTile) {
                return false;
            } 

            tiles.Add(gameObject);
            UpdateValues();

            connectEvent.Invoke();

            return true;
        }

        public bool TryRemoveTile(GameObject gameObject) {
            if(!tiles.Contains(gameObject)) {
                return false;
            }
            
            tiles.Remove(gameObject);
            UpdateValues();

            connectEvent.Invoke();

            return true;
        }

        public bool FitsTile(GameObject gameObject) {
            var registerTile = gameObject.GetComponent<RegisterTile>();
            return registerTile && (tileValue & registerTile.TileType) == 0;
        }

        public bool IsSpace() {
            return (tileValue & 3) == 0;
        }

        public bool IsPassable() {
            return (tileValue & 4) == 0;
        }

        public bool IsAtmosPassable() {
            return (tileValue & 2) == 0;
        }

        public bool Connects(ConnectType connectType) {
            if(connectType != null) {
                return ((connectType & (connectValue | tileValue)) > 0);
            }
            return false;
        }

        public void AddListener(UnityAction listener) {
            connectEvent.AddListener(listener);
        }

        public void RemoveListener(UnityAction listener) {
            connectEvent.RemoveListener(listener);
        }

        private void UpdateValues() {
            tileValue = 0;
            connectValue = 0;

            foreach(var tile in tiles) {
                var registerTile = tile.GetComponent<RegisterTile>();
                tileValue |= registerTile.TileType;

                var connectTrigger = tile.GetComponent<ConnectTrigger>();
                if(connectTrigger) {
                    connectValue |= connectTrigger.ConnectType;
                }
            }
        }
    }
}