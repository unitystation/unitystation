using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Logs;
using Mirror;
using NaughtyAttributes;
using Newtonsoft.Json;
using SecureStuff;
using UnityEngine;
using US13.Core.GameGizmos;
using US13.Core.ObjectConnection;
using US13.Managers.MatrixManager;
using US13.MapSaver;
using US13.Variable_Viewer;
using Util;

namespace US13.Systems.MaintRooms
{
	[Serializable]
	public class WeightedRoomEntry
	{
		//This would perfectly serve as a struct, except unity won't expose it in the editor unless this is a class
		public MaintRoomSO roomToSpawn;
		public int weight;
	}

	public class MaintRoomGenerator : NetworkBehaviour, IMultitoolSlaveable, ISelectionGizmo
	{
		[SerializeField, SyncVar(hook = nameof(SyncMaintGenerator))]
		private MaintGenerator maintGenerator;

		[Header("Exclusion Zone Dimensions")]
		[SerializeField, Range(1, MaintGenerator.MAX_DIMENSIONS)] private int roomWidth = 5;
		[SerializeField, Range(1, MaintGenerator.MAX_DIMENSIONS)] private int roomHeight = 5;

		private const int WALL_GAP = 2;
		[SerializeField] private List<WeightedRoomEntry> possibleRoomsWeighted = new List<WeightedRoomEntry>();

		private MaintRoomSO selectedRoom = null;

		public void SyncMaintGenerator(MaintGenerator oldGen, MaintGenerator newGen)
		{
			if (maintGenerator == newGen) return;
			maintGenerator = newGen;


			if (oldGen != null)
			{
				if (GameGizmoSquare != null) OnDeselect();
				oldGen.RemoveRoom(this);
			}

			if (maintGenerator == null) return;

			if (maintGenerator.GameGizmoSquare != null) OnSelected();
			maintGenerator.AddRoom(this);

		}

		#region Generation

		public Task ClearSpaceInMaze(Vector3 generatorOffset, int mazeWidth, in short[] maze)
		{
			if (roomWidth < 0 || roomWidth > MaintGenerator.MAX_DIMENSIONS)
			{
				Loggy.Error($"Room size cannot be negative or exceed {MaintGenerator.MAX_DIMENSIONS}!");
				return Task.CompletedTask;
			}

			var pos = (transform.localPosition - generatorOffset).RoundTo2Int();

			if (pos.x % 2 == 0 || pos.y % 2 == 0)
			{
				Loggy.Warning(
					"Maintroom generator placed on odd coordinates, this might result in undesired generation!");
			}
			if (roomWidth % 2 == 0 || roomHeight % 2 == 0)
			{
				Loggy.Warning(
					"Maintroom generator has even dimensions, this might result in undesired generation!");
			}

			for (int y = 0; y < roomHeight; y++)
			{
				int startIndex = pos.x + ((pos.y + y) * mazeWidth);
				Array.Fill(maze, (short)MazeState.ExcludedCell, startIndex, roomWidth);
			}

			return Task.CompletedTask;
		}

		public void SelectRoom()
		{
			PickWeightedRoom(out selectedRoom);
		}

		private bool PickWeightedRoom(out MaintRoomSO room)
		{
			room = null;

			int totalWeight = 0;
			int chosenWeight = 0;
			int currentTotal = 0;

			foreach (WeightedRoomEntry entry in possibleRoomsWeighted)
			{
				totalWeight += entry.weight;
			}


			chosenWeight = UnityEngine.Random.Range(0, totalWeight + 1);

			foreach (WeightedRoomEntry entry in possibleRoomsWeighted)
			{
				currentTotal += entry.weight;
				if (chosenWeight > currentTotal) continue;
				room = entry.roomToSpawn;
				return true;
			}
			return false;
		}

		public void CarveRoomDoors(Vector3 generatorOffset, int mazeWidth, ref short[] maze)
		{
			var pos = (transform.localPosition - generatorOffset).RoundTo2Int();

			int halfX = (roomWidth - 1) / 2;
			int halfY = (roomHeight - 1) / 2;

			if (selectedRoom.DoorDirections.HasFlag(DirectionFlag.Up))
			{
				Vector2 doorPosition = new Vector2Int(pos.x + halfX, pos.y + roomHeight);
				int index = (int)(doorPosition.x + (doorPosition.y * mazeWidth));
				if (index > 0 && index < maze.Length) maze[index] = (short)MazeState.EmptyCell;
			}
			if (selectedRoom.DoorDirections.HasFlag(DirectionFlag.Down))
			{
				Vector2 doorPosition = new Vector2Int(pos.x + halfX, pos.y - 1);
				int index = (int)(doorPosition.x + (doorPosition.y * mazeWidth));
				if (index > 0 && index < maze.Length) maze[index] = (short)MazeState.EmptyCell;
			}
			if (selectedRoom.DoorDirections.HasFlag(DirectionFlag.Left))
			{
				Vector2 doorPosition = new Vector2Int(pos.x - 1, pos.y + halfY);
				int index = (int)(doorPosition.x + (doorPosition.y * mazeWidth));
				if (index > 0 && index < maze.Length) maze[index] = (short)MazeState.EmptyCell;
			}
			if (selectedRoom.DoorDirections.HasFlag(DirectionFlag.Right))
			{
				Vector2 doorPosition = new Vector2Int(pos.x + roomWidth, pos.y + halfY);
				int index = (int)(doorPosition.x + (doorPosition.y * mazeWidth));
				if (index > 0 && index < maze.Length) maze[index] = (short)MazeState.EmptyCell;
			}
		}

		[Button("Test Room Spawn")]
		private void TestRoomSpawn()
		{
			if(PickWeightedRoom(out selectedRoom)) SpawnRandomRoom(true);
		}

		public void SpawnRandomRoom(bool isEditor = false)
		{
			if (possibleRoomsWeighted.Count == 0) return;

			string filePath = Path.Combine("MaintRoomBluePrints", selectedRoom.roomFileName);
			MapSaver.MapSaver.CodeClass.ThisCodeClass.Reset();

			Loggy.Info("Accessing Room: " + filePath);
			if (string.IsNullOrEmpty(filePath)) return;

			var positionOffset = (transform.position - new Vector3(1,1,0));

			string data = AccessFile.Load(filePath, FolderType.Rooms);

			var mapData = JsonConvert.DeserializeObject<MapSaver.MapSaver.MapData>(data);
			var matrixData = mapData.ContainedMatrices.Count != 0 ? mapData.ContainedMatrices[0] : JsonConvert.DeserializeObject<MapSaver.MapSaver.MatrixData>(data);

			if (maintGenerator == false)
			{
				Loggy.Error($"No maint generator parent for {gameObject.ExpensiveName()}");
				return;
			}

			MatrixInfo matrixInfo = null;
			if(isEditor == false) matrixInfo = maintGenerator?.MatrixInfo;


			if (matrixData == null || mapData.ContainedMatrices.Count == 0)
			{
				Loggy.Error($"Invalid MapData at filePath: {Path.Combine(Application.streamingAssetsPath,FolderType.Rooms.ToString(), filePath + ".json")}");
				return;
			}

			if(isEditor == false)
			{
				Loggy.Info("Loading Room..." + filePath);
				MapLoader.ServerLoadSectionNoCoRoutine(matrixInfo, Vector3.zero, transform.localPosition,
				matrixData, null, MatrixName: "Backrooms");
			}
			else
			{
				var Imnum = MapLoader.ServerLoadSection(null, Vector3.zero, positionOffset,
					matrixData, null, MatrixName: "Backrooms");
				List<IEnumerator> previousLevels = new List<IEnumerator>();
				bool loop = true;
				while (loop && previousLevels.Count == 0)
				{
					if (Imnum.Current is IEnumerator)
					{
						previousLevels.Add(Imnum);
						Imnum = (IEnumerator)Imnum.Current;
					}
					loop = Imnum.MoveNext();
					if (!loop && previousLevels.Count > 0)
					{
						Imnum = previousLevels[^1];
						previousLevels.RemoveAt(previousLevels.Count - 1);
						loop = Imnum.MoveNext();
					}
				}
			}
		}

		#endregion

		#region Connection

		public MultitoolConnectionType ConType => MultitoolConnectionType.MaintGeneratorExclusionZone;
		public bool CanRelink => true;

		public IMultitoolMasterable Master
		{
			get => maintGenerator as MaintGenerator;
			set { maintGenerator = value as MaintGenerator; }
		}

		public bool RequireLink => true;

		public bool TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			Master = master;
			if (Master != null)
			{
				var generator = (Master as MaintGenerator);
				generator?.RemoveRoom(this);
			}

			Master = master;
			if (Master != null)
			{
				var generator = (Master as MaintGenerator);
				generator?.AddRoom(this);
			}
			return true;
		}

		public void SetMasterEditor(IMultitoolMasterable master)
		{
			Master = master;
		}


		#endregion

		#region Gizmos

		private readonly Vector3 GIZMO_OFFSET = new Vector3(-0.5f, -0.5f, 0);
		private GameGizmoSquare GameGizmoSquare;

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.cyan;

			Gizmos.DrawWireCube(transform.position + new Vector3(roomWidth, roomHeight, 0) / WALL_GAP + GIZMO_OFFSET, new Vector3(roomWidth, roomHeight, 1));
		}


		public void OnSelected()
		{
			GameGizmoSquare.OrNull()?.Remove();
			GameGizmoSquare = GameGizmomanager.AddNewSquareStaticClient(this.gameObject,
				new Vector3(roomWidth, roomHeight, 0) / WALL_GAP + GIZMO_OFFSET, Color.cyan, BoxSize: new Vector3(roomWidth, roomHeight, 1));
		}

		public void OnDeselect()
		{
			GameGizmoSquare.OrNull()?.Remove();
			GameGizmoSquare = null;
		}

		public void UpdateGizmos()
		{
			GameGizmoSquare.Position = new Vector3(roomWidth, roomHeight, 0) / WALL_GAP + GIZMO_OFFSET;
			GameGizmoSquare.transform.localScale = new Vector3(roomWidth, roomHeight, 1);
		}

		#endregion
	}
}