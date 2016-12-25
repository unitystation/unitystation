using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Events;
using UnityEngine.Events;


public enum TileType {
    Space, Floor, Wall, Glass, Door
}

public class TileEvent: UnityEvent<TileType> { }

class MatrixNode {
    private TileEvent tileEvent = new TileEvent();
    private SortedList<TileType, int> tileTypes = new SortedList<TileType, int>();

    public MatrixNode() {
        RegisterTile(TileType.Space);
    }

    public TileType Type {
        get {
            return tileTypes.Keys[tileTypes.Values.Count - 1];
        }
    }

    public void RegisterTile(TileType tileType) {
        if(!tileTypes.ContainsKey(tileType)) {
            tileTypes[tileType] = 1;
        }else {
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

    public void AddListener(UnityAction<TileType> listener) {
        tileEvent.AddListener(listener);
    }

    public void RemoveListener(UnityAction<TileType> listener) {
        tileEvent.RemoveListener(listener);
    }
}

[ExecuteInEditMode]
public class WallMap: MonoBehaviour {

    public Vector3 offset;

    private MatrixNode[,] map = new MatrixNode[2000, 2000];

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
            Instance.map[y, x] = new MatrixNode();
        }
        Instance.map[y, x].RegisterTile(tileType);
    }

    public static void Remove(int x, int y, TileType tileType) {
        if(Instance && Instance.map[y, x] != null) {
            Instance.map[y, x].RemoveTileType(tileType);
        }
    }

    public static TileType GetTypeAt(int x, int y) {
        if(Instance.map[y, x] == null) {
            return TileType.Space;
        }else {
            return Instance.map[y, x].Type;
        }
    }

    public static void AddListener(int x, int y, UnityAction<TileType> listener) {
        if(Instance.map[y, x] == null) {
            Instance.map[y, x] = new MatrixNode();
        }
        Instance.map[y, x].AddListener(listener);
    }

    public static void RemoveListener(int x, int y, UnityAction<TileType> listener) {
        if(Instance.map[y, x] != null) {
            Instance.map[y, x].RemoveListener(listener);
        }
    }
}