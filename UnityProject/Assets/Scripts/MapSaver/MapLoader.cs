using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Initialisation;
using Logs;
using Mirror;
using Newtonsoft.Json;
using SecureStuff;
using Tiles;
using UnityEngine;

namespace MapSaver
{
	public static class MapLoader
	{
		//TODO Gameobject references?
		//TODO is missing  Cross make such IDs when List!!

		//TODO Children rotation scale and position?
		//TODO Multiple components?
		//TODO ACU not set? Test

		public static void ProcessorCompactTileMapData(Matrix Matrix, Vector3Int Offset00, Vector3Int Offset,
			MapSaver.CompactTileMapData CompactTileMapData, HashSet<LayerType> LoadLayers = null)
		{
			var CommonMatrix4x4 = new List<Matrix4x4>();

			foreach (var Matrix4x4 in CompactTileMapData.CommonMatrix4x4)
			{
				CommonMatrix4x4.Add(MapSaver.StringToMatrix4X4(Matrix4x4));
			}

			var CommonColours = new List<Color>();
			foreach (var Colour in CompactTileMapData.CommonColours)
			{
				ColorUtility.TryParseHtmlString(Colour, out var IndividualColour);
				CommonColours.Add(IndividualColour);
			}

			var CommonLayerTiles = new List<LayerTile>();

			foreach (var TileName in CompactTileMapData.CommonLayerTiles)
			{
				CommonLayerTiles.Add(TileManager.GetTile(TileName));
			}


			var TileLocations = CompactTileMapData.Data.Split("@");

			foreach (var TileData in TileLocations)
			{
				if (string.IsNullOrEmpty(TileData)) continue;
				//74,91,0☰0§2 Example
				var Positions = TileData.Split(",");
				Vector3Int Position = Vector3Int.zero;

				try
				{
					Position = new Vector3Int(int.Parse(Positions[0]), int.Parse(Positions[1]),
						int.Parse(Positions[2]));
				}
				catch (Exception e)
				{
					Loggy.LogError(e.ToString());
					continue;
				}


				Position += Offset00;
				Position += Offset;

				var data = Positions[3];

				var TileID = GetDataForTag(data, MapSaver.TileIDChar);
				LayerTile Tel = null;
				if (TileID != null)
				{
					Tel = CommonLayerTiles[TileID.Value];
				}
				else
				{
					Tel = CommonLayerTiles[0];
				}

				if (LoadLayers != null && LoadLayers.Contains(Tel.LayerType) == false)
				{
					continue;
				}


				Color? Colour = null;
				var ColourID = GetDataForTag(data, MapSaver.ColourChar);
				if (ColourID != null)
				{
					Colour = CommonColours[ColourID.Value];
				}
				else
				{
					Colour = CommonColours[0];
				}

				Matrix4x4? Matrix4x4 = null;
				var MatrixID = GetDataForTag(data, MapSaver.Matrix4x4Char);
				if (MatrixID != null)
				{
					Matrix4x4 = CommonMatrix4x4[MatrixID.Value];
				}
				else
				{
					Matrix4x4 = CommonMatrix4x4[0];
				}

				Matrix.MetaTileMap.SetTile(Position, Tel, Matrix4x4, Colour, Application.isPlaying, true);
			}
		}


		private static int? GetDataForTag(string data, char Tag)
		{
			if (data.Contains(Tag))
			{
				var StartTileID = data.IndexOf(Tag) + 1;
				for (int i = StartTileID; i < data.Length; i++)
				{
					if (data[i] is MapSaver.Matrix4x4Char or MapSaver.TileIDChar or MapSaver.ColourChar
					    or MapSaver.LayerChar or MapSaver.LocationChar)
					{
						//is end
						var Substring = data[StartTileID..i]; //Fancy new Syntax for basically substring
						try
						{
							return int.Parse(Substring);
							;
						}
						catch (Exception e)
						{
							Loggy.LogError(e.ToString());
							return int.Parse("1");
						}
					}
				}

				var Substring2 = data[StartTileID..]; //Fancy new Syntax for basically substring
				return int.Parse(Substring2);
			}
			else
			{
				return null;
			}
		}


		public static void ProcessorGitFriendlyTiles(Matrix Matrix, Vector3Int Offset00, Vector3Int Offset,
			MapSaver.GitFriendlyTileMapData GitFriendlyTileMapData, HashSet<LayerType> LoadLayers = null)
		{
			foreach (var XY in GitFriendlyTileMapData.XYs)
			{
				var Pos = MapSaver.GitFriendlyPositionToVectorInt(XY.Key);
				Pos += Offset00;
				Pos += Offset;
				foreach (var Layer in Matrix.MetaTileMap.LayersKeys)
				{
					if (LoadLayers != null && LoadLayers.Contains(Layer) == false)
					{
						continue;
					}

					Matrix.MetaTileMap.RemoveTileWithlayer(Pos, Layer);
				}

				Matrix.MetaTileMap.RemoveAllOverlays(Pos, LayerType.Effects);

				foreach (var Tile in XY.Value)
				{
					var Tel = TileManager.GetTile(Tile.Tel);

					if (LoadLayers != null && LoadLayers.Contains(Tel.LayerType) == false)
					{
						continue;
					}

					var NewPos = Pos;

					if (Tile.Z != null)
					{
						NewPos.z = Tile.Z.Value;
					}

					Color? Colour = null;
					if (string.IsNullOrEmpty(Tile.Col) == false)
					{
						ColorUtility.TryParseHtmlString(Tile.Col, out var NonNullbleColour);
						Colour = NonNullbleColour;
					}

					Matrix4x4? Matrix4x4 = null;
					if (string.IsNullOrEmpty(Tile.Tf) == false)
					{
						Matrix4x4 = MapSaver.StringToMatrix4X4(Tile.Tf);
					}

					Matrix.MetaTileMap.SetTile(NewPos, Tel, Matrix4x4, Colour, Application.isPlaying, useExactForMultilayer : true);
				}
			}
		}

		public static void ProcessorCompactObjectMapData(Matrix Matrix, Vector3Int Offset00, Vector3Int Offset,
			MapSaver.CompactObjectMapData CompactObjectMapData, bool LoadingMultiple = false)
		{
			if (LoadingMultiple == false)
			{
				MapSaver.CodeClass.ThisCodeClass.Reset(); //TODO Some of the functionality could be handled here
			}


			foreach (var prefabData in CompactObjectMapData.PrefabData)
			{
				try
				{
					ProcessIndividualObject(CompactObjectMapData, prefabData, Matrix, Offset00, Offset);
				}
				catch (Exception e)
				{
					Loggy.LogError(e.ToString());
				}
			}


			if (LoadingMultiple == false)
			{
				MapSaver.CodeClass.ThisCodeClass.ReportStatus();
				MapSaver.CodeClass.ThisCodeClass.Reset(); //TODO Some of the functionality could be handled here
			}
		}


		public static void ProcessIndividualObject(MapSaver.CompactObjectMapData CompactObjectMapData,
			MapSaver.PrefabData prefabData, Matrix Matrix, Vector3Int Offset00, Vector3Int Offset,
			GameObject Object = null)
		{
			bool ReddySpawned = false;
			SpawnResult SpawnResult = null;
			if (Object == null)
			{
				MapSaver.StringToVector(prefabData.LocalPRS, out Vector3 position);
				string PrefabID = prefabData.PrefabID;
				if (CompactObjectMapData.CommonPrefabs.Count > 0)
				{
					PrefabID = CompactObjectMapData.CommonPrefabs[int.Parse(prefabData.PrefabID)];
				}

				position += Offset00;
				position += Offset;
#if UNITY_EDITOR
				if (Application.isPlaying == false)
				{
					if (CommonManagerEditorOnly.Instance.CustomNetworkManagerPrefab.ForeverIDLookupSpawnablePrefabs.Count == 0)
					{
						CommonManagerEditorOnly.Instance.CustomNetworkManagerPrefab.ForeverIDLookupSpawnablePrefabs.Clear();
						CommonManagerEditorOnly.Instance.CustomNetworkManagerPrefab.SetUpSpawnablePrefabsForEverIDManual();
						CustomNetworkManager.Instance = CommonManagerEditorOnly.Instance.CustomNetworkManagerPrefab;
					}




					ReddySpawned = true;
					Object =  (GameObject)  UnityEditor.PrefabUtility.InstantiatePrefab(
						CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[PrefabID],
						Matrix.MetaTileMap.ObjectLayer.transform);
					Object.transform.localPosition = position;
				}
#endif
				if (ReddySpawned == false)
				{
					SpawnResult = Spawn.ServerPrefab(
						CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[PrefabID],
						position.ToWorld(Matrix), AutoOnSpawnServerHook: false, mapspawn: true);

					Object = SpawnResult.GameObject;

				}

				MapSaver.StringToPRS(Object, prefabData.LocalPRS);
				Object.transform.localPosition += Offset00;
				Object.transform.localPosition += Offset;
				if (Application.isPlaying)
				{
					Object.GetComponent<UniversalObjectPhysics>()
						?.AppearAtWorldPositionServer(Object.transform.localPosition.ToWorld(Matrix),
							Matrixoveride: Matrix.MatrixInfo);
					Object.GetComponent<UniversalObjectPhysics>()?.SetMatrix(Matrix);
				}
			}

			Object.GetComponent<UniversalObjectPhysics>()?.ResetEverything();
			Object.transform.SetParent(Matrix.MetaTileMap.ObjectLayer.transform);
			if (string.IsNullOrEmpty(prefabData.Name) == false)
			{
				Object.name = prefabData.Name;
			}

			if (string.IsNullOrEmpty(prefabData.Name) == false)
			{
				Object.name = prefabData.Name;
			}

			var ID = prefabData.GitID;

			if (string.IsNullOrEmpty(ID))
			{
				ID = prefabData.PrefabID;
			}


			MapSaver.CodeClass.ThisCodeClass.Objects[ID] = Object;
			ProcessClassData(prefabData, Object, prefabData.Object);

			if (CustomNetworkManager.IsServer)
			{
				CompactObjectMapData.IDToNetIDClient[ID] = Object.GetComponent<NetworkIdentity>().netId;
			}

			if (MapSaver.CodeClass.ThisCodeClass.FinishLoading(ID, SpawnResult))
			{
				if (SpawnResult != null)
				{
					Spawn._ServerFireClientServerSpawnHooks(SpawnResult, SpawnInfo.Mapped(SpawnResult.GameObject));
				}
			}
		}

		public static void ProcessClassData(MapSaver.PrefabData prefabData, GameObject Object,
			MapSaver.IndividualObject IndividualObject)
		{
			if (IndividualObject == null)
			{
				return;
			}

			if (string.IsNullOrEmpty(IndividualObject.Name) == false)
			{
				Object.name = IndividualObject.Name;
			}

			foreach (var classData in IndividualObject.ClassDatas)
			{
				var ClassComponents = classData.ClassID.Split("@"); //TODO Multiple components?
				var Component = Object.GetComponent(ClassComponents[0]);
				if (Component == null)
				{
					try
					{
						Component = Object.AddComponent(AllowedReflection.GetTypeByName(ClassComponents[0]));
					}
					catch (Exception e)
					{
						Loggy.LogError(e.ToString());
						continue;
					}
				}

				var ID = prefabData.GitID;

				if (string.IsNullOrEmpty(ID))
				{
					ID = prefabData.PrefabID;
				}

				bool IsServer = CustomNetworkManager.IsServer;
#if UNITY_EDITOR
				if (Application.isPlaying == false)
				{
					IsServer = true;
				}
#endif
				SecureMapsSaver.LoadData(ID, Component, classData.Data, MapSaver.CodeClass.ThisCodeClass,
					IsServer);
			}

			if (IndividualObject.Children != null)
			{
				foreach (var Child in IndividualObject.Children)
				{
					var Id = int.Parse(Child.ID.Split(",").Last());
					while (Object.transform.childCount <= Id)
					{
						var NewChild = new GameObject();
						NewChild.transform.SetParent(Object.transform);
						NewChild.transform.localPosition = Vector3.zero;
						NewChild.transform.localScale = Vector3.one;
						NewChild.transform.rotation = Quaternion.identity;
					}

					var ObjectChild = Object.transform.GetChild(Id);
					ProcessClassData(prefabData, ObjectChild.gameObject, Child);
				}
			}
		}

		public static IEnumerator ServerLoadMap(Vector3 Offset00, Vector3 Offset, MapSaver.MapData MapData)
		{
			foreach (var ToMapMatrix in MapData.ContainedMatrices)
			{
				yield return ServerLoadSection(null, Offset00, Offset, ToMapMatrix, null,
					MatrixName: ToMapMatrix.MatrixName, LoadingMultiple: true);
			}

			MapSaver.CodeClass.ThisCodeClass.ReportStatus();

		}

		public static void ServerLoadSectionNoCoRoutine(MatrixInfo Matrix, Vector3 Offset00, Vector3 Offset,
			MapSaver.MatrixData MatrixData, Action completeAction, HashSet<LayerType> LoadLayers = null,
			bool LoadObjects = true, string MatrixName = null, bool LoadingMultiple = false)
		{
			GameManager.Instance.StartCoroutine(ServerLoadSection(Matrix, Offset00, Offset, MatrixData, completeAction,
				LoadLayers, LoadObjects, MatrixName, LoadingMultiple));
		}

		//Offset00 the off set In the data so objects will appear at 0,0
		//Offset to apply 0,0 to get the position you want
		public static IEnumerator ServerLoadSection(MatrixInfo Matrix, Vector3 Offset00, Vector3 Offset,
			MapSaver.MatrixData MatrixData, Action completeAction, HashSet<LayerType> LoadLayers = null,
			bool LoadObjects = true, string MatrixName = null, bool LoadingMultiple = false)
		{
#if UNITY_EDITOR
			if (Application.isPlaying == false)
			{
				(CommonManagerEditorOnly.Instance.VariableViewerManager as IInitialise).Initialise();
				TileManager.Instance = 	CommonManagerEditorOnly.Instance.TileManager;
				if (CommonManagerEditorOnly.Instance.TileManager.AllTiles.Count == 0)
				{
					var Load = CommonManagerEditorOnly.Instance.TileManager.LoadAllTiles();
					while (Load.MoveNext())
					{

					}
				}
			}
#endif

			Matrix aaMatrix = null;
			try
			{
				if (Matrix == null)
				{
					aaMatrix = MatrixManager.MakeNewMatrix(MatrixName);
#if UNITY_EDITOR
					if (Application.isPlaying == false)
					{
						aaMatrix.Awake();
					}
#endif

					if (Application.isPlaying == false)
					{
						MapSaver.StringToPRS(aaMatrix.MatrixMove.NetworkedMatrixMove.transform.parent.gameObject,
							MatrixData.Location);
					}
					else
					{
						MapSaver.StringToPRS(aaMatrix.MatrixMove.NetworkedMatrixMove.TargetTransform.gameObject,
							MatrixData.Location);
						aaMatrix.MatrixMove.NetworkedMatrixMove.SetTransformPosition(aaMatrix.MatrixMove.NetworkedMatrixMove
							.TargetTransform.position);
					}

				}
				else
				{
					aaMatrix = Matrix.Matrix;
				}

				//TODO MapSaver.CodeClass.ThisCodeClass?? Clearing?
				MapSaver.CompactObjectMapData data = null;
				if (MatrixData.CompactTileMapData != null)
				{
					ProcessorCompactTileMapData(aaMatrix, Offset00.RoundToInt(), Offset.RoundToInt(),
						MatrixData.CompactTileMapData, LoadLayers);
				}

				if (MatrixData.GitFriendlyTileMapData != null)
				{
					ProcessorGitFriendlyTiles(aaMatrix, Offset00.RoundToInt(), Offset.RoundToInt(),
						MatrixData.GitFriendlyTileMapData, LoadLayers);
				}
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
			}


			bool CheckState = true;
			while (CheckState)
			{
				lock (aaMatrix.MetaTileMap.QueuedChanges)
				{
					if (aaMatrix.MetaTileMap.QueuedChanges.Count > 0)
					{
						CheckState = true;
					}
					else
					{
						CheckState = false;
					}
				}

				if (Application.isPlaying == false)
				{
					CheckState = false;
				}
				else
				{
					yield return null;
				}
			}


			try
			{

				if (MatrixData.CompactObjectMapData != null && LoadObjects)
				{
					ProcessorCompactObjectMapData(aaMatrix, Offset00.RoundToInt(), Offset.RoundToInt(),
						MatrixData.CompactObjectMapData, LoadingMultiple: LoadingMultiple); //TODO Handle GitID better
				}

				if (LoadObjects)
				{
					if (Application.isPlaying == false) yield break;
					var newdata = JsonConvert.SerializeObject(MatrixData.CompactObjectMapData);
					CustomNetworkManager.LoadedMapDatas.Add(newdata);
					ServerReturnMapData.SendAll(newdata, ServerReturnMapData.MessageType.MapDataForClient, true);
				}

				completeAction?.Invoke();
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
			}
		}
	}
}