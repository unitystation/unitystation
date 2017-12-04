using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// object representation of JSON map
/// </summary>
[Serializable]
public class TilemapLayer
{
    //    [SerializeField] private string name;
    [SerializeField] private List<XYCoord> tilePositions = new List<XYCoord>(); //to be converted into Vector3Int
    [SerializeField] private List<UniTileData> tiles = new List<UniTileData>();
    //    [SerializeField] private List<UniTile> tiles = new List<UniTile>();

    //    public TilemapLayer()
    //    {
    //    }

    //    public TilemapLayer(string name)
    //    {
    //        this.name = name;
    //    }

    public List<XYCoord> TilePositions => tilePositions;

    public List<UniTileData> Tiles => tiles;

    public void Add(int x, int y, UniTileData tile)
    {
        tilePositions.Add(new XYCoord(x, y));
        tiles.Add(tile);
    }

    public override string ToString()
    {
        return String.Format("{0} positions, {1} tiles", tilePositions.Count, tiles.Count);
    }
}