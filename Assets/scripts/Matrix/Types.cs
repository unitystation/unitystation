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
    }

    public class TileType: Type {
        private static List<TileType> tileTypes = new List<TileType>();
        public static IList<TileType> List { get { return tileTypes.AsReadOnly(); } }

        public static TileType None = new TileType("None", 0);
        public static TileType Floor = new TileType("Floor", 1);
        public static TileType Object = new TileType("Object", 4);
        public static TileType Door = new TileType("Door", 6);
        public static TileType Window = new TileType("Window", 6);
        public static TileType Wall = new TileType("Wall", 7);

        public TileType(string name, int value) : base(name, value) {
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

        private ConnectType(string name, int value = -1) : base(name, value) {
            if(value < 0) {
                this.value = nextValue;
                nextValue <<= 1;
            }

            tileConnectTypes.Add(this);
        }
    }
}