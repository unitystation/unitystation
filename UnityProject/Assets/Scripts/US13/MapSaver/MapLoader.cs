using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Logs;
using Mirror;
using Newtonsoft.Json;
using SecureStuff;
using UnityEngine;
using US13.Core.Initialisation;
using US13.Core.Lifecycle;
using US13.Managers;
using US13.Managers.MatrixManager;
using US13.Managers.NetworkManagement;
using US13.Managers.SubSceneManager;
using US13.Messages.Server.Mapping;
using US13.Tilemaps;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Tiles;
using US13.Tilemaps.Utils;
using US13.Variable_Viewer;
using Util;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.MapSaver
{
	public static class MapLoader
	{
		// Reference resolution uses shared state across all matrices in a map.
		private static readonly SemaphoreSlim loadGate = new SemaphoreSlim(1, 1);
		public static bool IsLoadingMap => loadGate.CurrentCount == 0;
		public readonly struct LoadingProgress
		{
			public readonly string Stage;
			public readonly string Context;
			public readonly int Completed;
			public readonly int Total;
			public float Fraction => Total > 0 ? Mathf.Clamp01((float)Completed / Total) : 0f;
			public LoadingProgress(string stage, string context, int completed, int total)
			{
				Stage = stage; Context = context; Completed = completed; Total = total;
			}
		}

		private sealed class LoadBatch
		{
			private readonly System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
			private readonly Action<LoadingProgress> progress;
			private readonly string stage, context;
			private readonly int total;
			private readonly CancellationToken token;
			private int completed;
			public LoadBatch(string stage, string context, int total, Action<LoadingProgress> progress, CancellationToken token)
			{
				this.stage = stage; this.context = context; this.total = total; this.progress = progress; this.token = token;
				progress?.Invoke(new LoadingProgress(stage, context, 0, total));
			}
			public async UniTask Step()
			{
				token.ThrowIfCancellationRequested();
				completed++;
				if (watch.ElapsedMilliseconds < 5 && completed < total) return;
				progress?.Invoke(new LoadingProgress(stage, context, completed, total));
				// Spawn the next batch after gameplay updates. Objects created in an Update
				// continuation otherwise reach UpdateManager before Unity has run Start.
				if (Application.isPlaying)
					await UniTask.NextFrame(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: token);
				watch.Restart();
			}
		}

		//TODO Gameobject references?
		//TODO is missing  Cross make such IDs when List!!

		//TODO Children rotation scale and position?
		//TODO Multiple components?
		//TODO ACU not set? Test

		public static async UniTask ProcessorCompactTileMapData(Matrix Matrix, Vector3Int Offset00, Vector3Int Offset,
			MapSaver.CompactTileMapData CompactTileMapData, HashSet<LayerType> LoadLayers = null, Action<LoadingProgress> progress = null, CancellationToken cancellationToken = default)
		{
			var CommonMatrix4x4 = new List<Matrix4x4>();

			foreach (var Matrix4x4 in CompactTileMapData.CommonMatrix4x4)
			{
				CommonMatrix4x4.Add(MapSaver.StringToMatrix4X4(Matrix4x4));
			}

			var CommonColours = new List<Color>();
			foreach (var Colour in CompactTileMapData.CommonColours)
			{
				ColorUtility.TryParseHtmlString("#" + Colour, out var IndividualColour);
				CommonColours.Add(IndividualColour);
			}

			var CommonLayerTiles = new List<LayerTile>();

			foreach (var TileName in CompactTileMapData.CommonLayerTiles)
			{
				CommonLayerTiles.Add(TileManager.GetTile(TileName));
			}


			var TileLocations = CompactTileMapData.Data.Split("@", StringSplitOptions.RemoveEmptyEntries);
			var batch = new LoadBatch("Tiles processed", Matrix.name, TileLocations.Length, progress, cancellationToken);

			foreach (var TileData in TileLocations)
			{
				cancellationToken.ThrowIfCancellationRequested();
				try
				{
					//74,91,0☰0§2 Example
					var Positions = TileData.Split(",");
					Vector3Int Position = Vector3Int.zero;

					try
					{
						Position = new Vector3Int(int.Parse(Positions[0], CultureInfo.InvariantCulture), int.Parse(Positions[1], CultureInfo.InvariantCulture),
							int.Parse(Positions[2], CultureInfo.InvariantCulture));
					}
					catch (Exception e)
					{
						Loggy.Error(e.ToString());
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

					try
					{
						Matrix.MetaTileMap.SetTile(Position, Tel, Matrix4x4, Colour, Application.isPlaying, true);
					}
					catch (Exception e)
					{
						try
						{
							Matrix.MetaTileMap.SetTile(Position, TileManager.Instance.ErrorTile, Matrix4x4, Colour, Application.isPlaying, true);
						}
						catch (Exception exception)
						{
							Loggy.Error(exception.ToString());
						}
						Loggy.Error(e.ToString());
					}

				}
				finally
				{
					await batch.Step();
				}
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
							return int.Parse(Substring, CultureInfo.InvariantCulture);
							;
						}
						catch (Exception e)
						{
							Loggy.Error(e.ToString());
							return 1;
						}
					}
				}

				var Substring2 = data[StartTileID..]; //Fancy new Syntax for basically substring
				return int.Parse(Substring2, CultureInfo.InvariantCulture);
			}
			else
			{
				return null;
			}
		}


		public static async UniTask ProcessorGitFriendlyTiles(Matrix Matrix, Vector3Int Offset00, Vector3Int Offset,
			MapSaver.GitFriendlyTileMapData GitFriendlyTileMapData, HashSet<LayerType> LoadLayers = null, Action<LoadingProgress> progress = null, CancellationToken cancellationToken = default)
		{
			var batch = new LoadBatch("Tile positions processed", Matrix.name, GitFriendlyTileMapData.XYs.Count, progress, cancellationToken);
			foreach (var XY in GitFriendlyTileMapData.XYs)
			{
				cancellationToken.ThrowIfCancellationRequested();
				try
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
						try
						{
							var Tel = TileManager.GetTile(Tile.Tel);

							if (Tel == null)
							{
								Loggy.Error($"Unable to find tile with name > {Tile.Tel}");
							}

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

							Matrix.MetaTileMap.SetTile(NewPos, Tel, Matrix4x4, Colour, Application.isPlaying,
								useExactForMultilayer: true);
						}
						catch (Exception e)
						{
							Loggy.Error(e.ToString());
						}
					}

				}
				finally
				{
					await batch.Step();
				}
			}
		}

		public static async UniTask ProcessorCompactObjectMapData(Matrix Matrix, Vector3Int Offset00, Vector3Int Offset,
			MapSaver.CompactObjectMapData CompactObjectMapData, bool LoadingMultiple = false, Action<LoadingProgress> progress = null, CancellationToken cancellationToken = default)
		{
			if (Application.isPlaying)
				await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken);
			if (LoadingMultiple == false)
			{
				MapSaver.CodeClass.ThisCodeClass.Reset(); //TODO Some of the functionality could be handled here
			}


			var batch = new LoadBatch("Objects processed", Matrix.name, CompactObjectMapData.PrefabData.Count, progress, cancellationToken);
			foreach (var prefabData in CompactObjectMapData.PrefabData)
			{
				cancellationToken.ThrowIfCancellationRequested();
				try
				{
					try
					{
						ProcessIndividualObject(CompactObjectMapData, prefabData, Matrix, Offset00, Offset);
					}
					catch (Exception e)
					{
						Loggy.Error(e.ToString());
					}

				}
				finally
				{
					await batch.Step();
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
					PrefabID = CompactObjectMapData.CommonPrefabs[int.Parse(prefabData.PrefabID, CultureInfo.InvariantCulture)];
				}

				position += Offset00;
				position += Offset;
#if UNITY_EDITOR
				if (Application.isPlaying == false)
				{
					if (CommonManagerEditorOnly.Instance.CustomNetworkManagerPrefab.ForeverIDLookupSpawnablePrefabs
						    .Count == 0)
					{
						CommonManagerEditorOnly.Instance.CustomNetworkManagerPrefab.ForeverIDLookupSpawnablePrefabs
							.Clear();
						CommonManagerEditorOnly.Instance.CustomNetworkManagerPrefab
							.SetUpSpawnablePrefabsForEverIDManual();
						CustomNetworkManager.Instance = CommonManagerEditorOnly.Instance.CustomNetworkManagerPrefab;
					}

					ReddySpawned = true;
					Object = (GameObject) UnityEditor.PrefabUtility.InstantiatePrefab(
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
			if (Matrix != null)
			{
				Object.transform.SetParent(Matrix.MetaTileMap.ObjectLayer.transform);
			}

			if (string.IsNullOrEmpty(prefabData.Name) == false)
			{
				Object.name = prefabData.Name;
			}

			if (string.IsNullOrEmpty(prefabData.SortPath) == false)
			{
				Transform Parent = null;
				Parent = Matrix.MetaTileMap.ObjectLayer.transform;
				Transform Child = null;

				foreach (var step in prefabData.SortPath.Split("/"))
				{
					Child = Parent.Find(step);
					// for (int i = 0; i < Child.childCount; i++)
					// {
					// 	Transform pchild = Child.GetChild(i);
					// 	if (pchild.name == step)
					// 	{
					// 		Child = pchild;
					// 		break;
					// 	}
					// }
					if (Child == null)
					{
						Child = new GameObject(step).transform;
						Child.SetParent(Parent);
						Child.localPosition = Vector3.zero;
						Child.localScale = Vector3.one;
						Child.rotation = Quaternion.identity;
					}

					Parent = Child;
				}

				Object.transform.SetParent(Child);
			}

			Object.gameObject.SetActive(prefabData.Active);

			var ID = prefabData.GitID;

			if (string.IsNullOrEmpty(ID))
			{
				ID = prefabData.ID.ToString(CultureInfo.InvariantCulture);
			}

			MapSaver.CodeClass.ThisCodeClass.Objects[ID] = Object;
			ProcessClassData(prefabData, Object, prefabData.Object);

			if (CustomNetworkManager.IsServer)
			{
				CompactObjectMapData.IDToNetIDClient[ID] = Object.GetComponent<NetworkIdentity>().netId;
			}

			if (MapSaver.CodeClass.ThisCodeClass.FinishLoading(ID, SpawnResult, Object))
			{
				foreach (var Spawn in Object.GetComponentsInChildren<INewMappedOnSpawn>())
				{
					Spawn.OnNewMappedOnSpawn();
				}

				if (SpawnResult != null)
				{
					Spawn._ServerFireClientServerSpawnHooks(SpawnResult,
						SpawnInfo.IsJsonMapped(SpawnResult.GameObject));
				}
			}
		}

		public static int GetNumberOfRequireComponents(string ClassID, GameObject Object)
		{
			var ClassComponents = ClassID.Split("@"); //TODO Multiple components?
			var Component = Object.GetComponent(ClassComponents[0]);

			if (Component == null)
			{
				return 0;
			}


			var Attributes = SecureStuff.AllowedReflection.GetNumberOfAttributeRequireComponent(Component.GetType());

			return Attributes;
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

			if (IndividualObject.ObjectLayer != null)
			{
				Object.layer = IndividualObject.ObjectLayer.Value;
			}

			if (string.IsNullOrEmpty(IndividualObject.LocalPRS) == false)
			{
				MapSaver.StringToPRS(Object, IndividualObject.LocalPRS);
			}

			var Removals = IndividualObject.ClassDatas.Where(x => x.Removed)
				.OrderByDescending(x => GetNumberOfRequireComponents(x.ClassID, Object)).ToList();

			foreach (var Remove in Removals)
			{
				var ClassComponents = Remove.ClassID.Split("@"); //TODO Multiple components?
				var Component = Object.GetComponent(ClassComponents[0]);
				if (Component == null) continue;

				if (Application.isPlaying)
				{
					UnityEngine.Object.Destroy(Component);
				}
				else
				{
					UnityEngine.Object.DestroyImmediate(Component);
				}
			}


			foreach (var classData in IndividualObject.ClassDatas)
			{
				if (classData.Removed == true)
				{
					continue;
				}

				var ClassComponents = classData.ClassID.Split("@"); //TODO Multiple components?
				var Component = Object.GetComponent(ClassComponents[0]);
				if (Component == null && classData.Removed == false)
				{
					try
					{
						Type newComponentToAdd = AllowedReflection.GetTypeByName(ClassComponents[0]);
						if (newComponentToAdd == null)
						{
							Loggy.Error($"{Object.name}: {ClassComponents[0]} does not exist anymore, or has been renamed.");
							continue;
						}
						Component = Object.AddComponent(newComponentToAdd);
					}
					catch (Exception e)
					{
						Loggy.Error($"Could not add a component on a gameObject.\n {e}");
						continue;
					}
				}

				var ID = prefabData.GitID;

				if (string.IsNullOrEmpty(ID))
				{
					ID = prefabData.ID.ToString(CultureInfo.InvariantCulture);
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

					var Id = int.Parse(Child.ID.Split(",").Last(), CultureInfo.InvariantCulture);
					while (Object.transform.childCount <= Id)
					{
						var NewChild = new GameObject();
						NewChild.transform.SetParent(Object.transform);
						NewChild.transform.localPosition = Vector3.zero;
						NewChild.transform.localScale = Vector3.one;
						NewChild.transform.rotation = Quaternion.identity;
					}



					var ObjectChild = Object.transform.GetChild(Id);
					if (Child.Removed)
					{
						if (Application.isPlaying)
						{
							UnityEngine.Object.DestroyImmediate(ObjectChild.gameObject);
						}
						else
						{
							UnityEngine.Object.DestroyImmediate(ObjectChild.gameObject);
						}
					}
					else
					{
						ProcessClassData(prefabData, ObjectChild.gameObject, Child);
					}
				}
			}
		}

		public static void ServerLoadMapNoCoRoutine(Vector3 Offset00, Vector3 Offset, MapSaver.MapData MapData,
			SceneType sceneType = SceneType.HiddenScene, HashSet<LayerType> LoadLayers = null,
			bool LoadObjects = true)
		{
			ServerLoadMap(Offset00, Offset, MapData, sceneType, LoadLayers, LoadObjects).Forget(e => Logs.Loggy.Error(e.ToString()));
		}


		public static async UniTask ServerLoadMap(Vector3 Offset00, Vector3 Offset, MapSaver.MapData MapData,
			SceneType sceneType = SceneType.HiddenScene, HashSet<LayerType> LoadLayers = null,
			bool LoadObjects = true, bool TestLoad = false, Action<LoadingProgress> progress = null, CancellationToken cancellationToken = default)
		{
			await loadGate.WaitAsync(cancellationToken);
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				foreach (var ToMapMatrix in MapData.ContainedMatrices)
				{
					await ServerLoadSectionCore(null, Offset00, Offset, ToMapMatrix, null,
						MatrixName: ToMapMatrix.MatrixName, LoadingMultiple: true, sceneType: sceneType, LoadLayers : LoadLayers,LoadObjects :LoadObjects, TestLoad : TestLoad, progress: progress, cancellationToken: cancellationToken);
				}

				MapSaver.CodeClass.ThisCodeClass.ReportStatus();
			}
			finally
			{
				MapSaver.CodeClass.ThisCodeClass.Reset();
				loadGate.Release();
			}

#if UNITY_EDITOR
			if (Application.isPlaying == false && CustomNetworkManager.Instance != null)
			{
				CustomNetworkManager.Instance.SpawnedByMappingTool = true;
			}
#endif
		}

		public static void ServerLoadSectionNoCoRoutine(MatrixInfo Matrix, Vector3 Offset00, Vector3 Offset,
			MapSaver.MatrixData MatrixData, Action completeAction, HashSet<LayerType> LoadLayers = null,
			bool LoadObjects = true, string MatrixName = null)
		{
			ServerLoadSection(Matrix, Offset00, Offset, MatrixData, completeAction,
				LoadLayers, LoadObjects, MatrixName, false).Forget(e => Logs.Loggy.Error(e.ToString()));
		}

		//Offset00 the off set In the data so objects will appear at 0,0
		//Offset to apply 0,0 to get the position you want
		public static async UniTask ServerLoadSection(MatrixInfo Matrix, Vector3 Offset00, Vector3 Offset,
			MapSaver.MatrixData MatrixData, Action completeAction, HashSet<LayerType> LoadLayers = null,
			bool LoadObjects = true, string MatrixName = null, bool LoadingMultiple = false,
			SceneType sceneType = SceneType.HiddenScene, bool TestLoad = false, Action<LoadingProgress> progress = null, CancellationToken cancellationToken = default)
		{
			await loadGate.WaitAsync(cancellationToken);
			try
			{
				await ServerLoadSectionCore(Matrix, Offset00, Offset, MatrixData, completeAction, LoadLayers,
					LoadObjects, MatrixName, LoadingMultiple, sceneType, TestLoad, progress, cancellationToken);
			}
			finally
			{
				if (!LoadingMultiple) MapSaver.CodeClass.ThisCodeClass.Reset();
				loadGate.Release();
			}
		}

		private static async UniTask ServerLoadSectionCore(MatrixInfo Matrix, Vector3 Offset00, Vector3 Offset,
			MapSaver.MatrixData MatrixData, Action completeAction, HashSet<LayerType> LoadLayers = null,
			bool LoadObjects = true, string MatrixName = null, bool LoadingMultiple = false,
			SceneType sceneType = SceneType.HiddenScene, bool TestLoad = false, Action<LoadingProgress> progress = null, CancellationToken cancellationToken = default)
		{
			try
			{
#if UNITY_EDITOR
				if (Application.isPlaying == false)
				{
					if (TestLoad)
					{
						VariableViewerManager.TestLoad = TestLoad;
					}

					(CommonManagerEditorOnly.Instance.VariableViewerManager as IInitialise).Initialise();
					TileManager.Instance = CommonManagerEditorOnly.Instance.TileManager;
					if (CommonManagerEditorOnly.Instance.TileManager.AllTiles.Count == 0)
					{
						await CommonManagerEditorOnly.Instance.TileManager.LoadAllTiles();
					}
				}
			#endif

				cancellationToken.ThrowIfCancellationRequested();
				progress?.Invoke(new LoadingProgress("Creating matrix", MatrixName, 0, 0));
				Matrix aaMatrix = null;
				try
				{
					if (Matrix == null)
					{
						aaMatrix = MatrixManager.MakeNewMatrix(MatrixName, sceneType);
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

							if (Offset.magnitude > 0)
							{
								aaMatrix.MatrixMove.NetworkedMatrixMove.transform.parent.gameObject.transform
									.localPosition = Offset;
								Offset = Vector3.zero;
							}

							aaMatrix.NetworkedMatrix.IsJsonLoaded = false;
						}
						else
						{
							MapSaver.StringToPRS(aaMatrix.MatrixMove.NetworkedMatrixMove.TargetTransform.gameObject,
								MatrixData.Location);
							aaMatrix.MatrixMove.NetworkedMatrixMove.SetTransformPosition(aaMatrix.MatrixMove
								.NetworkedMatrixMove
								.TargetTransform.position);

							if (Offset.magnitude > 0)
							{
								aaMatrix.MatrixMove.NetworkedMatrixMove.transform.parent.gameObject.transform
									.localPosition = Offset;
								Offset = Vector3.zero;
							}
						}
					}
					else
					{
						aaMatrix = Matrix.Matrix;
					}


					//TODO MapSaver.CodeClass.ThisCodeClass?? Clearing?
					if (MatrixData.CompactTileMapData != null)
					{
						await ProcessorCompactTileMapData(aaMatrix, Offset00.RoundToInt(), Offset.RoundToInt(),
							MatrixData.CompactTileMapData, LoadLayers, progress, cancellationToken);
					}

					if (MatrixData.GitFriendlyTileMapData != null)
					{
						await ProcessorGitFriendlyTiles(aaMatrix, Offset00.RoundToInt(), Offset.RoundToInt(),
							MatrixData.GitFriendlyTileMapData, LoadLayers, progress, cancellationToken);
					}
				}
				catch (OperationCanceledException) { throw; }
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
				}


				if (aaMatrix == null) throw new InvalidOperationException("Unable to create map matrix.");
				bool CheckState = true;
				while (CheckState)
				{
					cancellationToken.ThrowIfCancellationRequested();
					lock (aaMatrix.MetaTileMap.QueuedChanges)
					{
						progress?.Invoke(new LoadingProgress("Tile changes remaining", MatrixName, aaMatrix.MetaTileMap.QueuedChanges.Count, 0));
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
						await UniTask.NextFrame(cancellationToken: cancellationToken);
					}
				}


				try
				{
					if (MatrixData.CompactObjectMapData != null && LoadObjects)
					{
						await ProcessorCompactObjectMapData(aaMatrix, Offset00.RoundToInt(), Offset.RoundToInt(),
							MatrixData.CompactObjectMapData, LoadingMultiple: LoadingMultiple, progress: progress, cancellationToken: cancellationToken); //TODO Handle GitID better
					}

					if (LoadObjects)
					{
						if (Application.isPlaying == false) return;
						progress?.Invoke(new LoadingProgress("Publishing object data", MatrixName, 0, 0));
						var newdata = JsonConvert.SerializeObject(MatrixData.CompactObjectMapData);
						CustomNetworkManager.Instance.LoadedMapDatas.Add(new Tuple<string, int>(newdata, aaMatrix.Id));
						ServerReturnMapData.SendAll(newdata, ServerReturnMapData.MessageType.MapDataForClient, true,
							aaMatrix.Id);
					}
				}
				catch (OperationCanceledException) { throw; }
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
				}

				if (aaMatrix.NetworkedMatrix.RequestInitialiseMapLoader && Application.isPlaying)
				{
					progress?.Invoke(new LoadingProgress("Initializing matrix", MatrixName, 0, 0));
					await aaMatrix.MatrixInitialization(cancellationToken);
				}

				try
				{
					completeAction?.Invoke();
				}
				catch (OperationCanceledException) { throw; }
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
				}

#if UNITY_EDITOR
				if (Application.isPlaying == false)
				{
					if (TestLoad)
					{
						VariableViewerManager.TestLoad = false;
					}
				}
#endif
			}
			finally
			{
#if UNITY_EDITOR
				if (!Application.isPlaying && TestLoad) VariableViewerManager.TestLoad = false;
#endif
			}

		}
	}
}
