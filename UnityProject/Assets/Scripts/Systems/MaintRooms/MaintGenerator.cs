using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Tiles;
using System.Threading.Tasks;
using Objects;

namespace Systems.Scenes
{
	public class MaintGenerator : MonoBehaviour
	{
		private enum Direction
		{
			North = 1,
			East = 2,
			South = 3,
			West = 4,
		}

		private readonly Dictionary<Direction, Vector2Int> DirectionVector = new Dictionary<Direction, Vector2Int>
		{
			{ Direction.North, new Vector2Int(0,1) },
			{ Direction.South, new Vector2Int(0,-1) },
			{ Direction.East, new Vector2Int(1,0) },
			{ Direction.West, new Vector2Int(-1,0) }
		};

		private const int MAX_DIMENSIONS = 50;
		private const int MAX_PERCENT = 100; //Damn codacy and it's obsession with constants.
		private const int WALL_GAP = 2;
		private readonly Vector3 GIZMO_OFFSET = new Vector3(-0.5f, -0.5f, 0);

		[SerializeField] private Vector2 offset = Vector2.zero;

		[SerializeField, Range(1, MAX_DIMENSIONS)] private int width = 20;
		[SerializeField, Range(1, MAX_DIMENSIONS)] private int height = 20;
		[SerializeField] private LayerTile wallTile;

		[SerializeField, Range(0,MAX_PERCENT)] private int objectChance = 50;

		[SerializeField] private Matrix matrix;

		[SerializeField] private List<MaintObject> possibleSpawns = new List<MaintObject>();

		[SerializeField,Tooltip("Possible crates or lockers that items can spawn in")]
		private List<GameObject> containers = new List<GameObject>();

		[SerializeField, Tooltip("Possible crates or lockers that items can spawn in")]
		private List<ExclusionZone> exclusionZones = new List<ExclusionZone>();

		private int[,] mazeArray;
		private HashSet<Vector2Int> bordercells;

		public void Start()
		{
			if (CustomNetworkManager.IsServer == false) return;

			Task.Run(GenerateNewMaze);
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.red;
			var size = new Vector2Int(width, height).To3();

			Gizmos.DrawWireCube(transform.position + offset.To3() + size/WALL_GAP + GIZMO_OFFSET, size);

			Gizmos.color = Color.cyan;
			foreach(ExclusionZone zone in exclusionZones)
			{
				Gizmos.DrawWireCube(transform.position + offset.To3() + zone.Offset.To3() + zone.Size.To3()/WALL_GAP + GIZMO_OFFSET, zone.Size.To3());
			}
		}

		#region Tiles

		private async Task GenerateNewMaze()
		{
			mazeArray = new int[width, height];
			bordercells = new HashSet<Vector2Int>();

			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					mazeArray[i, j] = 1; //Sets the maze to be solid walls initially
				}
			}

			await CarveRooms();

			await CarvePath(1, 1);
		
			MaintGeneratorManager.MaintGenerators.Add(this);

		}

		private Task CarvePath(int x, int y)
		{
			var directions = new List<Direction>
			{
				Direction.North,
				Direction.South,
				Direction.East,
				Direction.West
			}.OrderBy(z => Guid.NewGuid());

			mazeArray[x, y] = 0; //Sets current cell to air

			foreach (Direction direction in directions) //Carves walls
			{
				Vector2Int newCell = new Vector2Int(x, y) + (DirectionVector[direction] * WALL_GAP);

				if (IsOutOfBounds(newCell.x, newCell.y)) continue;

				if (mazeArray[newCell.x, newCell.y] == 0) //If cell is already empty (has already been visited) remove the wall that speerates current cell to previous
				{
					mazeArray[x + DirectionVector[direction].x, y + DirectionVector[direction].y] = 0;
					break; //If a wall is removed, move onto next cell.
				}
			}

			foreach (Direction direction in directions) //Gets new bordercells
			{
				Vector2Int newCell = new Vector2Int(x, y) + (DirectionVector[direction] * WALL_GAP);

				if (IsOutOfBounds(newCell.x, newCell.y)) continue;

				if (mazeArray[newCell.x, newCell.y] == 1 && bordercells.Contains(new Vector2Int(newCell.x, newCell.y)) == false) //If cell is a wall and not current in border cells, add it to the border cells
				{
					bordercells.Add(new Vector2Int(newCell.x, newCell.y));
				}
			}

			bordercells.Remove(new Vector2Int(x, y));

			if (bordercells.Count == 0)
			{
				return Task.CompletedTask;
			}
			else
			{
				Vector2Int nextCell = bordercells.PickRandom();
				return CarvePath(nextCell.x, nextCell.y);
			}
		}

		private Task CarveRooms()
		{
			foreach(ExclusionZone zone in exclusionZones)
			{
				for (int x = 0; x < zone.Size.x; x++)
				{
					for(int y = 0; y < zone.Size.y; y++)
					{
						var pos = zone.Offset + new Vector2Int(x, y);

						mazeArray[pos.x, pos.y] = WALL_GAP; //Not a wall but no objects can be spawn here either.
					}
				}
			}
			return Task.CompletedTask;
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

			//Places tiles at mazeArray elements with value 1
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					Vector3Int pos = new Vector3Int(x, y, 0) + transform.localPosition.CutToInt() + offset.To3Int();

					if (mazeArray[x, y] == 1)
					{
						matrix.MatrixInfo.MetaTileMap.SetTile(pos, wallTile);
					}
				}
			}
		}

		#endregion

		#region Objects

		public void PlaceObjects()
		{
			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					if (mazeArray[i, j] != 0) continue;

					int h = UnityEngine.Random.Range(0, MAX_PERCENT);
					if (h > objectChance) continue;

					TrySpawnObject(i,j);
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
				if(obj.ObjectToSpawn != null) Spawn.ServerPrefab(obj.ObjectToSpawn, SpawnDestination.At(pos));

				if (obj.SpawnLockerCrate)
				{
					GameObject container = Spawn.ServerPrefab(containers.PickRandom(), SpawnDestination.At(pos)).GameObject;
					if (container.TryGetComponent<ClosetControl>(out var closet) == false) break;

					closet.CollectObjects();
				}

				if (obj.TileToSpawn != null)
				{
					matrix.MatrixInfo.MetaTileMap.SetTile(pos.ToLocalInt(matrix), obj.TileToSpawn);
				}

				break;
			}
		}

		private bool CheckOpposites(int x, int y)
		{
			Vector2Int newCellA = new Vector2Int(x, y) + DirectionVector[Direction.East];
			Vector2Int newCellB = new Vector2Int(x, y) + DirectionVector[Direction.West];
			if (IsOutOfBounds(newCellB.x, newCellB.y) == false && IsOutOfBounds(newCellA.x, newCellA.y) == false
				&& (mazeArray[newCellA.x, newCellA.y] == 1 && mazeArray[newCellB.x, newCellB.y] == 1)) return true;

			newCellA = new Vector2Int(x, y) + DirectionVector[Direction.North];
			newCellB = new Vector2Int(x, y) + DirectionVector[Direction.South];
			if (IsOutOfBounds(newCellB.x, newCellB.y) == false && IsOutOfBounds(newCellA.x, newCellA.y) == false
			&& (mazeArray[newCellA.x, newCellA.y] == 1 && mazeArray[newCellB.x, newCellB.y] == 1)) return true;

			return false;
		}

		private int CountNeighbours(int x, int y)
		{
			int count = 0;
			foreach(KeyValuePair<Direction, Vector2Int> direction in DirectionVector)
			{
				Vector2Int newCell = new Vector2Int(x, y) + direction.Value;

				if (IsOutOfBounds(newCell.x, newCell.y)) continue;

				if (mazeArray[newCell.x, newCell.y] == 1) count++;
			}

			return count;
		}

		#endregion

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

		[field: SerializeField, Range(0, MAX_NEIGHBOURS), Tooltip("The required walls next to an object in order for it to spawn.")]
		public int RequiredWalls { get; private set; }

		[field: SerializeField, Tooltip("Used by doors and grills, requires walls to be on opposite sides of the object to be a valid spawn.")]
		public bool RequireOpposingWalls { get; private set; }

		[field: SerializeField, Tooltip("Will this object spawn in combination with a locker or crate?")]
		public bool SpawnLockerCrate { get; private set; }

		[field: SerializeField, Tooltip("The tile, if any, spawned when this object is placed")]
		public LayerTile TileToSpawn { get; private set; }
	}

	[Serializable]
	public class ExclusionZone
	{
		[field: SerializeField]
		public Vector2Int Offset { get; private set; }

		[field: SerializeField]
		public Vector2Int Size { get; private set; }
	}
}
