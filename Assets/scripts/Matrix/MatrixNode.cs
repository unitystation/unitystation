using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Matrix {

    [Serializable]
    public class MatrixNode {
        private int tileValue = 0;
        private int connectValue = 0;

        private UnityEvent connectEvent = new UnityEvent();

        [NonSerialized]
        private List<GameObject> tiles = new List<GameObject>();

        private bool isDoor;
		private bool isObject;
        private bool isSpace;
		private bool isPlayer;

        [SerializeField]
        private Section section;
        public Section Section {
            get { return section; }
            set { section = value; UpdateSection(); }
        }

        public bool TryAddTile(GameObject gameObject) {
            var registerTile = gameObject.GetComponent<RegisterTile>();
            if(!registerTile) {
                return false;
            }

            tiles.Add(gameObject);
            UpdateValues();
            return true;
        }

        public bool TryRemoveTile(GameObject gameObject) {
            if(!tiles.Contains(gameObject)) {
                return false;
            }

            tiles.Remove(gameObject);
            UpdateValues();
            return true;
        }

        public bool FitsTile(GameObject gameObject) {
            var registerTile = gameObject.GetComponent<RegisterTile>();
            return registerTile && (tileValue & registerTile.TileType) == 0;
        }

        public bool IsSpace() {
            return isSpace || (tileValue & (int) (TileProperty.AtmosNotPassable | TileProperty.HasFloor)) == 0;
        }

        public bool IsPassable() {
            return (tileValue & (int) TileProperty.NotPassable) == 0;
        }

        public bool IsAtmosPassable() {
            return (tileValue & (int) TileProperty.AtmosNotPassable) == 0;
        }

        public bool IsDoor() {
            return isDoor;
        }

		public bool IsPlayer() {
			return isPlayer;
		}

		public bool IsObject() {
			return isObject;
		}

        public DoorController GetDoor() {
            if(isDoor) {
                foreach(var tile in tiles) {
                    var registerTile = tile.GetComponent<RegisterTile>();
                    if(registerTile.TileType == TileType.Door) {
                        DoorController doorControl = registerTile.gameObject.GetComponent<DoorController>();
                        return doorControl;
                    }
                }
            }
            return null;
        }

		public ObjectActions GetObjectActions() {
			if(isObject) {
				foreach(var tile in tiles) {
					var registerTile = tile.GetComponent<RegisterTile>();
					if(registerTile.TileType == TileType.Object) {
						ObjectActions objCollisions = registerTile.gameObject.GetComponent<ObjectActions>();
						return objCollisions;
					}
				}
			}
			return null;
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

        public bool HasTileType(TileType tileType) {
            foreach(var tile in tiles) {
                var registerTile = tile.GetComponent<RegisterTile>();
                if(tileType == registerTile.TileType)
                    return true;
            }
            return false;
        }

        private void UpdateValues() {
            tileValue = 0;
            connectValue = 0;
            isDoor = false;
            isSpace = false;
			isPlayer = false;
			isObject = false;

            foreach(var tile in tiles) {
				if (tile == null)
					return;
                var registerTile = tile.GetComponent<RegisterTile>();


				
                tileValue |= registerTile.TileType;

                var connectTrigger = tile.GetComponent<ConnectTrigger>();
                if(connectTrigger) {
                    connectValue |= connectTrigger.ConnectType;
                }

                if(registerTile.TileType == TileType.Door) {
                    isDoor = true;
                }
				if(registerTile.TileType == TileType.Object) {
					isObject = true;
				}
				if(registerTile.TileType == TileType.Player) {
					isPlayer = true;
				}

                if(registerTile.inSpace) {
                    isSpace = true;
                }
            }

            if(isSpace) {
                if((tileValue | (int) TileProperty.HasFloor) != (int) TileProperty.HasFloor) {
                    isSpace = false;
                }
            }

            connectEvent.Invoke();
        }

        private void UpdateSection() {
            foreach(var tile in tiles) {
                tile.transform.MoveToSection(section);
            }
        }
    }
}