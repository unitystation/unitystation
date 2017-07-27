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

		[NonSerialized]
		private List<ItemControl> items = new List<ItemControl>();
	    
	    [NonSerialized]
		private List<HealthBehaviour> damageables = new List<HealthBehaviour>();

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

        public bool TryAddTile(GameObject _gameObject) {
            var registerTile = _gameObject.GetComponent<RegisterTile>();
            if(!registerTile) {
                return false;
            }
	        var tileType = registerTile.TileType;

	        if (tileType == TileType.Item) {
				var itemControl = _gameObject.GetComponent<ItemControl>();
				if (!items.Contains(itemControl)) {
					items.Add(itemControl);
				}
			} 
			else 
			{
				if (tileType == TileType.Object || tileType == TileType.Player)
				{
					var healthBehaviour = _gameObject.GetComponent<HealthBehaviour>();
					if ( !damageables.Contains(healthBehaviour) ) {
						damageables.Add(healthBehaviour);
					}
				}
				tiles.Add(_gameObject);
				UpdateValues();
			}

            return true;
        }

        public bool TryRemoveTile(GameObject _gameObject) {
			var registerTile = _gameObject.GetComponent<RegisterTile>();
	        var tileType = registerTile.TileType;
	        if (tileType == TileType.Item) {
				var iT = _gameObject.GetComponent<ItemControl>();
				if (items.Contains(iT)) {
						items.Remove(iT);
				}
			} 
	        else
	        {
		        if (tileType == TileType.Object || tileType == TileType.Player)
		        {
			        var healthBehaviour = _gameObject.GetComponent<HealthBehaviour>();
			        if ( damageables.Contains(healthBehaviour) )
			        {
				        damageables.Remove(healthBehaviour);
			        }
		        }
		        if ( !tiles.Contains(_gameObject) )
		        {
			        return false;
		        }

		        tiles.Remove(_gameObject);
		        UpdateValues();
	        }
	        return true;
        }

		public bool ContainsTile(GameObject _gameObject){
			if (tiles.Contains(_gameObject)) {
				return true;
			}
			return false;
		}

		public bool ContainsItem(GameObject _gameObject){
			RegisterTile rT = _gameObject.GetComponent<RegisterTile>();
			if (rT.TileType == TileType.Item) {
				ItemControl iT = _gameObject.GetComponent<ItemControl>();
				if (items.Contains(iT)) {
					return true;
				}
				return false;
			}
			return false;
		}

        public bool FitsTile(GameObject _gameObject) {
            var registerTile = _gameObject.GetComponent<RegisterTile>();
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

		public List<ItemControl> GetItems(){
			return items;
		}
	    
	    public List<HealthBehaviour> GetDamageables(){
			return damageables;
		}

		public FloorTile GetFloorTile(){
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