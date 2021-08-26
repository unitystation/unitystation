using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TileManagement;
using UnityEngine;
using System.Linq;
using System.Reflection;
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

		//Prefab ID -> Game object ID -> Component ID
		//

		public class ObjectMapData
		{
			public List<PrefabData> PrefabData;
		}

		public class PrefabData
		{
			public ulong ID; //is good
			public string PrefabID;
			public string Name;
			public string LocalPosition; //TODO Needs scale and rotation to
			public IndividualObject Object;
		}


		public class IndividualObject
		{
			public uint ChildLocation;
			public string ID; //Child index, Child index,  Child index,
			public string Name;
			public List<ClassData> ClassDatas = new List<ClassData>();
			public List<IndividualObject> Children = new List<IndividualObject>();

			public bool RemoveEmptys()
			{
				bool ISEmpty = true;

				if (string.IsNullOrEmpty(Name) == false)
				{
					ISEmpty = false;
				}

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
			public List<FieldData> Data = new List<FieldData>();

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
					Prefab.LocalPosition = Math.Round(Object.transform.localPosition.x, 2) + "," +
					                       Math.Round(Object.transform.localPosition.z, 2) +
					                       "," + Math.Round(Object.transform.localPosition.y, 2);
					RecursiveSaveObject("0", Prefab.Object,
						CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[Tracker.ForeverID],
						Object.gameObject);
					if (Prefab.Object.RemoveEmptys())
					{
						Prefab.Object = null;
					}

					ObjectMapData.PrefabData.Add(Prefab);
				}
			}

			return ObjectMapData;
		}

		public static void RecursiveSaveObject(string ID, IndividualObject individualObject,
			GameObject PrefabEquivalent, GameObject gameObject)
		{
			if (gameObject.name != PrefabEquivalent.name)
			{
				individualObject.Name = gameObject.name;
			}

			individualObject.ID = ID;
			//Compare classes here
			FillOutClassData(individualObject, PrefabEquivalent, gameObject);


			if (PrefabEquivalent.transform.childCount != gameObject.transform.childCount)
			{
				Logger.LogError("Mismatched children between Prefab " + PrefabEquivalent + " and game object " +
				                gameObject + " at " + gameObject.transform.localPosition +
				                "  Added children is not currently supported in This version of the map saver ");
			}

			for (int i = 0; i < PrefabEquivalent.transform.childCount; i++)
			{
				var newindividualObject = new IndividualObject();
				individualObject.Children.Add(newindividualObject);
				RecursiveSaveObject(ID + "," + i, newindividualObject,
					PrefabEquivalent.transform.GetChild(i).gameObject,
					gameObject.transform.GetChild(i).gameObject);
			}
		}

		public static Dictionary<string, int> ClassCount = new Dictionary<string, int>();

		public static void FillOutClassData(IndividualObject individualObject,
			GameObject PrefabEquivalent, GameObject gameObject)
		{
			ClassCount.Clear();
			var PrefabComponents = PrefabEquivalent.GetComponents<MonoBehaviour>().ToList();
			var gameObjectComponents = gameObject.GetComponents<MonoBehaviour>().ToList();

			for (int i = 0; i < PrefabComponents.Count; i++)
			{
				var PrefabMono = PrefabComponents[i];
				if (ClassCount.ContainsKey(PrefabMono.GetType().Name) == false)
					ClassCount[PrefabMono.GetType().Name] = 0;
				ClassCount[PrefabMono.GetType().Name]++;

				var OutClass = new ClassData();
				OutClass.ClassID = PrefabMono.GetType().Name + "@" + ClassCount[PrefabMono.GetType().Name];

				RecursiveSearchData(OutClass, "", PrefabMono, gameObjectComponents[i]);

				if (OutClass.IsEmpty() ==false)
				{
					individualObject.ClassDatas.Add(OutClass);
				}
			}
		}


		public static void RecursiveSearchData(ClassData ClassData, string Prefix, object PrefabInstance,
			object SpawnedInstance)
		{
			var TypeMono = PrefabInstance.GetType();
			var coolFields = ((TypeMono.GetFields(
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic |
				BindingFlags.FlattenHierarchy
			).ToList()));

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
				}

				if (!Field.FieldType.IsValueType && !(Field.FieldType == typeof(string)))
				{
					var APrefabDefault = Field.GetValue(PrefabInstance);
					var AMonoSet = Field.GetValue(SpawnedInstance);

					if ((Field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)))) continue; //Handle this with custom stuff

					if (APrefabDefault != null && AMonoSet != null)
					{
						RecursiveSearchData(ClassData, Prefix + "@" + Field.Name, APrefabDefault, AMonoSet);
						continue;
					}

				}

				if (Field.FieldType.IsGenericType && Field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) continue; //skipping all dictionaries For now

				//if Field is a class and is not related to unity engine.object Serialise it
				var PrefabDefault = Field.GetValue(PrefabInstance);
				var MonoSet = Field.GetValue(SpawnedInstance);

				var selfValueComparer = PrefabDefault as IComparable;
				bool AreSame;
				if (PrefabDefault == null && MonoSet == null)
					AreSame = true;
				else if ((PrefabDefault == null && MonoSet != null) || (PrefabDefault != null && MonoSet == null))
					AreSame = false; //One is null and the other wasn't
				else if (selfValueComparer != null && selfValueComparer.CompareTo(MonoSet) != 0)
					AreSame = false; //the comparison using IComparable failed
				else if (PrefabDefault.Equals(MonoSet) == false)
					AreSame = false; //Using the overridden one
				else if (!object.Equals(PrefabDefault, MonoSet))
					AreSame = false; //Using the Inbuilt one
				else
					AreSame = true; // match

				if (AreSame == false)
				{
					FieldData fieldData = new FieldData();
					fieldData.Name = Prefix + '@' + Field.Name;
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
	}
}