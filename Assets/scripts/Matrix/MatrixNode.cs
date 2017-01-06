using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Matrix {

    public class MatrixNode {

        private class TileEvent: UnityEvent<TileType> { }
        
        private TileEvent tileEvent = new TileEvent();
        private SortedList<TileType, int> tileTypes = new SortedList<TileType, int>();

        public MatrixNode() {
            AddTileType(TileType.Space);
        }

        public TileType Type {
            get {
                return tileTypes.Keys[tileTypes.Values.Count - 1];
            }
        }

        public void AddTileType(TileType tileType) {
            if(!tileTypes.ContainsKey(tileType)) {
                tileTypes[tileType] = 1;
            } else {
                tileTypes[tileType]++;
            }
            tileEvent.Invoke(tileType);
        }

        public void RemoveTileType(TileType tileType) {
            tileTypes[tileType] -= 1;
            if(tileTypes[tileType] == 0) {
                tileTypes.Remove(tileType);
            }

            tileEvent.Invoke(Type);
        }

        public bool HasTileType(TileType tileType) {
            return tileTypes.Keys.Contains(tileType);
        }

        public void AddListener(UnityAction<TileType> listener) {
            tileEvent.AddListener(listener);
        }

        public void RemoveListener(UnityAction<TileType> listener) {
            tileEvent.RemoveListener(listener);
        }
    }
}