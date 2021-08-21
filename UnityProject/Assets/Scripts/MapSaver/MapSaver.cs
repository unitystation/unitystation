using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TileManagement;
using UnityEngine;
using System.Linq;
using NUnit.Framework;

namespace MapSaver
{
	public static class MapSaver
	{
		public readonly static char Matrix4x4Char = '#';
		public readonly static char TileIDChar = '§';
		public readonly static char ColourChar = '◉';
		public readonly static char LayerChar = '☰';
		public readonly static char LocationChar = '@';

		public struct TileMapData
		{
			public List<string> CommonColours;
			public List<string> CommonLayerTiles;
			public List<string> CommonMatrix4x4;
			public string Data;
		}


		//Floating reference
		//Is floating reference can be left alone


		//Cross matrix reference
		//Lookup table
		//saving,
		//Jane and then bob
		//Jane needs reference to Bob

		//dictionary Bob game object ->  Jane ID (or game object ) //For when Jane goes first
		//dictionary Bob game object ->  Bob ID //For when Bob goes first

		//Internal matrix reference
		//Lookup table
		//saving,  Assign everything ID Index location,  pre-fill

		public class ObjectMapData
		{
			public List<PrefabData> PrefabData;
		}

		public class PrefabData
		{
			public ulong ID;
			public string PrefabID;
			public string Name;
			public string LocalPosition;
			public IndividualObject Object;
		}


		public class IndividualObject
		{
			public uint ChildLocation;
			public ulong ID;
			public string Name;
			public List<ClassData> ClassDatas = new List<ClassData>();
			public List<IndividualObject> Children = new List<IndividualObject>();
		}

		public class ClassData
		{
			public ulong ClassID;
			public List<FieldData> Data;
		}

		public class FieldData
		{
			public string Name;
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

			var PresentTiles = MetaTileMap.PresentTilesNeedsLock;
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


			List<Color> CommonColours = CommonColoursCount.OrderByDescending(kp => kp.Value)
				.Select(kp => kp.Key)
				.ToList();

			List<LayerTile> CommonLayerTiles = CommonLayerTilesCount.OrderByDescending(kp => kp.Value)
				.Select(kp => kp.Key)
				.ToList();

			List<Matrix4x4> CommonMatrix4x4 = CommonMatrix4x4Count.OrderByDescending(kp => kp.Value)
				.Select(kp => kp.Key)
				.ToList();
			lock (PresentTiles)
			{
				foreach (var Layer in PresentTiles)
				{
					foreach (var TileAndLocation in Layer.Value)
					{
						SB.Append(LocationChar);
						SB.Append(TileAndLocation.Key.x);
						SB.Append(",");
						SB.Append(TileAndLocation.Key.y);
						SB.Append(",");
						SB.Append(TileAndLocation.Key.z);
						SB.Append(LayerChar);
						SB.Append((int) Layer.Key.LayerType);

						int Index = CommonLayerTiles.IndexOf(TileAndLocation.Value.Tile);


						if (Index != 0)
						{
							SB.Append(TileIDChar);
							SB.Append(Index);
						}

						Index = CommonColours.IndexOf(TileAndLocation.Value.Colour);
						if (Index != 0)
						{
							SB.Append(ColourChar);
							SB.Append(Index);
						}

						Index = CommonMatrix4x4.IndexOf(TileAndLocation.Value.TransformMatrix);
						if (Index != 0)
						{
							SB.Append(Matrix4x4Char);
							SB.Append(Index);
						}
					}
				}
			}


			TileMapData.Data = SB.ToString();
			foreach (var layerTile in CommonLayerTiles)
			{
				TileMapData.CommonLayerTiles.Add(layerTile.name + LayerChar + layerTile.TileType);
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


		public static ObjectMapData SaveObjects(MetaTileMap MetaTileMap)
		{
			ulong ID = 0;
			ObjectMapData ObjectMapData = new ObjectMapData();
			ObjectMapData.PrefabData = new List<PrefabData>();
			foreach (var Object in MetaTileMap.ObjectLayer.GetTileList(CustomNetworkManager.Instance._isServer)
				.AllObjects)
			{
				PrefabData Prefab = new PrefabData();
				var Tracker = Object.GetComponent<PrefabTracker>();
				if (Tracker != null)
				{
					Prefab.PrefabID = Tracker.ForeverID;
					Prefab.ID = ID;
					ID++;
					Prefab.Name = Object.name;
					Prefab.Object = new IndividualObject();
					RecursiveSaveObject(ref ID, Prefab.Object,
						CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[Tracker.ForeverID],
						Object.gameObject);
					ObjectMapData.PrefabData.Add(Prefab);


				}
			}

			return ObjectMapData;
		}

		public static void RecursiveSaveObject(ref ulong ID,  IndividualObject individualObject, GameObject PrefabEquivalent, GameObject gameObject)
		{
			if (gameObject.name != PrefabEquivalent.name)
			{
				individualObject.Name = gameObject.name;
			}

			individualObject.ID = ID;
			ID++;
			//Compare classes here


			if (PrefabEquivalent.transform.childCount != gameObject.transform.childCount)
			{
				Logger.LogError("Mismatched children between Prefab " + PrefabEquivalent + " and game object " +
				                gameObject + " at " + gameObject.transform.localPosition + "  Added children is not currently supported in This version of the map saver ");
			}

			for(int i = 0; i < PrefabEquivalent.transform.childCount; i++)
			{
				var newindividualObject = new IndividualObject();
				individualObject.Children.Add(newindividualObject);
				RecursiveSaveObject(ref ID,newindividualObject,PrefabEquivalent.transform.GetChild(i).gameObject, gameObject.transform.GetChild(i).gameObject);
			}
		}
	}
}