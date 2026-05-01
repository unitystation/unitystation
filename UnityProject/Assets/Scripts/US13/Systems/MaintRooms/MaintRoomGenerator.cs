using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Logs;
using Mirror;
using UnityEngine;
using US13.Core.GameGizmos;
using US13.Core.ObjectConnection;
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
		public List<WeightedRoomEntry> Rooms => possibleRoomsWeighted;

		public int SelectedRoom { get; private set; } = -1;

		[field: SerializeField] public string RoomListId { get; private set; } = "Maintrooms7x7";

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

		public Task ClearSpaceInMaze(Vector3 generatorOffset, int mazeWidth, int mazeHeight, in short[] maze)
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

			for (int y = Math.Max(pos.y,0); y < Math.Min(pos.y + roomHeight, mazeHeight); y++)
			{
				int startIndex = Math.Clamp(pos.x, 0, mazeWidth - 1) + (y * mazeWidth);
				Array.Fill(maze, (short)MazeState.ExcludedCell, startIndex, roomWidth);
			}

			return Task.CompletedTask;
		}

		public void CarveRoomDoors(Vector3 generatorOffset, Directions doorDirections, int mazeWidth, ref short[] maze)
		{
			var pos = (transform.localPosition - generatorOffset).RoundTo2Int();

			int halfX = (roomWidth - 1) / 2;
			int halfY = (roomHeight - 1) / 2;

			if (doorDirections.HasFlag(Directions.North))
			{
				Vector2 doorPosition = new Vector2Int(pos.x + halfX, pos.y + roomHeight);
				int index = (int)(doorPosition.x + (doorPosition.y * mazeWidth));
				if (index > 0 && index < maze.Length) maze[index] = (short)MazeState.EmptyCell;
			}
			if (doorDirections.HasFlag(Directions.South))
			{
				Vector2 doorPosition = new Vector2Int(pos.x + halfX, pos.y - 1);
				int index = (int)(doorPosition.x + (doorPosition.y * mazeWidth));
				if (index > 0 && index < maze.Length) maze[index] = (short)MazeState.EmptyCell;
			}
			if (doorDirections.HasFlag(Directions.West))
			{
				Vector2 doorPosition = new Vector2Int(pos.x - 1, pos.y + halfY);
				int index = (int)(doorPosition.x + (doorPosition.y * mazeWidth));
				if (index > 0 && index < maze.Length) maze[index] = (short)MazeState.EmptyCell;
			}
			if (doorDirections.HasFlag(Directions.East))
			{
				Vector2 doorPosition = new Vector2Int(pos.x + roomWidth, pos.y + halfY);
				int index = (int)(doorPosition.x + (doorPosition.y * mazeWidth));
				if (index > 0 && index < maze.Length) maze[index] = (short)MazeState.EmptyCell;
			}
		}

		public void ChooseRoom(int roomToChoose = -1, string newListId = null)
		{
			if (roomToChoose == -1) BluePrintSpawner.PickWeightedRoom(RoomListId, out roomToChoose);
			else RoomListId = newListId;

			SelectedRoom = roomToChoose;
		}

		public void TriggerGeneration(bool isEditor = false)
		{
			BluePrintSpawner.SpawnRandomRoom(isEditor ? null : maintGenerator.MatrixInfo.Matrix, isEditor ? transform.position.CutToInt() - new Vector3Int(1, 1, 0) : transform.localPosition.CutToInt(), RoomListId, isEditor, SelectedRoom);
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