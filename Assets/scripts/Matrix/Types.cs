using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Matrix {
    public class Type {
        protected int value;
        public string Name { get; private set; }

        public Type(string name, int value) {
            Name = name;
            this.value = value;
        }

        public static implicit operator int(Type type) {
            return type == null ? -1 : type.value;
        }

        public override string ToString() {
            return base.ToString();
        }
    }

	[System.Flags] enum TileProperty { HasFloor = 1, AtmosNotPassable = 2, NotPassable = 4, PassableRestrictions = 8, IsItem = 128}

    public class TileType: Type {
        private static List<TileType> tileTypes = new List<TileType>();
        public static IList<TileType> List { get { return tileTypes.AsReadOnly(); } }

        public static TileType None = new TileType("None");
        public static TileType Floor = new TileType("Floor", (int) TileProperty.HasFloor);
        public static TileType Object = new TileType("Object", (int) TileProperty.NotPassable);
        public static TileType Door = new TileType("Door", (int) (TileProperty.AtmosNotPassable | TileProperty.NotPassable));
        public static TileType Window = new TileType("Window", (int) (TileProperty.AtmosNotPassable | TileProperty.NotPassable));
        public static TileType Wall = new TileType("Wall", (int) (TileProperty.HasFloor | TileProperty.AtmosNotPassable | TileProperty.NotPassable));
		public static TileType Player = new TileType("Player", (int) TileProperty.NotPassable);
		public static TileType Item = new TileType("Item", (int) TileProperty.IsItem);
		//this is for tiles with glass that prevents movement in some of the directions
		public static TileType RestrictedMovement = new TileType("RestrictedMovement", (int)TileProperty.PassableRestrictions);

        public TileType(string name, int value=0) : base(name, value) {
            tileTypes.Add(this);
        }
    } 

    public class ConnectType: Type {
        private static int nextValue = 8;
        private static List<ConnectType> tileConnectTypes = new List<ConnectType>();
        public static IList<ConnectType> List { get { return tileConnectTypes.AsReadOnly(); } }

        public static ConnectType None = new ConnectType("None", 0);
        public static ConnectType Lattice = new ConnectType("Lattice", TileType.Floor);
        public static ConnectType Table = new ConnectType("Table");
        public static ConnectType Wall = new ConnectType("Wall");
        public static ConnectType Window = new ConnectType("Window");
        public static ConnectType Carpet = new ConnectType("Carpet");
        public static ConnectType Catwalk = new ConnectType("Catwalk");
        public static ConnectType Spaceship = new ConnectType("Spaceship");

        private ConnectType(string name, int value = -1) : base(name, value) {
            if(value < 0) {
                this.value = nextValue;
                nextValue <<= 1;
            }

            tileConnectTypes.Add(this);
        }
    }
}