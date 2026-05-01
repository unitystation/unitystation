using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Logs;
using Newtonsoft.Json;
using SecureStuff;
using UnityEngine;
using US13.Managers.MatrixManager;
using US13.MapSaver;
using US13.Tilemaps.Behaviours.Layers;

namespace US13.Systems.MaintRooms
{
	[Serializable]
	public class WeightedBlueprintEntry
	{
		public string roomDirectory;
		public int weight;
		public int maxOccurrences;
		[HideInInspector] public int currentOccurrences;
	}

	[Serializable]
	public class WeightedMaintBlueprintEntry : WeightedBlueprintEntry
	{
		[SerializeField] public Directions doorDirections = Directions.South;
	}

	public class BlueprintList
	{
		public BlueprintList(List<WeightedBlueprintEntry> entries)
		{
			_list = entries;
			_totalWeight = 0;
			foreach (var entry in entries)
			{
				_totalWeight += entry.weight;
			}
		}
		public BlueprintList(List<WeightedMaintBlueprintEntry> entries)
		{

			_list = new List<WeightedBlueprintEntry>();
			_list.AddRange(entries); //Weird but necessary intermediate step.

			_totalWeight = 0;
			foreach (var entry in entries)
			{
				_totalWeight += entry.weight;
			}
		}
		public List<WeightedBlueprintEntry> _list;
		public int _totalWeight;
	}

	public static class BluePrintSpawner
	{

		private static readonly Dictionary<string, BlueprintList> roomListRegistry = new();

		public static bool RegisterNewBlueprintList(string listId, List<WeightedBlueprintEntry> entries)
		{
			if (roomListRegistry.ContainsKey(listId))
			{
				Loggy.Warning($"BlueprintSpawner already contains a list with id: {listId}");
				return false;
			}
			roomListRegistry.Add(listId, new BlueprintList(entries));
			return true;
		}

		//This seems like a dupe at first, but for some reason a list of child objects is not registered as a valid parameter.
		public static bool RegisterNewBlueprintList(string listId, List<WeightedMaintBlueprintEntry> entries)
		{
			if (roomListRegistry.ContainsKey(listId))
			{
				Loggy.Warning($"BlueprintSpawner already contains a list with id: {listId}");
				return false;
			}
			roomListRegistry.Add(listId, new BlueprintList(entries));
			return true;
		}

		public static bool UnregisterBlueprintList(string listId)
		{
			if (roomListRegistry.ContainsKey(listId) == false)
			{
				Loggy.Warning($"BlueprintSpawner does not contain a list with id: {listId}");
				return false;
			}

			roomListRegistry.Remove(listId);
			return true;
		}

		public static void ClearBlueprintRegistry()
		{
			roomListRegistry.Clear();
		}

		public static bool PickWeightedRoom(string listId, out int chosenEntry)
		{
			chosenEntry = -1;
			if (roomListRegistry.TryGetValue(listId, out var roomList) == false)
			{
				Loggy.Warning($"Could not find registry entry: {listId}");
				return false;
			}
			return PickWeightedRoom(roomList, out chosenEntry);
		}

		private static bool PickWeightedRoom(in BlueprintList blueprintList, out int chosenEntry)
		{
			chosenEntry = -1;

			int chosenWeight = UnityEngine.Random.Range(0, blueprintList._totalWeight + 1);
			int runningTotal = 0;

			for (int i = 0; i < blueprintList._list.Count; i++)
			{
				WeightedBlueprintEntry entry = blueprintList._list[i];
				runningTotal += entry.weight;
				if (runningTotal < chosenWeight) continue;

				int startingIndex = i;
				while (entry.currentOccurrences >= entry.maxOccurrences) //Find the closest entry that still has occurrences left
				{
					i = (i + 1) % blueprintList._list.Count;
					if (i == startingIndex) return false;

					entry = blueprintList._list[i];
				}

				entry.currentOccurrences++;
				chosenEntry = i;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Spawns a blueprint onto an attached parent matrix.
		/// </summary>
		/// <param name="parentMatrix">The matrix that this blueprint will belong too</param>
		/// <param name="positionToSpawn">The local position of the blueprint to spawn</param>
		/// <param name="roomListId">The string key for the registered blueprint option list</param>
		/// <param name="isEditor">Is this being run outside playmode?</param>
		/// <returns>The index of the chosen room to spawn or -1 on a failure</returns>
		public static int SpawnRandomRoom(Matrix parentMatrix, Vector3Int positionToSpawn, string roomListId,
			bool isEditor = false, int preChosenId = -1)
		{
			if (parentMatrix == false && isEditor == false)
			{
				Loggy.Error($"No parent matrix selected for blueprint: {roomListId}");
				return -1;
			}

			if (roomListRegistry.TryGetValue(roomListId, out var roomList) == false)
			{
				Loggy.Warning($"Could not find registry entry: {roomListId}");
				return -1;
			}

			if (roomList._list.Count == 0) return -1;

			int chosenEntry = preChosenId;
			if (preChosenId == -1)
			{
				if (PickWeightedRoom(roomList, out chosenEntry) == false)
				{
					Loggy.Warning($"No appropriate room to spawn in list: {roomListId}");
					return -1;
				}
			}

			string roomDirectory = roomList._list[chosenEntry].roomDirectory;
			string filePath = roomDirectory;
			MapSaver.MapSaver.CodeClass.ThisCodeClass.Reset();

			if (string.IsNullOrEmpty(filePath)) return -1;

			//Ensure is correct format for OS (Will replace forward slashes '/' with backslashes '\' for Windows systems.
			if(Path.AltDirectorySeparatorChar != Path.DirectorySeparatorChar) filePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

			Loggy.Info($"Accessing blueprint: {filePath}");
			string data = AccessFile.Load(filePath, FolderType.Rooms);

			var mapData = JsonConvert.DeserializeObject<MapSaver.MapSaver.MapData>(data);
			var matrixData = mapData.ContainedMatrices.Count != 0
				? mapData.ContainedMatrices[0]
				: JsonConvert.DeserializeObject<MapSaver.MapSaver.MatrixData>(data);

			MatrixInfo matrixInfo = null;
			if (isEditor == false) matrixInfo = parentMatrix.MatrixInfo;
			if (matrixData == null || mapData.ContainedMatrices.Count == 0)
			{
				Loggy.Error(
					$"Invalid MapData at filePath: {Path.Combine(Application.streamingAssetsPath, FolderType.Rooms.ToString(), filePath + ".json")}");
				return -1;
			}

			if (isEditor == false)
			{
				Loggy.Info("Loading Room..." + filePath);
				MapLoader.ServerLoadSectionNoCoRoutine(matrixInfo, Vector3.zero, positionToSpawn,
					matrixData, null, MatrixName: parentMatrix.name);
			}
			else SpawnBlueprintEditor(matrixData, positionToSpawn);
			return chosenEntry;
		}

		private static void SpawnBlueprintEditor(MapSaver.MapSaver.MatrixData dataToSpawn, Vector3Int positionToSpawn)
		{
			//Begin loading the map async
			var mapLoaderRoutine = MapLoader.ServerLoadSection(null, Vector3.zero, positionToSpawn, dataToSpawn, null, MatrixName: "Test Blueprint Matrix");
			List<IEnumerator> mapQueue = new List<IEnumerator>();
			bool isRoutineFinished = false;
			while (isRoutineFinished == false)
			{
				if (mapLoaderRoutine.Current is IEnumerator)
				{
					mapQueue.Add(mapLoaderRoutine);
					mapLoaderRoutine = (IEnumerator)mapLoaderRoutine.Current;
				}
				isRoutineFinished = mapLoaderRoutine.MoveNext() == false;

				if (isRoutineFinished && mapQueue.Count > 0)
				{
					mapLoaderRoutine = mapQueue[^1];
					mapQueue.RemoveAt(mapQueue.Count - 1);
					isRoutineFinished = mapLoaderRoutine.MoveNext() == false;
				}
			}
		}
	}
}