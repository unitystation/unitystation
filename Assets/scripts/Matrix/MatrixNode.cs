using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Doors;

namespace Matrix {

    [Serializable]
    public class MatrixNode {
        private int tileValue = 0;
        private int connectValue = 0;

        private UnityEvent connectEvent = new UnityEvent();

        [NonSerialized]
        private List<GameObject> tiles = new List<GameObject>();

        [NonSerialized]
		private List<ObjectBehaviour> items = new List<ObjectBehaviour>();

        [NonSerialized]
        private List<HealthBehaviour> damageables = new List<HealthBehaviour>();

        [NonSerialized]
		private List<ObjectBehaviour> players = new List<ObjectBehaviour>();

        private bool isDoor;
        private bool isWindow;
        private bool isWall;
        private bool isObject;
        private bool isSpace;
        private bool isPlayer;
		private bool isRestrictiveTile;

		//Holds the details if tile is blocking movement in certain directions
		private RestrictedMoveStruct restrictedMoveStruct;
	
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
            var tileType = registerTile.TileType;

            if(tileType == TileType.Item) {
				var obj = gameObject.GetComponent<ObjectBehaviour>();
                if(!items.Contains(obj)) {
                    items.Add(obj);
                }
            } else {
                if(tileType == TileType.Object || tileType == TileType.Player) {
                    var healthBehaviour = gameObject.GetComponent<HealthBehaviour>();
                    if(healthBehaviour != null && !damageables.Contains(healthBehaviour)) {
                        damageables.Add(healthBehaviour);
                    }
                }

                if (tileType == TileType.Player)
                {
					players.Add(gameObject.GetComponent<ObjectBehaviour>());
                }

				if (tileType == TileType.RestrictedMovement) {
					restrictedMoveStruct = gameObject.GetComponent<RestrictiveMoveTile>().GetRestrictedData;
					isRestrictiveTile = true;
				}

                tiles.Add(gameObject);
                UpdateValues();
            }

            return true;
        }

        public bool TryRemoveTile(GameObject gameObject) {
            var registerTile = gameObject.GetComponent<RegisterTile>();
            var tileType = registerTile.TileType;
            if(tileType == TileType.Item) {
				var objB = gameObject.GetComponent<ObjectBehaviour>();
                if(items.Contains(objB)) {
                    items.Remove(objB);
                }
            } else {
                if(tileType == TileType.Object || tileType == TileType.Player) {
                    var healthBehaviour = gameObject.GetComponent<HealthBehaviour>();
                    if(damageables.Contains(healthBehaviour)) {
                        damageables.Remove(healthBehaviour);
                    }
                }

                if (tileType == TileType.Player)
                {
					players.Remove(gameObject.GetComponent<ObjectBehaviour>());
                }

				if (tileType == TileType.RestrictedMovement) {
					isRestrictiveTile = false;
				}

                if(!tiles.Contains(gameObject)) {
                    return false;
                }

                tiles.Remove(gameObject);
                UpdateValues();
            }
            return true;
        }

        public bool ContainsTile(GameObject gameObject)
        {
            return tiles.Contains(gameObject);
        }

        public bool ContainsItem(GameObject gameObject) {
            var rT = gameObject.GetComponent<RegisterTile>();
            if(rT.TileType == TileType.Item) {
				var iT = gameObject.GetComponent<ObjectBehaviour>();
                return items.Contains(iT);
            }
            return false;
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

        public bool IsWindow() {
            return isWindow;
        }

        public bool IsWall() {
            return isWall;
        }

        public bool IsPlayer() {
            return isPlayer;
        }

        public bool IsObject() {
            return isObject;
        }

		public bool IsRestrictiveTile()
		{
			return isRestrictiveTile;
		}

        public DoorController GetDoor() {
            if(isDoor) {
                foreach(var tile in tiles) {
                    var registerTile = tile.GetComponent<RegisterTile>();
                    if(registerTile.TileType == TileType.Door) {
                        var doorControl = registerTile.gameObject.GetComponent<DoorController>();
                        return doorControl;
                    }
                }
            }
            return null;
        }

		public RestrictedMoveStruct GetMoveRestrictions(){
				return restrictedMoveStruct;
		}

        public PushPull GetPushPull() {
            if(isObject) {
                foreach(var tile in tiles) {
                    var registerTile = tile.GetComponent<RegisterTile>();
                    if(registerTile.TileType == TileType.Object) {
                        var objCollisions = registerTile.gameObject.GetComponent<PushPull>();
                        return objCollisions;
                    }
                }
            }
            return null;
        }

		public List<ObjectBehaviour> GetItems() {
			List<ObjectBehaviour> newList = new List<ObjectBehaviour>(items);
            return newList;
        }

        public List<ObjectBehaviour> GetPlayers(){
			List<ObjectBehaviour> newList = new List<ObjectBehaviour>(players);
            return newList;
        }

        public List<HealthBehaviour> GetDamageables() {
            return damageables;
        }

        public FloorTile GetFloorTile() {
            foreach(var tile in tiles) {
                var registerTile = tile.GetComponent<RegisterTile>();
                if(registerTile.TileType == TileType.Floor) {
                    return registerTile.gameObject.GetComponent<FloorTile>();
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
                if(tile != null)
                {
                    var registerTile = tile.GetComponent<RegisterTile>();
                    if (tileType == registerTile.TileType)
                        return true;
                }                
            }
            return false;
        }

        public List<GameObject> GetTiles() {
            return tiles;
        }

        private void UpdateValues() {
            tileValue = 0;
            connectValue = 0;
            isDoor = false;
            isWall = false;
            isSpace = false;
            isPlayer = false;
            isObject = false;

            foreach(var tile in tiles) {
                if(tile == null)
                    return;
                var registerTile = tile.GetComponent<RegisterTile>();


                if(registerTile.TileType != TileType.Item)
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
                if(registerTile.TileType == TileType.Wall) {
                    isWall = true;
                }
                if(registerTile.TileType == TileType.Window) {
                    isWindow = true;
                }
				if (registerTile.TileType == TileType.RestrictedMovement) {
					isRestrictiveTile = true;
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