using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Tiles;
using System.Threading.Tasks;
using InGameGizmos;
using MaintRooms;
using Objects;
using Shared.Systems.ObjectConnection;
using TileMap.Behaviours;
using NaughtyAttributes;

namespace Systems.Scenes
{
	public class MaintGenerator : ItemMatrixSystemInit, IMultitoolMasterable, ISelectionGizmo
	{
		public MultitoolConnectionType ConType => MultitoolConnectionType.MaintGeneratorExclusionZone;

		public int MaxDistance => 9999;

		[field: SerializeField] public bool CanRelink { get; set; } = true;
		[field: SerializeField] public bool IgnoreMaxDistanceMapper { get; set; } = true;

		private enum Direction
		{
			North = 1,
			East = 2,
			South = 3,
			West = 4,
		}

		private readonly Dictionary<Direction, Vector2Int> DirectionVector = new Dictionary<Direction, Vector2Int>
		{
			{Direction.North, new Vector2Int(0, 1)},
			{Direction.South, new Vector2Int(0, -1)},
			{Direction.East, new Vector2Int(1, 0)},
			{Direction.West, new Vector2Int(-1, 0)}
		};

		private const int MAX_DIMENSIONS = 256;
		private const int MAX_PERCENT = 100;
		private const int WALL_GAP = 2;
		private readonly Vector3 GIZMO_OFFSET = new Vector3(-0.5f, -0.5f, 0);

		[SerializeField] private Vector2 offset = Vector2.zero;

		[SerializeField, Range(1, MAX_DIMENSIONS)]
		private int width = 20;

		[SerializeField, Range(1, MAX_DIMENSIONS)]
		private int height = 20;

		[SerializeField] private LayerTile wallTile;

		[SerializeField, Range(0, MAX_PERCENT)]
		private int objectChance = 50;

		[SerializeField] private List<MaintObject> possibleSpawns = new List<MaintObject>();

		[SerializeField, Tooltip("Possible crates or lockers that items can spawn in")]
		private List<GameObject> containers = new List<GameObject>();

		[SerializeField, Tooltip("The areas inside the bounds of this maze that the maze should not generate atop of.")]
		private List<ExclusionZoneMono> exclusionZonesMono = new List<ExclusionZoneMono>();

		private short[] mazeArray;
		private List<Vector2Int> possibleCells;

		public GameGizmoSquare GameGizmoSquare { get; private set; }

		public void AddExclusionZoneMono(ExclusionZoneMono ExclusionZoneMono)
		{
			exclusionZonesMono.Add(ExclusionZoneMono);
		}

		public void RemoveExclusionZoneMono(ExclusionZoneMono ExclusionZoneMono)
		{
			exclusionZonesMono.Remove(ExclusionZoneMono);
		}

		[Button("Add Generator Reference to Zone List Entries")]
		public void AddGeneratorToZones()
		{
			List<ExclusionZoneMono> lst = new List<ExclusionZoneMono>(exclusionZonesMono);
			foreach(ExclusionZoneMono zone in lst)
			{
				zone.SetMasterEditor(this);
			}
		}

		public override void Start()
		{
			base.Start();
			if (CustomNetworkManager.IsServer == false) return;

			mazeArray = new short[width * height];
			possibleCells = new List<Vector2Int>();

			CarveRooms();
			Task.Run(GenerateNewMaze);
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.red;
			var size = new Vector2Int(width, height).To3();

			Gizmos.DrawWireCube(transform.position + offset.To3() + size / WALL_GAP + GIZMO_OFFSET, size);
		}

		#region Tiles

		private async Task GenerateNewMaze()
		{
			await CarvePath(new Vector2Int(0,0));

			MaintGeneratorManager.MaintGenerators.Add(this);
		}

		public void OnEnable()
		{
			EventManager.AddHandler(Event.ScenesLoadedServer, CreateTiles);
			EventManager.AddHandler(Event.RoundStarted, PlaceObjects);
		}

		public void OnDisable()
		{
			EventManager.RemoveHandler(Event.ScenesLoadedServer, CreateTiles);
			EventManager.RemoveHandler(Event.RoundStarted, PlaceObjects);
		}

		//Growing Tree algorithm for maze generation using a 'newest' choosing method for next cell.
		//Eller's algorithm does scale better for larger mazes,
		private Task CarvePath(Vector2Int startingCellLocation)
		{
			possibleCells.Add(startingCellLocation);

			Vector2Int currentCell;
			Vector2Int newCell;
			bool foundPath;

			do
			{
				foundPath = false;
				currentCell = possibleCells[possibleCells.Count - 1];

				var directions = new List<Direction>
				{
					Direction.North,
					Direction.South,
					Direction.East,
					Direction.West
				}.OrderBy(z => Guid.NewGuid());

				mazeArray[currentCell.x + currentCell.y*width] = (short)MazeState.EmptyCell;

				foreach (Direction direction in directions)
				{
					newCell = new Vector2Int(currentCell.x, currentCell.y) + (DirectionVector[direction] * WALL_GAP);

					if (IsOutOfBounds(newCell.x, newCell.y)) continue;

					if (mazeArray[newCell.x + newCell.y*width] == (short)MazeState.FullCell)
					{
						possibleCells.Add(new Vector2Int(newCell.x, newCell.y));

						mazeArray[newCell.x + newCell.y*width] = (short)MazeState.EmptyCell;
						mazeArray[currentCell.x + DirectionVector[direction].x + (currentCell.y + DirectionVector[direction].y)*width] = (short)MazeState.EmptyCell;

						foundPath = true;
					}
				}

				if (foundPath == false) possibleCells.Remove(currentCell);

			} while (possibleCells.Count != 0);

			return Task.CompletedTask;
		}

		private void CarveRooms()
		{
			foreach (ExclusionZoneMono zone in exclusionZonesMono)
			{
				for (int y = 0; y < zone.Size.y; y++)
				{
					var pos = (zone.transform.localPosition - this.gameObject.transform.localPosition).RoundTo2Int();
					int startIndex = pos.x + ((pos.y + y) * width);

					Array.Fill(mazeArray, (short)MazeState.ExcludedCell, startIndex, zone.Size.x);
				}
			}
		}

		private bool IsOutOfBounds(int x, int y)
		{
			if (x < 0 || x >= width)
				return true;

			if (y < 0 || y >= height)
				return true;

			return false;
		}

		public void CreateTiles()
		{
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					Vector3Int pos = new Vector3Int(x, y, 0) + transform.localPosition.CutToInt() + offset.To3Int();

					if (mazeArray[x + y*width] == (short)MazeState.FullCell)
					{
						MetaTileMap.SetTile(pos, wallTile);
					}
				}
			}
		}

		#endregion

		public void OnSelected()
		{
			var size = new Vector2Int(width, height).To3();
			GameGizmoSquare.OrNull()?.Remove();
			GameGizmoSquare = GameGizmomanager.AddNewSquareStaticClient(this.gameObject,
				offset.To3() + size / WALL_GAP + GIZMO_OFFSET, Color.red, BoxSize: size);

			foreach (var ExclusionMono in exclusionZonesMono)
			{
				ExclusionMono.OnSelected();
			}
		}

		public void OnDeselect()
		{
			GameGizmoSquare.OrNull()?.Remove();
			GameGizmoSquare = null;
			foreach (var ExclusionMono in exclusionZonesMono)
			{
				ExclusionMono.OnDeselect();
			}
		}

		public void UpdateGizmos()
		{
			var size = new Vector2Int(width, height).To3();

			GameGizmoSquare.Position = offset.To3() + size / WALL_GAP + GIZMO_OFFSET;
			GameGizmoSquare.transform.localScale = size;

			foreach (var ExclusionMono in exclusionZonesMono)
			{
				ExclusionMono.UpdateGizmos();
			}
		}

		#region Objects

		public void PlaceObjects()
		{
			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					if (mazeArray[i + j*width] != (short)MazeState.EmptyCell) continue;

					int h = UnityEngine.Random.Range(0, MAX_PERCENT);
					if (h > objectChance) continue;

					TrySpawnObject(i, j);
				}
			}
		}

		private void TrySpawnObject(int i, int j)
		{
			int neighbourCount = CountNeighbours(i, j);

			foreach (MaintObject obj in possibleSpawns)
			{
				if (obj.RequiredWalls != neighbourCount) continue;
				int h = UnityEngine.Random.Range(0, MAX_PERCENT);

				if (h > obj.ObjectChance || (obj.RequireOpposingWalls && CheckOpposites(i, j) == false)) continue;

				Vector3 pos = new Vector3Int(i, j, 0) + transform.position.CutToInt() + offset.To3();
				if (obj.ObjectToSpawn != null) Spawn.ServerPrefab(obj.ObjectToSpawn, SpawnDestination.At(pos));

				if (obj.SpawnLockerCrate)
				{
					GameObject container = Spawn.ServerPrefab(containers.PickRandom(), SpawnDestination.At(pos))
						.GameObject;
					if (container.TryGetComponent<ClosetControl>(out var closet) == false) break;

					closet.CollectObjects();
				}

				if (obj.TileToSpawn != null)
				{
					MetaTileMap.SetTile(pos.ToLocalInt(MetaTileMap.matrix), obj.TileToSpawn);
				}

				break;
			}
		}

		private bool CheckOpposites(int x, int y)
		{
			Vector2Int newCellA = new Vector2Int(x, y) + DirectionVector[Direction.East];
			Vector2Int newCellB = new Vector2Int(x, y) + DirectionVector[Direction.West];

			if (IsOutOfBounds(newCellB.x, newCellB.y) == false
				&& IsOutOfBounds(newCellA.x, newCellA.y) == false
				&& mazeArray[newCellA.x + newCellA.y*width] == (short)MazeState.FullCell
				&& mazeArray[newCellB.x + newCellB.y*width] == (short)MazeState.FullCell) return true;

			newCellA = new Vector2Int(x, y) + DirectionVector[Direction.North];
			newCellB = new Vector2Int(x, y) + DirectionVector[Direction.South];

			if (IsOutOfBounds(newCellB.x, newCellB.y) == false
				&& IsOutOfBounds(newCellA.x, newCellA.y) == false
			    && mazeArray[newCellA.x + newCellA.y*width] == (short)MazeState.FullCell
                && mazeArray[newCellB.x + newCellB.y*width] == (short)MazeState.FullCell) return true;

			return false;
		}

		private int CountNeighbours(int x, int y)
		{
			int count = 0;
			foreach (KeyValuePair<Direction, Vector2Int> direction in DirectionVector)
			{
				Vector2Int newCell = new Vector2Int(x, y) + direction.Value;

				if (IsOutOfBounds(newCell.x, newCell.y)) continue;

				if (mazeArray[newCell.x + newCell.y*width] == (short)MazeState.FullCell) count++;
			}

			return count;
		}

		#endregion
	}

	public enum MazeState
	{
		FullCell = 0,
		EmptyCell = 1,
		ExcludedCell = 2,
	}

	[Serializable]
	public class MaintObject
	{
		private const int MAX_PERCENT = 100;
		private const int MAX_NEIGHBOURS = 4;

		[field: SerializeField, Tooltip("The object to be spawned during maint generation.")]
		public GameObject ObjectToSpawn { get; private set; }

		[field: SerializeField, Range(0, MAX_PERCENT), Tooltip("The chance that this object will spawn.")]
		public int ObjectChance { get; private set; }

		[field: SerializeField, Range(0, MAX_NEIGHBOURS),
		        Tooltip("The required walls next to an object in order for it to spawn.")]
		public int RequiredWalls { get; private set; }

		[field: SerializeField,
		        Tooltip(
			        "Used by doors and grills, requires walls to be on opposite sides of the object to be a valid spawn.")]
		public bool RequireOpposingWalls { get; private set; }

		[field: SerializeField, Tooltip("Will this object spawn in combination with a locker or crate?")]
		public bool SpawnLockerCrate { get; private set; }

		[field: SerializeField, Tooltip("The tile, if any, spawned when this object is placed")]
		public LayerTile TileToSpawn { get; private set; }
	}

	[Serializable]
	public class ExclusionZone
	{
		[field: SerializeField] public Vector2Int Offset { get; private set; }

		[field: SerializeField] public Vector2Int Size { get; private set; }
	}
}