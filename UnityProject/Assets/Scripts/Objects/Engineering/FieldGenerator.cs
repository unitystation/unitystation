using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FieldGenerator : MonoBehaviour
{
	[SerializeField]
	private SpriteHandler topSpriteHandler = null;
	[SerializeField]
	private SpriteHandler powerSpriteHandler = null;
	[SerializeField]
	private SpriteHandler healthSpriteHandler = null;

	[SerializeField]
	private AnimatedTile vertical = null;
	[SerializeField]
	private AnimatedTile horizontal = null;

	[SerializeField]
	private bool alwaysOn;

	[SerializeField]
	[Range(0, 100)]
	private int detectionRange = 8;

	[SerializeField]
	private int maxEnergy = 100;

	/// <summary>
	/// Having energy means that the field will stay on, shared between connecting generators
	/// Emitters used to increase energy
	/// DO NOT SET DIRECTLY USE SetEnergy()
	/// </summary>
	[SerializeField]
	private int energy;

	/// <summary>
	/// energy increases health, if health 0 then field fails
	/// </summary>
	private int health;

	/// <summary>
	/// Gameobject = connectedgenerator, then bool = slave/master
	/// </summary>
	private Dictionary<Direction, Tuple<GameObject, bool>> connectedGenerator = new Dictionary<Direction, Tuple<GameObject, bool>>();

	private Integrity integrity;
	private RegisterTile registerTile;

	private List<Vector3Int> adjacentCoords = new List<Vector3Int>
	{
		new Vector3Int(0, 1, 0),
		new Vector3Int(1, 0, 0),
		new Vector3Int(0, -1, 0),
		new Vector3Int(-1, 0, 0)
	};

	private void Awake()
	{
		integrity = GetComponent<Integrity>();
		registerTile = GetComponent<RegisterTile>();
	}

	private void OnEnable()
	{
		UpdateManager.Add(FieldGenUpdate, 1f);
		integrity.OnWillDestroyServer.AddListener(OnDestroySelf);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, FieldGenUpdate);
		integrity.OnWillDestroyServer.RemoveListener(OnDestroySelf);
	}

	private void FieldGenUpdate()
	{
		if (CustomNetworkManager.IsServer == false) return;

		energy--;

		BalanceEnergy();

		DetectGenerators();

		if (energy <= 0 && alwaysOn == false)
		{
			RemoveAllShields();
			return;
		}

		TrySpawnShields();
	}

	#region Energy

	/// <summary>
	/// Get all connected energy values, average then set them
	/// </summary>
	private void BalanceEnergy()
	{
		if (connectedGenerator.Count == 0) return;

		var newEnergy = 0;

		foreach (var generator in connectedGenerator.ToArray())
		{
			newEnergy += generator.Value.Item1.GetComponent<FieldGenerator>().energy;
		}

		newEnergy += energy;

		if (newEnergy == 0)
		{
			return;
		}

		newEnergy /= connectedGenerator.Count + 1;

		if (newEnergy < 0)
		{
			newEnergy = 0;
		}

		SetEnergy(newEnergy);

		foreach (var generator in connectedGenerator.ToArray())
		{
			generator.Value.Item1.GetComponent<FieldGenerator>().SetEnergy(newEnergy);
		}
	}

	/// <summary>
	/// Use when changing energy values
	/// </summary>
	public void SetEnergy(int energyChange)
	{
		if (energy + energyChange >= maxEnergy)
		{
			energy = maxEnergy;
			return;
		}

		if (energy + energyChange < maxEnergy)
		{
			energy += energyChange;
			return;
		}

		if (energy < 0)
		{
			energy = 0;
		}
	}

	#endregion

	#region DetectGenerators

	/// <summary>
	/// Detect generators
	/// </summary>
	private void DetectGenerators()
	{
		var enumValues = Enum.GetValues(typeof(Direction));

		foreach (var value in enumValues)
		{
			if (connectedGenerator.ContainsKey((Direction)value)) continue;

			for (int i = 1; i <= detectionRange; i++)
			{
				var pos = registerTile.WorldPositionServer + GetCoordFromDirection((Direction)value) * i;

				var objects = MatrixManager.GetAt<FieldGenerator>(pos, true);

				//If there isn't a field generator but it is impassable dont check further
				if (objects.Count == 0 && !MatrixManager.IsPassableAtAllMatricesOneTile(pos, true, false))
				{
					break;
				}

				if (objects.Count > 0)
				{
					//Should be more than one, but just in case pick first
					//Add to connected gen dictionary
					connectedGenerator.Add((Direction)value, new Tuple<GameObject, bool>(objects[0].gameObject, false));
					objects[0].integrity.OnWillDestroyServer.AddListener(OnConnectedDestroy);
				}
			}
		}
	}

	#endregion

	#region SpawnShields

	private void TrySpawnShields()
	{
		foreach (var generator in connectedGenerator.ToArray())
		{
			if (generator.Value.Item2 == false)
			{
				var coords = new List<Vector3Int>();
				bool passCheck = false;

				for (int i = 1; i <= detectionRange; i++)
				{
					var pos = registerTile.WorldPositionServer + GetCoordFromDirection(generator.Key) * i;

					if (pos == generator.Value.Item1.WorldPosServer())
					{
						passCheck = true;
						break;
					}

					coords.Add(pos);
				}

				if (passCheck == false) continue;

				foreach (var coord in coords)
				{
					var matrix = MatrixManager.AtPoint(coord, true);

					matrix.TileChangeManager.UpdateTile(MatrixManager.WorldToLocalInt(coord, matrix), GetTileFromDirection(generator.Key));
				}

				connectedGenerator[generator.Key] = new Tuple<GameObject, bool>(generator.Value.Item1, true);
			}
		}
	}

	#endregion

	#region OnDestroy

	private void OnDestroySelf(DestructionInfo info)
	{
		if (CustomNetworkManager.IsServer == false) return;

		RemoveAllShields();
	}

	private void RemoveAllShields()
	{
		foreach (var generator in connectedGenerator.ToArray())
		{
			for (int i = 1; i <= detectionRange; i++)
			{
				var pos = registerTile.WorldPositionServer + GetCoordFromDirection(generator.Key) * i;

				if (pos == generator.Value.Item1.gameObject.WorldPosServer())
				{
					break;
				}

				var matrix = MatrixManager.AtPoint(pos, true);

				matrix.TileChangeManager.RemoveTile(MatrixManager.WorldToLocalInt(pos, matrix), LayerType.Windows);
			}
		}

		connectedGenerator.Clear();
	}

	private void OnConnectedDestroy(DestructionInfo info)
	{
		foreach (var generator in connectedGenerator.ToArray())
		{
			if (generator.Value.Item1 == info.Destroyed.gameObject)
			{
				for (int i = 1; i <= detectionRange; i++)
				{
					var pos = registerTile.WorldPositionServer + GetCoordFromDirection(generator.Key) * i;

					if (pos == info.Destroyed.gameObject.WorldPosServer())
					{
						break;
					}

					var matrix = MatrixManager.AtPoint(pos, true);

					matrix.TileChangeManager.RemoveTile(MatrixManager.WorldToLocalInt(pos, matrix), LayerType.Windows);
				}

				connectedGenerator.Remove(generator.Key);
				break;
			}
		}
	}

	#endregion

	#region DirectionMethods

	private AnimatedTile GetTileFromDirection(Direction direction)
	{
		switch (direction)
		{
			case Direction.Up:
				return vertical;
			case Direction.Down:
				return vertical;
			case Direction.Left:
				return horizontal;
			case Direction.Right:
				return horizontal;
			default:
				Debug.LogError($"Somehow got a wrong direction for {gameObject.ExpensiveName()} tile setting");
				return vertical;
		}
	}

	private Vector3Int GetCoordFromDirection(Direction direction)
	{
		switch (direction)
		{
			case Direction.Up:
				return Vector3Int.up;
			case Direction.Down:
				return Vector3Int.down;
			case Direction.Left:
				return Vector3Int.left;
			case Direction.Right:
				return Vector3Int.right;
			default:
				Debug.LogError($"Somehow got a wrong direction for {gameObject.ExpensiveName()}");
				return Vector3Int.zero;
		}
	}

	private Direction GetOppositeDirection(Direction direction)
	{
		switch (direction)
		{
			case Direction.Up:
				return Direction.Down;
			case Direction.Down:
				return Direction.Up;
			case Direction.Left:
				return Direction.Right;
			case Direction.Right:
				return Direction.Left;
			default:
				Debug.LogError($"Somehow got wrong opposite direction for {gameObject.ExpensiveName()}");
				return Direction.Up;
		}
	}

	private enum Direction
	{
		Up,
		Down,
		Left,
		Right
	}

	#endregion
}
