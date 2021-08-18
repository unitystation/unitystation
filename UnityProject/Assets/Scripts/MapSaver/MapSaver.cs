using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TileManagement;
using UnityEngine;
using System.Linq;

public static class MapSaver
{
	public struct TileMapData
	{
		public List<string> CommonColours;
		public List<string> CommonLayerTiles;
		public List<string> CommonMatrix4x4;
		public string Data;
	}

	public static TileMapData SaveTileMap(MetaTileMap MetaTileMap)
	{
		//# Matrix4x4
		//§ TileID
		//◉ Colour

		//☰ Layer
		//@ location

		var TileMapData = new TileMapData();

		TileMapData.CommonColours = new List<string>();
		TileMapData.CommonMatrix4x4 = new List<string>();

		TileMapData.CommonLayerTiles = new List<string>();

		StringBuilder SB = new StringBuilder();

		Dictionary<Color, int> CommonColoursCount = new Dictionary<Color, int>();
		Dictionary<LayerTile, int> CommonLayerTilesCount = new Dictionary<LayerTile, int>();
		Dictionary<Matrix4x4, int> CommonMatrix4x4Count = new Dictionary<Matrix4x4, int>();

		List<Color> CommonColours = new List<Color>();
		List<LayerTile> CommonLayerTiles = new List<LayerTile>();
		List<Matrix4x4> CommonMatrix4x4 = new List<Matrix4x4>();

		//TileType tileType, string key

		var PresentTiles = MetaTileMap.GetPresentTiles();
		lock (PresentTiles)
		{
			foreach (var Layer in PresentTiles)
			{
				foreach (var TileAndLocation in Layer.Value)
				{
					if (CommonLayerTilesCount.ContainsKey(TileAndLocation.Value.Tile))
					{
						CommonLayerTilesCount[TileAndLocation.Value.Tile]++;
					}
					else
					{
						CommonLayerTilesCount[TileAndLocation.Value.Tile] = 1;
					}

					if (CommonColoursCount.ContainsKey(TileAndLocation.Value.Colour))
					{
						CommonColoursCount[TileAndLocation.Value.Colour]++;
					}
					else
					{
						CommonColoursCount[TileAndLocation.Value.Colour] = 1;
					}


					if (CommonMatrix4x4Count.ContainsKey(TileAndLocation.Value.TransformMatrix))
					{
						CommonMatrix4x4Count[TileAndLocation.Value.TransformMatrix]++;
					}
					else
					{
						CommonMatrix4x4Count[TileAndLocation.Value.TransformMatrix] = 1;
					}
				}
			}
		}


		CommonColours = CommonColoursCount.OrderByDescending(kp => kp.Value)
			.Select(kp => kp.Key)
			.ToList();

		CommonLayerTiles = CommonLayerTilesCount.OrderByDescending(kp => kp.Value)
			.Select(kp => kp.Key)
			.ToList();

		CommonMatrix4x4 = CommonMatrix4x4Count.OrderByDescending(kp => kp.Value)
			.Select(kp => kp.Key)
			.ToList();

		foreach (var Layer in PresentTiles)
		{
			foreach (var TileAndLocation in Layer.Value)
			{
				SB.Append("@");
				SB.Append(TileAndLocation.Key.x);
				SB.Append(",");
				SB.Append(TileAndLocation.Key.y);
				SB.Append(",");
				SB.Append(TileAndLocation.Key.z);
				SB.Append("☰");
				SB.Append((int) Layer.Key.LayerType);

				int Index = CommonLayerTiles.IndexOf(TileAndLocation.Value.Tile);


				if (Index != 0)
				{
					SB.Append("§");
					SB.Append(Index);
				}

				Index = CommonColours.IndexOf(TileAndLocation.Value.Colour);
				if (Index != 0)
				{
					SB.Append("◉");
					SB.Append(Index);
				}

				Index = CommonMatrix4x4.IndexOf(TileAndLocation.Value.TransformMatrix);
				if (Index != 0)
				{
					SB.Append("#");
					SB.Append(Index);
				}
			}
		}


		TileMapData.Data = SB.ToString();
		foreach (var layerTile in CommonLayerTiles)
		{
			TileMapData.CommonLayerTiles.Add(layerTile.name + "☰" + layerTile.TileType);
		}

		foreach (var inColor in CommonColours)
		{
			TileMapData.CommonColours.Add(ColorUtility.ToHtmlStringRGBA(inColor));
		}

		foreach (var matrix4X4 in CommonMatrix4x4)
		{
			SB.Clear();
			SB.Append(matrix4X4.m00.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m01.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m02.ToString());
			SB.Append(",");

			SB.Append(matrix4X4.m10.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m11.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m12.ToString());
			SB.Append(",");

			SB.Append(matrix4X4.m20.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m21.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m22.ToString());

			TileMapData.CommonMatrix4x4.Add(SB.ToString());
		}

		return TileMapData;
	}
}