using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Utils;
using Logs;
using UnityEngine;
using TileManagement;
using Objects;
using SecureStuff;
using Util;
using Tiles;
using UI.Core;

namespace MapSaver
{
	public static class MapSaver
	{
		//TODO s
		//TODO matrix move all Matrix,
		//TODO Referencing other game objects?

		//TODO Cross matrix references check
		//TODO VVUIElementHandler For better serialisation support?


		//TODO ####### data output #######

		public static readonly char Matrix4x4Char = '#';
		public static readonly char TileIDChar = '§';
		public static readonly char ColourChar = '◉';
		public static readonly char LayerChar = '☰';
		public static readonly char LocationChar = '@';


		public class MapData
		{
			public string Ver = "1.0.0";
			public string MapName;
			public List<MatrixData> ContainedMatrices = new List<MatrixData>();
		}

		public class MatrixData
		{
			public string Ver = "1.0.0";
			public string Location;
			public uint MatrixID;
			public string MatrixName;
			public CompactTileMapData CompactTileMapData;
			public GitFriendlyTileMapData GitFriendlyTileMapData;
			public CompactObjectMapData CompactObjectMapData;
			public List<GameGizmoModel> PreviewGizmos;
		}

		public class GitFriendlyTileMapData
		{
			public string Ver = "1.0.0";

			private Dictionary<string, List<GitFriendlyIndividualTile>> InternalXYs =
				new Dictionary<string, List<GitFriendlyIndividualTile>>();

			public List<KeyValuePair<string, List<GitFriendlyIndividualTile>>> XYs =
				new List<KeyValuePair<string, List<GitFriendlyIndividualTile>>>();

			public Dictionary<string, List<GitFriendlyIndividualTile>> GetXYs()
			{
				return InternalXYs;
			}

			public void GenerateXYs()
			{
				XYs = InternalXYs.ToList().OrderBy(kvp => kvp.Key, new CustomKeyComparer()).ToList();
			}


			class CustomKeyComparer : IComparer<string>
			{
				public int Compare(string x, string y)
				{
					string[] partsX = x.Split('X', 'Y');
					string[] partsY = y.Split('X', 'Y');

					int x1 = int.Parse(partsX[1]);
					int x2 = int.Parse(partsY[1]);

					int y1 = int.Parse(partsX[2]);
					int y2 = int.Parse(partsY[2]);

					if (x1 != x2)
					{
						return x1.CompareTo(x2);
					}
					else
					{
						return y1.CompareTo(y2);
					}
				}
			}
		}

		public class GitFriendlyIndividualTile
		{
			public string Tel;
			public int Lay;
			public int? Z;
			public string Col;
			public string Tf;

			//public int? W;
		}


		public class CompactTileMapData
		{
			public string Ver = "1.0.0";
			public List<string> CommonColours;
			public List<string> CommonLayerTiles;
			public List<string> CommonMatrix4x4;
			public string Data;
		}

		public class ClassData
		{
			public string ClassID; //name and int, is good
			public HashSet<FieldData> Data = new HashSet<FieldData>();

			public bool Removed = false;
			public bool Disabled = false;

			public virtual bool IsEmpty()
			{
				if (Data.Count == 0)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}


		//Bob game object ->  Jane ID
		public static List<Tuple<Component, FieldData>> UnserialisedObjectReferences =
			new List<Tuple<Component, FieldData>>();

		//Bob game object ->  Bob ID
		public static Dictionary<Component, string> ComponentToID = new Dictionary<Component, string>();


		public static HashSet<FieldData> FieldsToRefresh = new HashSet<FieldData>();

		public static HashSet<string> AlreadyReadySavedIDs = new HashSet<string>();

		public static ulong IDStatic = 0;

		public static uint IDmatrixStatic = 0;
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

		//Prefab ID -> Game object ID -> Component ID
		//


		public class CompactObjectMapData
		{
			public string Ver = "1.1.0";
			//1.1.0 Added support for Dictionaries and change syntax for Removed Elements

			public List<string> CommonPrefabs = new List<string>();

			public List<PrefabData> PrefabData;
			public Dictionary<string, uint> IDToNetIDClient = new Dictionary<string, uint>();
		}

		public class PrefabData
		{
			public string GitID;
			public ulong ID; //is good
			public string PrefabID;
			public string Name;
			public string LocalPRS;
			public IndividualObject Object;
		}


		public class IndividualObject
		{
			public string Name;
			public bool Removed = false;
			public uint ChildLocation;
			public string ID = "0"; //Child index, Child index,  Child index, NOTE Always has a Zero for root
			public List<ClassData> ClassDatas = new List<ClassData>();
			public List<IndividualObject> Children = null;

			public bool RemoveEmptys()
			{
				bool ISEmpty = true;

				if (ClassDatas != null && ClassDatas.Count > 0)
				{
					ISEmpty = false;
				}

				if (Removed)
				{
					ISEmpty = false;
				}

				List<IndividualObject> Toremove = new List<IndividualObject>();

				if (Children != null)
				{
					foreach (var Child in Children)
					{
						if (Child.RemoveEmptys() == false)
						{
							ISEmpty = false;
						}
						else
						{
							Toremove.Add(Child);
						}
					}

					foreach (var remove in Toremove)
					{
						Children.Remove(remove);
					}

					if (Children.Count == 0)
					{
						Children = null;
					}
				}


				return ISEmpty;
			}
		}

		public static MapData SaveMap(List<MetaTileMap> MetaTileMaps, bool Compact,
			string MapName = "Unknown map name your maps dam it")
		{
			var OutMapData = new MapData();

			UnserialisedObjectReferences.Clear();
			ComponentToID.Clear();
			FieldsToRefresh.Clear();
			IDStatic = 0;
			IDmatrixStatic = 0;
			AlreadyReadySavedIDs.Clear();

			OutMapData.MapName = MapName;
			foreach (var MetaTileMap in MetaTileMaps)
			{
				OutMapData.ContainedMatrices.Add(SaveMatrix(Compact, MetaTileMap, false));
			}


			//move outside if multiple  matrices
			foreach (var MFD in UnserialisedObjectReferences)
			{
				if (ComponentToID.ContainsKey(MFD.Item1))
				{
					MFD.Item2.AddID(ComponentToID[MFD.Item1]);
					MFD.Item2.RemoveRuntimeReference(MFD.Item1);
				}
				else
				{
					Loggy.LogError($"Missing money behaviour in MonoToID {MFD.Item1.name}");
				}
			}

			UnserialisedObjectReferences.Clear();
			ComponentToID.Clear();

			foreach (var FD in FieldsToRefresh)
			{
				FD.Serialise();
			}

			FieldsToRefresh.Clear();

			return OutMapData;
		}

		public static MatrixData SaveMatrix(bool Compact, MetaTileMap MetaTileMap, bool SingleSave = true,
			List<BetterBounds> LocalArea = null, bool NonmappedItems = false, HashSet<LayerType> LayersToProcess = null, bool DoSaveObjects = true)
		{
			if (SingleSave)
			{
				UnserialisedObjectReferences.Clear();
				ComponentToID.Clear();
				FieldsToRefresh.Clear();
				IDStatic = 0;
				IDmatrixStatic = 0;
				AlreadyReadySavedIDs.Clear();
			}


			HashSet<Vector3Int> AllowedPoints = null;

			if (LocalArea != null)
			{
				AllowedPoints = new HashSet<Vector3Int>();
				foreach (var Bounds in LocalArea)
				{
					AllowedPoints.UnionWith(Bounds.allPositionsWithin());
				}
			}


			MatrixData matrixData = new MatrixData();
			if (DoSaveObjects)
			{
				matrixData.CompactObjectMapData = SaveObjects(Compact, MetaTileMap, AllowedPoints, NonmappedItems);
			}

			SaveTileMap(Compact, matrixData, MetaTileMap, AllowedPoints, LayersToProcess);

			//matrixData.MatrixName = MetaTileMap.matrix.NetworkedMatrix.gameObject.name;
			matrixData.MatrixName = MetaTileMap.matrix.transform.parent.name;


			matrixData.MatrixID = IDmatrixStatic;
			matrixData.Location = Math.Round(MetaTileMap.matrix.transform.parent.localPosition.x, 2) + "┼" +
			                      Math.Round(MetaTileMap.matrix.transform.parent.localPosition.y, 2) + "┼" +
			                      Math.Round(MetaTileMap.matrix.transform.parent.localPosition.z, 2) + "┼";


			var Angles = MetaTileMap.matrix.transform.parent.eulerAngles;
			matrixData.Location = matrixData.Location +
			                      Math.Round(Angles.x, 2) + "ø" +
			                      Math.Round(Angles.y, 2) + "ø" +
			                      Math.Round(Angles.z, 2) + "ø";


			IDmatrixStatic++;

			if (SingleSave)
			{
				foreach (var MFD in UnserialisedObjectReferences)
				{
					if (ComponentToID.ContainsKey(MFD.Item1))
					{
						MFD.Item2.AddID(ComponentToID[MFD.Item1]);
					}
					else
					{
						MFD.Item2.Data = "MISSING";
						Loggy.LogError($"Missing money behaviour in MonoToID {MFD.Item1.name}");
					}
				}

				UnserialisedObjectReferences.Clear();
				ComponentToID.Clear();

				foreach (var FD in FieldsToRefresh)
				{
					FD.Serialise();
				}

				FieldsToRefresh.Clear();
			}

			return matrixData;
		}

		public static void SaveTileMap(bool Compact, MatrixData ToSaveTo, MetaTileMap metaTileMap,
			HashSet<Vector3Int> AllowedPoints = null, HashSet<LayerType> LayersToProcess = null)
		{
			if (Compact)
			{
				CompactTileMapSave(ToSaveTo, metaTileMap, AllowedPoints, LayersToProcess);
			}
			else
			{
				GitFriendlyTileMapSave(ToSaveTo, metaTileMap, AllowedPoints, LayersToProcess);
			}
		}

		public static string StringToVector(string Data, out Vector3 Vector)
		{
			var position = Data.Split("┼");
			Vector = new Vector3(float.Parse(position[0]), float.Parse(position[1]), float.Parse(position[2]));
			return position[3];
		}

		public static string VectorToString(Vector3 Position, bool Round = true)
		{
			if (Round)
			{
				return Math.Round(Position.x, 2) + "┼" + Math.Round(Position.y, 2) + "┼" + Math.Round(Position.z, 2) +
				       "┼";
			}
			else
			{
				return Math.Round(Position.x, 4) + "┼" + Math.Round(Position.y, 4) + "┼" + Math.Round(Position.z, 4) +
				       "┼";
			}
		}

		public static Vector3Int GitFriendlyPositionToVectorInt(string Position)
		{
			Position = Position[1..];
			var position = Position.Split("Y");
			return new Vector3Int(int.Parse(position[0]), int.Parse(position[1]));
		}

		public static string VectorIntToGitFriendlyPosition(Vector3Int pos)
		{
			return $"X{pos.x}Y{pos.y}";
		}


		public static string TileToString(LayerTile layerTile)
		{
			return layerTile.name;
		}


		public static Matrix4x4 StringToMatrix4X4(string Stringy)
		{
			var Entries = Stringy.Split(",");

			return new Matrix4x4()
			{
				m00 = float.Parse(Entries[0]),
				m01 = float.Parse(Entries[1]),
				m02 = float.Parse(Entries[2]),
				m03 = float.Parse(Entries[3]),

				m10 = float.Parse(Entries[4]),
				m11 = float.Parse(Entries[5]),
				m12 = float.Parse(Entries[6]),
				m13 = float.Parse(Entries[7]),

				m20 = float.Parse(Entries[8]),
				m21 = float.Parse(Entries[9]),
				m22 = float.Parse(Entries[10]),
				m23 = float.Parse(Entries[11]),

				m30 = float.Parse(Entries[12]),
				m31 = float.Parse(Entries[13]),
				m32 = float.Parse(Entries[14]),
				m33 = float.Parse(Entries[15]),
			};
		}


		public static string Matrix4X4ToString(Matrix4x4 matrix4X4, StringBuilder SB)
		{
			SB.Clear();

			SB.Append(matrix4X4.m00.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m01.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m02.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m03.ToString());
			SB.Append(",");


			SB.Append(matrix4X4.m10.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m11.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m12.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m13.ToString());
			SB.Append(",");

			SB.Append(matrix4X4.m20.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m21.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m22.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m23.ToString());
			SB.Append(",");

			SB.Append(matrix4X4.m30.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m31.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m32.ToString());
			SB.Append(",");
			SB.Append(matrix4X4.m33.ToString());

			return SB.ToString();
		}


		public static void GitFriendlyTileMapSave(MatrixData ToSaveTo, MetaTileMap metaTileMap,
			HashSet<Vector3Int> AllowedPoints = null, HashSet<LayerType> LayersToProcess = null)
		{
			//# Matrix4x4
			//§ TileID
			//◉ Colour

			//☰ Layer
			//@ location
			bool UseBoundary = AllowedPoints != null;

			var TileMapData = new GitFriendlyTileMapData();


			StringBuilder SB = new StringBuilder();

			var PresentTiles = metaTileMap.PresentTilesNeedsLock;

			var MultilayerPresentTiles = metaTileMap.MultilayerPresentTilesNeedsLock;

			var XYs = TileMapData.GetXYs();
			lock (PresentTiles)
			{
				foreach (var Layer in PresentTiles)
				{
					foreach (var TileAndLocation in Layer)
					{
						if (TileAndLocation?.layerTile == null) continue;

						if (UseBoundary)
						{
							var pos1 = TileAndLocation.LocalPosition;
							pos1.z = 0;
							if (AllowedPoints.Contains(pos1) == false)
							{
								continue;
							}
						}

						if (LayersToProcess != null)
						{
							if (LayersToProcess.Contains(TileAndLocation.layer.LayerType) == false)
							{
								continue;
							}
						}

						string pos = VectorIntToGitFriendlyPosition(TileAndLocation.LocalPosition);
						if (XYs.ContainsKey(pos) == false)
						{
							XYs[pos] = new List<GitFriendlyIndividualTile>();
						}

						GitFriendlyIndividualTile Tile = new GitFriendlyIndividualTile();
						if (TileAndLocation.LocalPosition.z != 0)
						{
							Tile.Z = TileAndLocation.LocalPosition.z;
						}

						Tile.Lay = (int) TileAndLocation.layer.LayerType;

						Tile.Tel = TileToString(TileAndLocation.layerTile);

						if (TileAndLocation.Colour != Color.white)
						{
							Tile.Col = TileAndLocation.Colour.ToHexString();
						}

						var matrix4X4 = TileAndLocation.transformMatrix;
						if (TileAndLocation.transformMatrix != Matrix4x4.identity)
						{
							Tile.Tf = Matrix4X4ToString(matrix4X4, SB);
						}

						XYs[pos].Add(Tile);
					}
				}
			}

			lock (MultilayerPresentTiles)
			{
				foreach (var Layer in MultilayerPresentTiles)
				{
					foreach (var TileAndLocations in Layer)
					{
						foreach (var TileAndLocation in TileAndLocations)
						{
							if (TileAndLocation == null) continue;
							if (UseBoundary)
							{
								var pos1 = TileAndLocation.LocalPosition;
								pos1.z = 0;
								if (AllowedPoints.Contains(pos1) == false)
								{
									continue;
								}
							}


							if (LayersToProcess != null)
							{
								if (LayersToProcess.Contains(TileAndLocation.layer.LayerType) == false)
								{
									continue;
								}
							}


							string pos = VectorIntToGitFriendlyPosition(TileAndLocation.LocalPosition);
							if (XYs.ContainsKey(pos) == false)
							{
								XYs[pos] = new List<GitFriendlyIndividualTile>();
							}

							GitFriendlyIndividualTile Tile = new GitFriendlyIndividualTile();
							//TODO Tile map upgrade , Change to vector 4
							if (TileAndLocation.LocalPosition.z != 0)
							{
								Tile.Z = TileAndLocation.LocalPosition.z;
							}

							Tile.Lay = (int) TileAndLocation.layer.LayerType;

							Tile.Tel = TileToString(TileAndLocation.layerTile);

							if (TileAndLocation.Colour != Color.white)
							{
								Tile.Col = TileAndLocation.Colour.ToHexString();
							}

							var matrix4X4 = TileAndLocation.transformMatrix;
							if (TileAndLocation.transformMatrix != Matrix4x4.identity)
							{
								Tile.Tf = Matrix4X4ToString(matrix4X4, SB);
							}

							XYs[pos].Add(Tile);
						}
					}
				}
			}


			TileMapData.GenerateXYs();
			ToSaveTo.GitFriendlyTileMapData = TileMapData;
		}

		public static void CompactTileMapSave(MatrixData ToSaveTo, MetaTileMap metaTileMap,
			HashSet<Vector3Int> AllowedPoints = null, HashSet<LayerType> LayersToProcess = null)
		{
			//# Matrix4x4
			//§ TileID
			//◉ Colour

			//☰ Layer
			//@ location
			bool UseBoundary = AllowedPoints != null;

			var TileMapData = new CompactTileMapData();

			TileMapData.CommonColours = new List<string>();
			TileMapData.CommonMatrix4x4 = new List<string>();

			TileMapData.CommonLayerTiles = new List<string>();

			StringBuilder SB = new StringBuilder();

			Dictionary<Color, int> CommonColoursCount = new Dictionary<Color, int>();
			Dictionary<LayerTile, int> CommonLayerTilesCount = new Dictionary<LayerTile, int>();
			Dictionary<Matrix4x4, int> CommonMatrix4x4Count = new Dictionary<Matrix4x4, int>();

			var PresentTiles = metaTileMap.PresentTilesNeedsLock;


			lock (PresentTiles)
			{
				foreach (var Layer in PresentTiles)
				{
					foreach (var TileAndLocation in Layer)
					{
						if (UseBoundary)
						{
							var pos = TileAndLocation.LocalPosition;
							pos.z = 0;
							if (AllowedPoints.Contains(pos) == false)
							{
								continue;
							}
						}


						if (TileAndLocation?.layerTile == null) continue;

						if (LayersToProcess != null)
						{
							if (LayersToProcess.Contains(TileAndLocation.layer.LayerType) == false)
							{
								continue;
							}
						}


						if (TileAndLocation.layer.LayerType.IsMultilayer())
						{
							break;
						}

						if (CommonLayerTilesCount.ContainsKey(TileAndLocation.layerTile))
						{
							CommonLayerTilesCount[TileAndLocation.layerTile]++;
						}
						else
						{
							CommonLayerTilesCount[TileAndLocation.layerTile] = 1;
						}

						if (CommonColoursCount.ContainsKey(TileAndLocation.Colour))
						{
							CommonColoursCount[TileAndLocation.Colour]++;
						}
						else
						{
							CommonColoursCount[TileAndLocation.Colour] = 1;
						}


						if (CommonMatrix4x4Count.ContainsKey(TileAndLocation.transformMatrix))
						{
							CommonMatrix4x4Count[TileAndLocation.transformMatrix]++;
						}
						else
						{
							CommonMatrix4x4Count[TileAndLocation.transformMatrix] = 1;
						}
					}
				}
			}

			var MultilayerPresentTiles = metaTileMap.MultilayerPresentTilesNeedsLock;


			lock (MultilayerPresentTiles)
			{
				foreach (var Layer in MultilayerPresentTiles)
				{
					foreach (var TileAndLocations in Layer)
					{
						foreach (var TileAndLocation in TileAndLocations)
						{
							if (TileAndLocation == null) continue;
							if (UseBoundary)
							{
								var pos = TileAndLocation.LocalPosition;
								pos.z = 0;
								if (AllowedPoints.Contains(pos) == false)
								{
									continue;
								}
							}

							if (LayersToProcess != null)
							{
								if (LayersToProcess.Contains(TileAndLocation.layer.LayerType) == false)
								{
									continue;
								}
							}


							if (CommonLayerTilesCount.ContainsKey(TileAndLocation.layerTile))
							{
								CommonLayerTilesCount[TileAndLocation.layerTile]++;
							}
							else
							{
								CommonLayerTilesCount[TileAndLocation.layerTile] = 1;
							}

							if (CommonColoursCount.ContainsKey(TileAndLocation.Colour))
							{
								CommonColoursCount[TileAndLocation.Colour]++;
							}
							else
							{
								CommonColoursCount[TileAndLocation.Colour] = 1;
							}


							if (CommonMatrix4x4Count.ContainsKey(TileAndLocation.transformMatrix))
							{
								CommonMatrix4x4Count[TileAndLocation.transformMatrix]++;
							}
							else
							{
								CommonMatrix4x4Count[TileAndLocation.transformMatrix] = 1;
							}
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
					foreach (var TileAndLocation in Layer)
					{
						if (TileAndLocation?.layerTile == null) continue;

						if (UseBoundary)
						{
							var pos = TileAndLocation.LocalPosition;
							pos.z = 0;
							if (AllowedPoints.Contains(pos) == false)
							{
								continue;
							}
						}

						if (LayersToProcess != null)
						{
							if (LayersToProcess.Contains(TileAndLocation.layer.LayerType) == false)
							{
								continue;
							}
						}

						SB.Append(LocationChar);
						SB.Append(TileAndLocation.LocalPosition.x);
						SB.Append(",");
						SB.Append(TileAndLocation.LocalPosition.y);
						SB.Append(",");
						SB.Append(TileAndLocation.LocalPosition.z);
						SB.Append(LayerChar);
						SB.Append((int) TileAndLocation.layer.LayerType);

						int Index = CommonLayerTiles.IndexOf(TileAndLocation.layerTile);


						if (Index != 0)
						{
							SB.Append(TileIDChar);
							SB.Append(Index);
						}

						Index = CommonColours.IndexOf(TileAndLocation.Colour);
						if (Index != 0)
						{
							SB.Append(ColourChar);
							SB.Append(Index);
						}

						Index = CommonMatrix4x4.IndexOf(TileAndLocation.transformMatrix);
						if (Index != 0)
						{
							SB.Append(Matrix4x4Char);
							SB.Append(Index);
						}
					}
				}
			}

			lock (MultilayerPresentTiles)
			{
				foreach (var Layer in MultilayerPresentTiles)
				{
					foreach (var TileAndLocations in Layer)
					{
						foreach (var TileAndLocation in TileAndLocations)
						{
							if (TileAndLocation == null) continue;
							if (UseBoundary)
							{
								var pos = TileAndLocation.LocalPosition;
								pos.z = 0;
								if (AllowedPoints.Contains(pos) == false)
								{
									continue;
								}
							}

							if (LayersToProcess != null)
							{
								if (LayersToProcess.Contains(TileAndLocation.layer.LayerType) == false)
								{
									continue;
								}
							}

							//TODO Tile map upgrade , Change to vector 4
							SB.Append(LocationChar);
							SB.Append(TileAndLocation.LocalPosition.x);
							SB.Append(",");
							SB.Append(TileAndLocation.LocalPosition.y);
							SB.Append(",");
							SB.Append(TileAndLocation.LocalPosition.z);
							SB.Append(LayerChar);
							SB.Append((int) TileAndLocation.layer.LayerType);

							int Index = CommonLayerTiles.IndexOf(TileAndLocation.layerTile);


							if (Index != 0)
							{
								SB.Append(TileIDChar);
								SB.Append(Index);
							}

							Index = CommonColours.IndexOf(TileAndLocation.Colour);
							if (Index != 0)
							{
								SB.Append(ColourChar);
								SB.Append(Index);
							}

							Index = CommonMatrix4x4.IndexOf(TileAndLocation.transformMatrix);
							if (Index != 0)
							{
								SB.Append(Matrix4x4Char);
								SB.Append(Index);
							}
						}
					}
				}
			}


			TileMapData.Data = SB.ToString();
			foreach (var layerTile in CommonLayerTiles)
			{
				TileMapData.CommonLayerTiles.Add(TileToString(layerTile));
			}

			foreach (var inColor in CommonColours)
			{
				TileMapData.CommonColours.Add(ColorUtility.ToHtmlStringRGBA(inColor));
			}

			foreach (var matrix4X4 in CommonMatrix4x4)
			{
				TileMapData.CommonMatrix4x4.Add(Matrix4X4ToString(matrix4X4, SB));
			}

			ToSaveTo.CompactTileMapData = TileMapData;
		}


		public static CompactObjectMapData SaveObjects(bool Compact, MetaTileMap MetaTileMap,
			HashSet<Vector3Int> AllowedPoints = null, bool NonmappedItems = false)
		{
			bool UseBoundary = AllowedPoints != null;
			CompactObjectMapData compactObjectMapData = new CompactObjectMapData();
			compactObjectMapData.PrefabData = new List<PrefabData>();

			IEnumerable<RegisterTile> Objects = null;

			if (Application.isPlaying)
			{
				Objects = MetaTileMap.ObjectLayer.GetTileList(CustomNetworkManager.Instance._isServer)
					.AllObjects; //TODO Disabled objectsxz
			}
			else
			{
				Objects = MetaTileMap.GetComponentsInChildren<RegisterTile>(true);
#if UNITY_EDITOR
				CommonManagerEditorOnly.Instance.CustomNetworkManagerPrefab.ForeverIDLookupSpawnablePrefabs.Clear();
				CommonManagerEditorOnly.Instance.CustomNetworkManagerPrefab.SetUpSpawnablePrefabsForEverIDManual();
				CustomNetworkManager.Instance = CommonManagerEditorOnly.Instance.CustomNetworkManagerPrefab;
#endif
			}


			foreach (var Object in Objects)
			{
				if (UseBoundary)
				{
					var pos1 = Object.transform.localPosition.RoundToInt();
					pos1.z = 0;
					if (AllowedPoints.Contains(pos1) == false)
					{
						continue;
					}
				}

				ProcessIndividualObject(Compact, Object.gameObject, compactObjectMapData, NonmappedItems : NonmappedItems);
			}

			if (Application.isPlaying) //EtherealThings Haven't been triggered so they are in the correct spot
			{
				foreach (var EtherealThing in MetaTileMap.matrix.MetaDataLayer.EtherealThings)
				{
					if (UseBoundary)
					{
						var pos1 = EtherealThing.transform.localPosition.RoundToInt();
						pos1.z = 0;
						if (AllowedPoints.Contains(pos1) == false)
						{
							continue;
						}
					}

					ProcessIndividualObject(Compact, EtherealThing.gameObject, compactObjectMapData,
						EtherealThing.transform.localPosition, NonmappedItems : NonmappedItems);
				}
			}

			if (Compact)
			{
				Dictionary<string, int> PrefabIDCount = new Dictionary<string, int>();

				foreach (var prefabData in compactObjectMapData.PrefabData)
				{
					if (PrefabIDCount.ContainsKey(prefabData.PrefabID) == false)
					{
						PrefabIDCount[prefabData.PrefabID] = 0;
					}

					PrefabIDCount[prefabData.PrefabID]++;
				}

				List<string> CommonPrefabID = PrefabIDCount.OrderByDescending(kp => kp.Value)
					.Select(kp => kp.Key)
					.ToList();

				compactObjectMapData.CommonPrefabs = CommonPrefabID;

				foreach (var prefabData in compactObjectMapData.PrefabData)
				{
					prefabData.PrefabID =
						CommonPrefabID.IndexOf(prefabData.PrefabID).ToString(); //TODO Rethink ToString To save ""
				}
			}

			return compactObjectMapData;
		}

		public static void StringToPRS(GameObject Object, string Data)
		{
			Data = StringToVector(Data, out Vector3 position);
			Object.transform.localPosition = position;
			if (Data.Contains("ø"))
			{
				var rotation = Data.Split("ø");
				Object.transform.rotation = Quaternion.Euler(new Vector3(float.Parse(rotation[0]),
					float.Parse(rotation[1]), float.Parse(rotation[2])));
				if (rotation.Length > 3)
				{
					Data = rotation[3];
				}
			}

			if (Data.Contains("↔"))
			{
				var Size = Data.Split("↔");
				Object.transform.localScale =
					new Vector3(float.Parse(Size[0]), float.Parse(Size[1]), float.Parse(Size[2]));
				if (Size.Length > 3)
				{
					Data = Size[3];
				}
			}
		}

		public static string PRSToString(GameObject Object, Vector3? CoordinateOverride = null)
		{
			string data = VectorToString(Object.transform.localPosition);
			if (CoordinateOverride == null)
			{
				data = VectorToString(Object.transform.localPosition);

				if (Object.transform.localRotation.eulerAngles != Vector3.zero)
				{
					var Angles = Object.transform.localRotation.eulerAngles;
					data = data + Math.Round(Angles.x, 2) + "ø" +
					       Math.Round(Angles.y, 2) + "ø" +
					       Math.Round(Angles.z, 2) + "ø";
				}


				if (Object.transform.localScale != Vector3.one)
				{
					var Angles = Object.transform.localScale;
					data = data + Math.Round(Angles.x, 2) + "↔" +
					       Math.Round(Angles.y, 2) + "↔" +
					       Math.Round(Angles.z, 2) + "↔";
				}
			}
			else
			{
				data = VectorToString(CoordinateOverride.GetValueOrDefault(Vector3.zero));
			}

			return data;
		}

		public static void ProcessIndividualObject(bool Compact, GameObject Object,
			CompactObjectMapData compactObjectMapData,
			Vector3? CoordinateOverride = null, bool NonmappedItems = false)
		{
			if (NonmappedItems == false)
			{
				var RuntimeSpawned = Object.GetComponent<RuntimeSpawned>();
				if (RuntimeSpawned != null) return;
			}


			PrefabData Prefab = new PrefabData();

			var Tracker = Object.GetComponent<PrefabTracker>();
			if (Tracker == null)
			{
				Loggy.LogError(Object.name +
				               " Is missing a PrefabTracker Please make it inherit from the base item/object prefab ");
				return;
			}

			var OriginPrefab = CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[Tracker.ForeverID];
			if (Compact)
			{
				if (Object.name != OriginPrefab.name)
				{
					Prefab.Name = Object.name;
				}
			}
			else
			{
				Prefab.Name = Object.name;
			}


			Prefab.PrefabID = Tracker.ForeverID;
			if (Compact)
			{
				Prefab.ID = IDStatic;
				IDStatic++;
			}
			else
			{
				int trys = 0;
				Prefab.GitID = Prefab.PrefabID + "_" + VectorToString(Object.transform.localPosition);
				while (AlreadyReadySavedIDs.Contains(Prefab.GitID))
				{
					var vec = Object.transform.localPosition;
					vec.x += (0.001f * trys); //TODO May cause issues if you resolve conflict
					Prefab.GitID = Prefab.PrefabID + "_" + VectorToString(vec, false);
					trys++;
				}

				AlreadyReadySavedIDs.Add(Prefab.GitID);
			}

			Prefab.Object = new IndividualObject();
			Prefab.LocalPRS = PRSToString(Object, CoordinateOverride);

			var OnObjectComplete = Object.GetComponentsInChildren<Component>(true).ToHashSet();
			var OnGmaeObjectComplete = Object.GetComponentsInChildren<Transform>(true).Select(x => x.gameObject)
				.ToHashSet();


			RecursiveSaveObject(OnObjectComplete, OnGmaeObjectComplete, Compact, Prefab, "0", Prefab.Object,
				OriginPrefab,
				Object.gameObject, compactObjectMapData, CoordinateOverride, NonmappedItems : NonmappedItems  );
			if (Prefab.Object.RemoveEmptys())
			{
				Prefab.Object = null;
			}

			compactObjectMapData.PrefabData.Add(Prefab);
		}


		public static void RecursiveSaveObject(HashSet<Component> AllComponentsOnObject,
			HashSet<GameObject> AllGameObjectOnObject, bool Compact, PrefabData PrefabData, string ID,
			IndividualObject individualObject,
			GameObject PrefabEquivalent, GameObject gameObject, CompactObjectMapData compactObjectMapData,
			Vector3? CoordinateOverride = null, bool UseInstance = false, bool NonmappedItems = false,
			bool IgnoreMapSaverIgnoreObject = false)
		{

			if (gameObject.HasComponent<MapSaverIgnoreObject>() && IgnoreMapSaverIgnoreObject == false) return;

			individualObject.ID =
				ID; //NOTE The zero is technically redundant for the First layer, But it's built into the saver

			if (PrefabEquivalent?.name != gameObject.name)
			{
				individualObject.Name = gameObject.name;
			}

			//Compare classes here
			FillOutClassData(AllComponentsOnObject, AllGameObjectOnObject, Compact, PrefabData, individualObject,
				PrefabEquivalent, gameObject, compactObjectMapData,
				CoordinateOverride, UseInstance, NonmappedItems);


			int PrefabObjectChildCount = 0;

			if (PrefabEquivalent != null)
			{
				PrefabObjectChildCount = PrefabEquivalent.transform.childCount;
			}

			var gameObjectChildCount = gameObject.transform.childCount;
			var loopMax = Mathf.Max(PrefabObjectChildCount, gameObjectChildCount);
			int PrefabIndex = 0;
			int GameObjectIndex = 0;


			int IDLocation = 0;

			for (int i = 0; i < loopMax; i++)
			{
				if (PrefabObjectChildCount > PrefabIndex
				    && PrefabEquivalent.transform.GetChild(PrefabIndex).name !=
				    gameObject.transform.GetChild(PrefabIndex).name)
				{
					while (PrefabObjectChildCount > PrefabIndex)
					{
						if (PrefabEquivalent.transform.GetChild(PrefabIndex).name !=
						    gameObject.transform.GetChild(GameObjectIndex).name)
						{
							var AnewindividualObject = new IndividualObject()
							{
								ID = ID + "," + IDLocation,
								Removed = true,
								ClassDatas = null
							};
							if (individualObject.Children == null)
								individualObject.Children = new List<IndividualObject>();
							individualObject.Children.Add(AnewindividualObject);
							PrefabIndex++;
							IDLocation++;
						}
						else
						{
							break;
						}
					}
				}

				GameObject PrefabChild = null;

				if (PrefabObjectChildCount > PrefabIndex)
				{
					PrefabChild = PrefabEquivalent.transform.GetChild(PrefabIndex).gameObject;
				}

				var newindividualObject = new IndividualObject();
				if (individualObject.Children == null) individualObject.Children = new List<IndividualObject>();
				individualObject.Children.Add(newindividualObject);
				RecursiveSaveObject(AllComponentsOnObject, AllGameObjectOnObject, Compact, PrefabData,
					ID + "," + IDLocation,
					newindividualObject,
					PrefabChild,
					gameObject.transform.GetChild(GameObjectIndex).gameObject, compactObjectMapData,
					UseInstance: UseInstance, NonmappedItems : NonmappedItems);

				GameObjectIndex++;
				PrefabIndex++;
				IDLocation++;
			}
		}

		public static void FillOutClassData(HashSet<Component> AllComponentsOnObject,
			HashSet<GameObject> AllGameObjectOnObject, bool Compact, PrefabData PrefabData,
			IndividualObject individualObject,
			GameObject PrefabEquivalent, GameObject gameObject, CompactObjectMapData compactObjectMapData,
			Vector3? CoordinateOverride = null, bool UseInstance = false, bool NonmappedItems = false)
		{
			Dictionary<string, int> ClassCount = new Dictionary<string, int>();

			List<Component> PrefabComponents = new List<Component>();
			if (PrefabEquivalent != null)
			{
				PrefabComponents = PrefabEquivalent.GetComponents<Component>().ToList();
			}

			var gameObjectComponents = gameObject.GetComponents<Component>().ToList();

			var loopMax = Mathf.Max(PrefabComponents.Count, gameObjectComponents.Count);
			int PrefabIndex = 0;
			int GameObjectIndex = 0;


			for (int i = 0; i < loopMax; i++)
			{
				if (PrefabComponents.Count > PrefabIndex
				    && PrefabComponents[PrefabIndex].GetType() != gameObjectComponents[GameObjectIndex].GetType())
				{
					while (PrefabComponents.Count > PrefabIndex)
					{
						if (PrefabComponents[PrefabIndex].GetType() != gameObjectComponents[GameObjectIndex].GetType())
						{
							ClassCount.TryAdd(PrefabComponents[PrefabIndex].GetType().Name, 0);
							var RemoveOutClass = new ClassData();
							RemoveOutClass.ClassID = PrefabComponents[PrefabIndex].GetType().Name + "@" +
							                         ClassCount[PrefabComponents[PrefabIndex].GetType().Name];
							ClassCount[PrefabComponents[PrefabIndex].GetType().Name]++;
							RemoveOutClass.Removed = true;
							RemoveOutClass.Data = null;
							individualObject.ClassDatas.Add(RemoveOutClass);
							PrefabIndex++;
						}
						else
						{
							break;
						}
					}
				}


				Component PrefabMono = null;
				if (PrefabComponents.Count > PrefabIndex)
				{
					PrefabMono = PrefabComponents[PrefabIndex];
				}

				var gameObjectMono = gameObjectComponents[GameObjectIndex];


				if (Application.isPlaying) //Is in edit mode you can't have stuff inside of inventories in this mode
				{
					var objectContainer = gameObjectMono as ObjectContainer;
					if (objectContainer != null)
					{
						foreach (var objectBehaviour in objectContainer.GetStoredObjects()
							         .Select(obj => obj.GetComponent<UniversalObjectPhysics>()))
						{
							if (CoordinateOverride == null)
							{
								ProcessIndividualObject(Compact, objectBehaviour.gameObject, compactObjectMapData,
									gameObject.transform.localPosition, NonmappedItems : NonmappedItems);
							}
							else
							{
								ProcessIndividualObject(Compact, objectBehaviour.gameObject, compactObjectMapData,
									CoordinateOverride,  NonmappedItems : NonmappedItems);
							}
						}
					}


					var itemStorage = gameObjectMono as ItemStorage;
					if (itemStorage != null)
					{
						foreach (var objectBehaviour in itemStorage.GetItemSlots())
						{
							if (objectBehaviour.Item == null) continue;
							if (CoordinateOverride == null)
							{
								ProcessIndividualObject(Compact, objectBehaviour.Item.gameObject, compactObjectMapData,
									gameObject.transform.localPosition, NonmappedItems : NonmappedItems);
							}
							else
							{
								ProcessIndividualObject(Compact, objectBehaviour.Item.gameObject, compactObjectMapData,
									CoordinateOverride, NonmappedItems : NonmappedItems);
							}
						}
					}
				}

				var OutClass = new ClassData();
				ClassCount.TryAdd(gameObjectMono.GetType().Name, 0);
				OutClass.ClassID = gameObjectMono.GetType().Name + "@" + ClassCount[gameObjectMono.GetType().Name];
				ClassCount[gameObjectMono.GetType().Name]++;
				if (gameObjectMono is MonoBehaviour Mono)
				{
					OutClass.Disabled = !Mono.enabled;
				}

				if (Compact)
				{
					ComponentToID[gameObjectMono] = PrefabData.ID + "@" + individualObject.ID + "@" + OutClass.ClassID;
				}
				else
				{
					ComponentToID[gameObjectMono] =
						PrefabData.GitID + "@" + individualObject.ID + "@" + OutClass.ClassID;
				}

				SecureMapsSaver.RecursiveSearchData(
					AllComponentsOnObject,
					AllGameObjectOnObject,
					CodeClass.ThisCodeClass,
					OutClass.Data,
					"",
					PrefabMono,
					gameObjectMono,
					UseInstance);

				if (OutClass.IsEmpty() == false || PrefabEquivalent == null)
				{
					individualObject.ClassDatas.Add(OutClass);
				}

				PrefabIndex++;
				GameObjectIndex++;
			}
		}


		public class CodeClass : IPopulateIDRelation
		{
			private static CodeClass thisCodeClass;
			public static CodeClass ThisCodeClass => thisCodeClass ??= new CodeClass();


			private static string GetID(string Id)
			{
				if (Id == "MISSING")
				{
					Loggy.LogError("Map has missing references");
					return null;
				}

				return GetGameObjectPath(Id);
			}

			private static string GetGameObjectPath(string Id)
			{
				if (Id == "MISSING")
				{
					Loggy.LogError("Map has missing references");
					return null;
				}

				var IDPath = Id.Split("@");
				return IDPath[0];
			}


			public void Reset()
			{
				NeededToProcess.Clear();
				Objects.Clear();
			}

			public void FlagSaveKey(string RootID, Component Object, FieldData FieldData)
			{
				var UnprocessedEntry = new UnprocessedData()
				{
					Object = Object,
					FieldData = FieldData,
					ID =  RootID
				};

				if (NeededToProcess.ContainsKey(RootID) == false)
				{
					NeededToProcess[RootID] = new ReferencesAndData();
				}

				//note [UnprocessedEntry] = new List<string>(); Can overwrite?


				var NeededID = GetID(FieldData.Data);
				if (string.IsNullOrEmpty(NeededID) == false)
				{
					NeededToProcess[RootID].ReferencesNeeded.Add(NeededID);
				}

				NeededToProcess[RootID].FieldsToPopulate.Add(UnprocessedEntry);
			}

			public void FinishLoading(string GitiD)
			{
				HashSet<FieldData> ReuseSEt = new HashSet<FieldData>();
				foreach (var Waiting in NeededToProcess) //TODO Probably a bit slow
				{
					if (Waiting.Value.ReferencesNeeded.Contains(GitiD))
					{
						Waiting.Value.ReferencesNeeded.Remove(GitiD);
					}

					if (Waiting.Value.ReferencesNeeded.Count == 0)
					{
						foreach (var Unprocessed in Waiting.Value.FieldsToPopulate)
						{
							ReuseSEt.Add(Unprocessed.FieldData);
							SecureMapsSaver.LoadData(Unprocessed.ID, Unprocessed.Object, ReuseSEt, MapSaver.CodeClass.ThisCodeClass, CustomNetworkManager.IsServer);
							ReuseSEt.Clear();
						}
					}
				}
			}

			public object ObjectsFromForeverID(string ForeverID, Type InType)
			{
				if (ForeverID == "NULL")
				{
					return null;
				}

				if (CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs.TryGetValue(ForeverID,
					    out var Gameobject))
				{
					return Gameobject;
				}


				return Librarian.Page.DeSerialiseValue(ForeverID, InType);
			}


			public Dictionary<string, ReferencesAndData> NeededToProcess =
				new Dictionary<string, ReferencesAndData>();

			public class ReferencesAndData
			{
				public List<string> ReferencesNeeded = new List<string>();
				public List<UnprocessedData> FieldsToPopulate = new List<UnprocessedData>();
			}

			public Dictionary<string, GameObject> Objects { get; set; } = new Dictionary<string, GameObject>();

			public void PopulateIDRelation(HashSet<FieldData> FieldDatas, FieldData fieldData, Component mono,
				bool UseInstance = false)
			{
				if (UseInstance)
				{
					fieldData.AddRuntimeReference(mono);
				}

				if (ComponentToID.TryGetValue(mono, out var value))
				{
					fieldData.AddID(value);
					fieldData.RemoveRuntimeReference(mono);
				}
				else
				{
					UnserialisedObjectReferences.Add(new Tuple<Component, FieldData>(mono, fieldData));
				}

				FieldsToRefresh.Add(fieldData);
				FieldDatas.Add(fieldData);
			}
		}
	}
}