using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TileManagement;
using UnityEngine;
using System.Linq;
using System.Reflection;
using Objects;

namespace MapSaver
{
	public static class MapSaver
	{
		public readonly static char Matrix4x4Char = '#';
		public readonly static char TileIDChar = '§';
		public readonly static char ColourChar = '◉';
		public readonly static char LayerChar = '☰';
		public readonly static char LocationChar = '@';


		public class MapData
		{
			public string MapName;
			public List<MatrixData> ContainedMatrices = new List<MatrixData>();
		}

		public class MatrixData
		{
			public string Location;
			public uint MatrixID;
			public string MatrixName;
			public TileMapData TileMapData;
			public ObjectMapData ObjectMapData;
		}


		public class TileMapData
		{
			public List<string> CommonColours;
			public List<string> CommonLayerTiles;
			public List<string> CommonMatrix4x4;
			public string Data;
		}

		//Bob game object ->  Jane ID
		public static List<Tuple<MonoBehaviour, FieldData>> UnserialisedObjectReferences =
			new List<Tuple<MonoBehaviour, FieldData>>();

		//Bob game object ->  Bob ID
		public static Dictionary<MonoBehaviour, string> MonoToID = new Dictionary<MonoBehaviour, string>();


		public static HashSet<FieldData> FieldsToRefresh = new HashSet<FieldData>();

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



		//TODO Future, matrix move,  escape shuttle, shuttle  fuel, cargo shuttle , needs to be standard system for more


		public class ObjectMapData
		{
			public List<PrefabData> PrefabData;
		}

		public class PrefabData
		{
			public ulong ID; //is good
			public string PrefabID;
			public string Name;
			public string LocalPRS;
			public IndividualObject Object;
		}


		public class IndividualObject
		{
			public uint ChildLocation;
			public string ID; //Child index, Child index,  Child index,
			public List<ClassData> ClassDatas = new List<ClassData>();
			public List<IndividualObject> Children = new List<IndividualObject>();

			public bool RemoveEmptys()
			{
				bool ISEmpty = true;

				if (ClassDatas.Count > 0)
				{
					ISEmpty = false;
				}

				List<IndividualObject> Toremove = new List<IndividualObject>();

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

				return ISEmpty;
			}
		}

		public class ClassData
		{
			public string ClassID; //name and int, is good
			public HashSet<FieldData> Data = new HashSet<FieldData>();

			public bool IsEmpty()
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

		public class FieldData
		{
			private List<string> ReferencingIDs;

			private List<MonoBehaviour> RuntimeReferences;

			public List<MonoBehaviour> GetRuntimeReferences()
			{
				return RuntimeReferences;
			}

			public void RemoveRuntimeReference(MonoBehaviour inRuntimeReference)
			{
				if (RuntimeReferences == null)
				{
					return;
				}

				RuntimeReferences.Remove(inRuntimeReference);
			}

			public void AddRuntimeReference(MonoBehaviour inRuntimeReference)
			{
				if (RuntimeReferences == null)
				{
					RuntimeReferences = new List<MonoBehaviour>();
				}

				RuntimeReferences.Add(inRuntimeReference);
			}

			public void AddID(string ToAdd)
			{
				if (ReferencingIDs == null)
				{
					ReferencingIDs = new List<string>();
				}

				ReferencingIDs.Add(ToAdd);
			}

			public void Serialise()
			{
				if (ReferencingIDs == null) return;
				foreach (var ID in ReferencingIDs)
				{
					Data = Data + "," + ID;
				}
			}

			public string Name;
			public string Data;
		}

		public static MapData SaveMap(List<MetaTileMap> MetaTileMaps,
			string MapName = "Unknown map name your maps dam it")
		{
			var OutMapData = new MapData();

			UnserialisedObjectReferences.Clear();
			MonoToID.Clear();
			FieldsToRefresh.Clear();
			IDStatic = 0;
			IDmatrixStatic = 0;

			OutMapData.MapName = MapName;
			foreach (var MetaTileMap in MetaTileMaps)
			{
				OutMapData.ContainedMatrices.Add(SaveMatrix(MetaTileMap, false));
			}

			//move outside if multiple  matrices
			foreach (var MFD in UnserialisedObjectReferences)
			{
				if (MonoToID.ContainsKey(MFD.Item1))
				{
					MFD.Item2.AddID(MonoToID[MFD.Item1]);
					MFD.Item2.RemoveRuntimeReference(MFD.Item1);
				}
				else
				{
					Logger.LogError("Missing money behaviour in MonoToID");
				}
			}

			UnserialisedObjectReferences.Clear();
			MonoToID.Clear();

			foreach (var FD in FieldsToRefresh)
			{
				FD.Serialise();
			}

			FieldsToRefresh.Clear();

			return OutMapData;
		}

		public static MatrixData SaveMatrix(MetaTileMap MetaTileMap, bool SingleSave = true,
			Vector3? Localboundarie1 = null, Vector3? Localboundarie2 = null, bool UseInstance = false)
		{
			if (SingleSave)
			{
				UnserialisedObjectReferences.Clear();
				MonoToID.Clear();
				FieldsToRefresh.Clear();
				IDStatic = 0;
				IDmatrixStatic = 0;
			}

			MatrixData matrixData = new MatrixData();
			matrixData.ObjectMapData = SaveObjects(MetaTileMap, Localboundarie1, Localboundarie2, UseInstance);
			matrixData.TileMapData = SaveTileMap(MetaTileMap, Localboundarie1, Localboundarie2);
			matrixData.MatrixName = MetaTileMap.PresentMatrix.NetworkedMatrix.gameObject.name;
			matrixData.MatrixID = IDmatrixStatic;
			matrixData.Location = Math.Round(MetaTileMap.PresentMatrix.NetworkedMatrix.transform.localPosition.x, 2) + "┼" +
			                      Math.Round(MetaTileMap.PresentMatrix.NetworkedMatrix.transform.localPosition.y, 2) + "┼" +
			                      Math.Round(MetaTileMap.PresentMatrix.NetworkedMatrix.transform.localPosition.z, 2) + "┼";


			var Angles = MetaTileMap.PresentMatrix.NetworkedMatrix.transform.eulerAngles;
			matrixData.Location = matrixData.Location +
			                      Math.Round(Angles.x, 2) + "ø" +
			                      Math.Round(Angles.y, 2) + "ø" +
			                      Math.Round(Angles.z, 2) + "ø";


			IDmatrixStatic++;

			if (SingleSave)
			{
				foreach (var MFD in UnserialisedObjectReferences)
				{
					if (MonoToID.ContainsKey(MFD.Item1))
					{
						MFD.Item2.AddID(MonoToID[MFD.Item1]);
					}
					else
					{
						Logger.LogError("Missing money behaviour in MonoToID");
					}
				}

				UnserialisedObjectReferences.Clear();
				MonoToID.Clear();

				foreach (var FD in FieldsToRefresh)
				{
					FD.Serialise();
				}

				FieldsToRefresh.Clear();
			}

			return matrixData;
		}

		// function to find if given point
		// lies inside a given rectangle or not.
		//TODO Tile map upgrade  upgrade to include xyz (Need special condition for underfloor since that's w)
		//note Localboundarie1 Needs to be the smaller one
		public static bool IsPointWithin(Vector3 Localboundarie1, Vector3 Localboundarie2, Vector3 Point)
		{
			if (Point.x > Localboundarie1.x && Point.x < Localboundarie2.x && Point.y > Localboundarie1.y &&
			    Point.y < Localboundarie2.y)
			{
				return true;
			}

			return false;
		}


		public static TileMapData SaveTileMap(MetaTileMap metaTileMap, Vector3? Localboundarie1 = null,
			Vector3? Localboundarie2 = null)
		{
			//# Matrix4x4
			//§ TileID
			//◉ Colour

			//☰ Layer
			//@ location
			bool UseBoundary = Localboundarie1 != null;

			var TileMapData = new TileMapData();

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
					if (Layer.Key.LayerType == LayerType.Underfloor) continue;
					foreach (var TileAndLocation in Layer.Value)
					{
						if (UseBoundary)
						{
							if (IsPointWithin(Localboundarie1.Value, Localboundarie2.Value, TileAndLocation.Key) ==
							    false)
							{
								continue;
							}
						}


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

			var MultilayerPresentTiles = metaTileMap.MultilayerPresentTilesNeedsLock;


			lock (MultilayerPresentTiles)
			{
				foreach (var Layer in MultilayerPresentTiles)
				{
					foreach (var TileAndLocations in Layer.Value)
					{
						foreach (var TileAndLocation in TileAndLocations.Value)
						{
							if (TileAndLocation == null) continue;
							if (UseBoundary)
							{
								if (IsPointWithin(Localboundarie1.Value, Localboundarie2.Value,
									    TileAndLocation.TileCoordinates) ==
								    false)
								{
									continue;
								}
							}


							if (CommonLayerTilesCount.ContainsKey(TileAndLocation.Tile))
							{
								CommonLayerTilesCount[TileAndLocation.Tile]++;
							}
							else
							{
								CommonLayerTilesCount[TileAndLocation.Tile] = 1;
							}

							if (CommonColoursCount.ContainsKey(TileAndLocation.Colour))
							{
								CommonColoursCount[TileAndLocation.Colour]++;
							}
							else
							{
								CommonColoursCount[TileAndLocation.Colour] = 1;
							}


							if (CommonMatrix4x4Count.ContainsKey(TileAndLocation.TransformMatrix))
							{
								CommonMatrix4x4Count[TileAndLocation.TransformMatrix]++;
							}
							else
							{
								CommonMatrix4x4Count[TileAndLocation.TransformMatrix] = 1;
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
					foreach (var TileAndLocation in Layer.Value)
					{
						if (UseBoundary)
						{
							if (IsPointWithin(Localboundarie1.Value, Localboundarie2.Value, TileAndLocation.Key) ==
							    false)
							{
								continue;
							}
						}

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

			lock (MultilayerPresentTiles)
			{
				foreach (var Layer in MultilayerPresentTiles)
				{
					foreach (var TileAndLocations in Layer.Value)
					{
						foreach (var TileAndLocation in TileAndLocations.Value)
						{
							if (TileAndLocation == null) continue;
							if (UseBoundary)
							{
								if (IsPointWithin(Localboundarie1.Value, Localboundarie2.Value,
									    TileAndLocation.TileCoordinates) ==
								    false)
								{
									continue;
								}
							}

							//TODO Tile map upgrade , Change to vector 4
							SB.Append(LocationChar);
							SB.Append(TileAndLocation.TileCoordinates.x);
							SB.Append(",");
							SB.Append(TileAndLocation.TileCoordinates.y);
							SB.Append(",");
							SB.Append(TileAndLocation.TileCoordinates.z);
							SB.Append(LayerChar);
							SB.Append((int) Layer.Key.LayerType);

							int Index = CommonLayerTiles.IndexOf(TileAndLocation.Tile);


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

							Index = CommonMatrix4x4.IndexOf(TileAndLocation.TransformMatrix);
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
				TileMapData.CommonLayerTiles.Add(layerTile.name + LayerChar + (int) layerTile.TileType);
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


		public static ObjectMapData SaveObjects(MetaTileMap MetaTileMap, Vector3? Localboundarie1 = null,
			Vector3? Localboundarie2 = null, bool UseInstance = false)
		{
			bool UseBoundary = Localboundarie1 != null;
			ObjectMapData ObjectMapData = new ObjectMapData();
			ObjectMapData.PrefabData = new List<PrefabData>();
			foreach (var Object in MetaTileMap.ObjectLayer.GetTileList(CustomNetworkManager.Instance._isServer)
				.AllObjects)
			{
				if (UseBoundary)
				{
					if (IsPointWithin(Localboundarie1.Value, Localboundarie2.Value, Object.transform.localPosition) ==
					    false)
					{
						continue;
					}
				}

				ProcessIndividualObject(Object.gameObject, ObjectMapData, UseInstance: UseInstance);
			}

			foreach (var ObjectCoordinate in MetaTileMap.PresentMatrix.MetaDataLayer.InitialObjects)
			{
				if (UseBoundary)
				{
					if (IsPointWithin(Localboundarie1.Value, Localboundarie2.Value, ObjectCoordinate.Value) ==
					    false)
					{
						continue;
					}
				}

				ProcessIndividualObject(ObjectCoordinate.Key, ObjectMapData, ObjectCoordinate.Value, UseInstance);
			}

			return ObjectMapData;
		}

		public static void ProcessIndividualObject(GameObject Object, ObjectMapData ObjectMapData,
			Vector3? CoordinateOverride = null, bool UseInstance = false)
		{
			var RuntimeSpawned = Object.GetComponent<RuntimeSpawned>();
			if (RuntimeSpawned != null) return;

			PrefabData Prefab = new PrefabData();


			var Tracker = Object.GetComponent<PrefabTracker>();
			if (Tracker != null)
			{
				Prefab.PrefabID = Tracker.ForeverID;
				Prefab.ID = IDStatic;
				IDStatic++;
				Prefab.Name = Object.name;
				Prefab.Object = new IndividualObject();
				if (CoordinateOverride == null)
				{
					Prefab.LocalPRS = Math.Round(Object.transform.localPosition.x, 2) + "┼" +
					                  Math.Round(Object.transform.localPosition.y, 2) + "┼" +
					                  Math.Round(Object.transform.localPosition.z, 2) + "┼";

					if (Object.transform.localRotation.eulerAngles != Vector3.zero)
					{
						var Angles = Object.transform.localRotation.eulerAngles;
						Prefab.LocalPRS = Prefab.LocalPRS + Math.Round(Angles.x, 2) + "ø" +
						                  Math.Round(Angles.y, 2) + "ø" +
						                  Math.Round(Angles.z, 2) + "ø";
					}


					if (Object.transform.localScale != Vector3.one)
					{
						var Angles = Object.transform.localScale;
						Prefab.LocalPRS = Prefab.LocalPRS + Math.Round(Angles.x, 2) + "↔" +
						                  Math.Round(Angles.y, 2) + "↔" +
						                  Math.Round(Angles.z, 2) + "↔";
					}
				}
				else
				{
					var Position = CoordinateOverride.GetValueOrDefault(Vector3.zero);
					Prefab.LocalPRS = Math.Round(Position.x, 2) + "┼" +
					                  Math.Round(Position.y, 2) + "┼" +
					                  Math.Round(Position.z, 2) + "┼";
				}


				RecursiveSaveObject(Prefab, "0", Prefab.Object,
					CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[Tracker.ForeverID],
					Object.gameObject, ObjectMapData, CoordinateOverride, UseInstance);
				if (Prefab.Object.RemoveEmptys())
				{
					Prefab.Object = null;
				}

				ObjectMapData.PrefabData.Add(Prefab);
			}
		}

		public static void RecursiveSaveObject(PrefabData PrefabData, string ID, IndividualObject individualObject,
			GameObject PrefabEquivalent, GameObject gameObject, ObjectMapData ObjectMapData,
			Vector3? CoordinateOverride = null, bool UseInstance = false)
		{
			individualObject.ID = ID;
			//Compare classes here
			FillOutClassData(PrefabData, individualObject, PrefabEquivalent, gameObject, ObjectMapData,
				CoordinateOverride, UseInstance);


			if (PrefabEquivalent.transform.childCount != gameObject.transform.childCount)
			{
				// Logger.LogError("Mismatched children between Prefab " + PrefabEquivalent + " and game object " +
				// gameObject + " at " + gameObject.transform.localPosition +
				// "  Added children is not currently supported in This version of the map saver ");
			}

			for (int i = 0; i < PrefabEquivalent.transform.childCount; i++)
			{
				var newindividualObject = new IndividualObject();
				individualObject.Children.Add(newindividualObject);
				RecursiveSaveObject(PrefabData, ID + "," + i, newindividualObject,
					PrefabEquivalent.transform.GetChild(i).gameObject,
					gameObject.transform.GetChild(i).gameObject, ObjectMapData, UseInstance: UseInstance);
			}
		}

		public static void FillOutClassData(PrefabData PrefabData, IndividualObject individualObject,
			GameObject PrefabEquivalent, GameObject gameObject, ObjectMapData ObjectMapData,
			Vector3? CoordinateOverride = null, bool UseInstance = false)
		{
			Dictionary<string, int> ClassCount = new Dictionary<string, int>();
			var PrefabComponents = PrefabEquivalent.GetComponents<MonoBehaviour>().ToList();
			var gameObjectComponents = gameObject.GetComponents<MonoBehaviour>().ToList();

			for (int i = 0; i < PrefabComponents.Count; i++)
			{
				var PrefabMono = PrefabComponents[i];
				if (ClassCount.ContainsKey(PrefabMono.GetType().Name) == false)
					ClassCount[PrefabMono.GetType().Name] = 0;
				ClassCount[PrefabMono.GetType().Name]++;

				var objectContainer = gameObjectComponents[i] as ObjectContainer;
				if (objectContainer != null)
				{
					foreach (var objectBehaviour in objectContainer.GetStoredObjects().Select(obj => obj.GetComponent<ObjectBehaviour>()))
					{
						if (CoordinateOverride == null)
						{
							ProcessIndividualObject(objectBehaviour.gameObject, ObjectMapData,
								gameObject.transform.localPosition, UseInstance);
						}
						else
						{
							ProcessIndividualObject(objectBehaviour.gameObject, ObjectMapData,
								CoordinateOverride, UseInstance);
						}
					}
				}


				var itemStorage = gameObjectComponents[i] as ItemStorage;
				if (itemStorage != null)
				{
					foreach (var objectBehaviour in itemStorage.GetItemSlots())
					{
						if (objectBehaviour.Item == null) continue;
						if (CoordinateOverride == null)
						{
							ProcessIndividualObject(objectBehaviour.Item.gameObject, ObjectMapData,
								gameObject.transform.localPosition, UseInstance);
						}
						else
						{
							ProcessIndividualObject(objectBehaviour.Item.gameObject, ObjectMapData,
								CoordinateOverride, UseInstance);
						}
					}
				}


				var OutClass = new ClassData();
				OutClass.ClassID = PrefabMono.GetType().Name + "@" + ClassCount[PrefabMono.GetType().Name];
				MonoToID[gameObjectComponents[i]] = PrefabData.ID + "@" + individualObject.ID + "@" + OutClass.ClassID;
				RecursiveSearchData(OutClass, "", PrefabMono, gameObjectComponents[i], UseInstance);

				if (OutClass.IsEmpty() == false)
				{
					individualObject.ClassDatas.Add(OutClass);
				}
			}
		}


		public static void RecursiveSearchData(ClassData ClassData, string Prefix, object PrefabInstance,
			object SpawnedInstance, bool UseInstance = false)
		{
			var TypeMono = PrefabInstance.GetType();
			var coolFields = TypeMono.GetFields(
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic |
				BindingFlags.FlattenHierarchy
			).ToList();

			foreach (var Field in coolFields)
			{
				if (Field.IsPrivate || Field.IsAssembly || Field.IsFamily)
				{
					var attribute = Field.GetCustomAttributes(typeof(SerializeField), true);
					if (attribute.Length == 0)
					{
						continue;
					}

					attribute = Field.GetCustomAttributes(typeof(HideInInspector), true);
					if (attribute.Length > 0)
					{
						continue;
					}

					attribute = Field.GetCustomAttributes(typeof(NaughtyAttributes.ReadOnlyAttribute), true);
					if (attribute.Length > 0)
					{
						continue;
					}
				}
				else if (Field.IsPublic)
				{
					if (Field.IsNotSerialized)
					{
						continue;
					}

					var attribute = Field.GetCustomAttributes(typeof(HideInInspector), true);
					if (attribute.Length > 0)
					{
						continue;
					}

					attribute = Field.GetCustomAttributes(typeof(NaughtyAttributes.ReadOnlyAttribute), true);
					if (attribute.Length > 0)
					{
						continue;
					}
				}

				if (!Field.FieldType.IsValueType && !(Field.FieldType == typeof(string)))
				{
					var APrefabDefault = Field.GetValue(PrefabInstance);
					var AMonoSet = Field.GetValue(SpawnedInstance);

					IEnumerable list = null;
					var Coolattribute = Field.GetCustomAttributes(typeof(SceneObjectReference), true);
					if (Coolattribute.Length > 0)
					{
						if (Field.FieldType.IsGenericType)
						{
							list = AMonoSet as IEnumerable;
							if (list != null)
							{
								bool isListCompatible = false;
								foreach (var Item in list)
								{
									if (Item == null) continue;
									if (Item.GetType().IsSubclassOf(typeof(UnityEngine.Object)) &&
									    Item is MonoBehaviour)
									{
										isListCompatible = true;
									}

									break;
								}

								if (isListCompatible)
								{
									var fieldData = new FieldData();
									fieldData.Name = Prefix + Field.Name;
									foreach (var Item in list)
									{
										var mono = Item as MonoBehaviour;
										if (mono == null) continue;
										
										PopulateIDRelation(ClassData, fieldData, mono, UseInstance);
									}
								}

								continue;
							}
						}

						if (Field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)))
						{
							var mono = Field.GetValue(SpawnedInstance) as MonoBehaviour;
							if (mono == null) continue;
							var fieldData = new FieldData();
							fieldData.Name = Prefix + Field.Name;
							PopulateIDRelation(ClassData, fieldData, mono, UseInstance);
						}

						continue;
					}

					//if Field is a class and is not related to unity engine.object Serialise it
					if (Field.FieldType.IsSubclassOf(typeof(UnityEngine.Object))) continue;

					if (APrefabDefault != null && AMonoSet != null)
					{
						RecursiveSearchData(ClassData, Prefix + Field.Name + "@", APrefabDefault, AMonoSet,
							UseInstance);
						continue;
					}
				}

				if (Field.FieldType.IsGenericType && Field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
				) continue; //skipping all dictionaries For now
				if (Field.FieldType == typeof(System.Action)) continue;


				var PrefabDefault = Field.GetValue(PrefabInstance);
				var MonoSet = Field.GetValue(SpawnedInstance);

				var selfValueComparer = PrefabDefault as IComparable;
				bool areSame;
				if (PrefabDefault == null && MonoSet == null)
				{
					areSame = true;
				}
				else if ((PrefabDefault == null && MonoSet != null) || (PrefabDefault != null && MonoSet == null))
				{
					areSame = false; //One is null and the other wasn't
				}
				else if (selfValueComparer != null && selfValueComparer.CompareTo(MonoSet) != 0)
				{
					areSame = false; //the comparison using IComparable failed
				}
				else if (PrefabDefault.Equals(MonoSet) == false)
				{
					areSame = false; //Using the overridden one
				}
				else if (!object.Equals(PrefabDefault, MonoSet))
				{
					areSame = false; //Using the Inbuilt one
				}
				else
				{
					areSame = true; // match
				}


				if (areSame == false)
				{
					FieldData fieldData = new FieldData();
					fieldData.Name = Prefix + Field.Name;
					fieldData.Data = MonoSet.ToString();
					ClassData.Data.Add(fieldData);
				}
				//if is a Variables inside of the class will be flattened with field name of class@Field name
				//Better if recursiveThrough the class

				//Don't do sub- variables in struct
				//If it is a class,
				//Is class Is thing thing,
				//and then Just repeat the loop but within that class with the added notation
			}
		}

		public static void PopulateIDRelation(ClassData ClassData, FieldData fieldData, MonoBehaviour mono,
			bool UseInstance = false)
		{
			if (UseInstance)
			{
				fieldData.AddRuntimeReference(mono);
			}

			if (MonoToID.ContainsKey(mono))
			{
				fieldData.AddID(MonoToID[mono]);
				fieldData.RemoveRuntimeReference(mono);
			}
			else
			{
				UnserialisedObjectReferences.Add(new Tuple<MonoBehaviour, FieldData>(mono, fieldData));
			}

			FieldsToRefresh.Add(fieldData);
			ClassData.Data.Add(fieldData);
		}
	}
}