using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// object representation of JSON map
/// </summary>
[Serializable]
public class TilemapLayer
{
    [SerializeField] private string name;
    [SerializeField] private List<Vector3Int> tilePositions = new List<Vector3Int>(); //to be converted into Vector3Int
    [SerializeField] private List<UniTile> tiles = new List<UniTile>();

    public TilemapLayer()
    {
    }

    public TilemapLayer(string name)
    {
        this.name = name;
    }

    public void Add(int x, int y, UniTile tile)
    {
        tilePositions.Add(new Vector3Int(x, y, 0));
        tiles.Add(tile);
    }

    public override string ToString()
    {
        return String.Format("{0}: {1} positions, {2} tiles",name, tilePositions.Count, tiles.Count);
    }
}