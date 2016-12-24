using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Events;
using UnityEngine.Events;

public class TileEvent: UnityEvent<TileType> { }

class Tile {
    private TileType type;

    public TileType Type {
        get {
            return type;
        }
        set {
            if(type == value) {
                sameTypeCounter++;
            } else {
                type = value;
                sameTypeCounter = 1;
            }

            Event.Invoke(type);

        }
    }

    private TileEvent _event;
    public TileEvent Event {
        get {
            if(_event == null) {
                _event = new TileEvent();
            }

            return _event;
        }
    }

    // temp
    public int sameTypeCounter = 1;
}

[ExecuteInEditMode]
public class WallMap: MonoBehaviour {

    public Vector3 offset;

    private Tile[,] map = new Tile[2000, 2000];

    private static WallMap wallMap;

    public static WallMap Instance {
        get {
            if(!wallMap) {
                wallMap = FindObjectOfType<WallMap>();
            }

            return wallMap;
        }
    }

    public static void Add(int x, int y, TileType tileType) {
        if(Instance.map[y, x] == null) {
            Instance.map[y, x] = new Tile();
        }
        Instance.map[y, x].Type = tileType;
    }

    public static void Remove(int x, int y) {
        if(Instance && Instance.map[y, x] != null) {
            if(Instance.map[y, x].sameTypeCounter > 1) {
                Instance.map[y, x].sameTypeCounter--;
            } else {
                Instance.map[y, x].Type = TileType.Space;
            }
        }
    }

    public static bool CheckType(int x, int y, TileType tileType) {
        if(Instance.map[y, x] == null) {
            return tileType == TileType.Space;
        }
        return Instance.map[y, x].Type == tileType;
    }

    public static void AddListener(int x, int y, UnityAction<TileType> listener) {
        if(Instance.map[y, x] == null) {
            Instance.map[y, x] = new Tile();
        }
        Instance.map[y, x].Event.AddListener(listener);
    }

    public static void RemoveListener(int x, int y, UnityAction<TileType> listener) {
        if(Instance.map[y, x] != null) {
            Instance.map[y, x].Event.RemoveListener(listener);
        }
    }
}
