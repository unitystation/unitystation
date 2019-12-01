using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

[Serializable]
public struct TileState
{
	public Sprite Sprite;
	public float Damage;
}

public abstract class BasicTile : LayerTile
{
	public bool AtmosPassable;
	public bool IsSealed;
	public bool Passable;
	public bool Mineable;
	public PassableDictionary PassableException;

	public float MaxHealth;
	public TileState[] HealthStates;

	[Tooltip("What object to spawn when it's deconstructed or destroyed. " +
	         "Can leave blank for floors to use default floor tile. Ignored if" +
	         " this tile is a wall.")]
	[SerializeField]
	private GameObject spawnOnDeconstruct;
	/// <summary>
	/// Object to spawn when deconstructed. Defaults to a standard tile for this layer if not specified
	/// for this particular basictile.
	/// </summary>
	public GameObject SpawnOnDeconstruct => spawnOnDeconstruct ? spawnOnDeconstruct : DefaultSpawnOnDeconstruct();

	[Tooltip("How much of the object to spawn when it's deconstructed. Can leave at 0 for floor tiles to use the" +
	         " default of 1. Ignored if" +
	         " this tile is a wall.")]
	[SerializeField]
	private int spawnAmountOnDeconstruct;

	[Tooltip("Deconstruction logic to use for this tile. Can leave as None for floor tiles to use" +
	         " default floor deconstruction logic.")]
	[SerializeField]
	private DeconstructionType deconstructionType;

	private DeconstructionType DeconstructionType => deconstructionType != DeconstructionType.None
		? deconstructionType
		: DefaultDeconstructionType();



	public LayerTile DestroyedTile;

	public override void RefreshTile(Vector3Int position, ITilemap tilemap)
	{
		foreach (Vector3Int p in new BoundsInt(-1, -1, 0, 3, 3, 1).allPositionsWithin)
		{
			tilemap.RefreshTile(position + p);
		}
	}

	/// <summary>
	/// Checks if the tile is Passable by the ColliderType
	/// It will return the default Passable bool unless an exception is avalaible in PassableException
	/// </summary>
	/// <param name="colliderType"></param>
	/// <returns>IsPassable</returns>
	public bool IsPassable(CollisionType colliderType)
	{
		if (PassableException.ContainsKey(colliderType))
		{
			return PassableException[colliderType];
		} else
		{
			return Passable;
		}
	}

	public bool IsAtmosPassable()
	{
		return AtmosPassable;
	}

	public bool IsSpace()
	{
		return IsAtmosPassable() && !IsSealed;
	}

	/// <summary>
	/// Returns true iff this tile can be deconstructed by the indicated interaction
	/// </summary>
	/// <param name="interaction"></param>
	/// <returns></returns>
	public bool CanDeconstruct(PositionalHandApply interaction)
	{
		switch (DeconstructionType)
		{
			case DeconstructionType.Crowbar:
				return Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar);
			case DeconstructionType.Wirecutters:
				return Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter);
			case DeconstructionType.Wrench:
				return Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench);
			case DeconstructionType.NormalWall:
				var welder = interaction.HandObject?.GetComponent<Welder>();
				return welder != null && welder.isOn;
		}

		return false;
	}

	/// <summary>
	/// Performs the deconstruction for this tile depending on its deconstruction type
	/// </summary>
	/// <param name="interaction"></param>
	public void ServerDeconstruct(PositionalHandApply interaction)
	{
		var interactableTiles = InteractableTiles.GetAt(interaction.WorldPositionTarget, true);
		var tileChangeManager = interactableTiles.TileChangeManager;
		LayerTile tile = interactableTiles.LayerTileAt(interaction.WorldPositionTarget);
		var cellPos = interactableTiles.WorldToCell(interaction.WorldPositionTarget);


		if (DeconstructionType == DeconstructionType.Crowbar)
		{
			tileChangeManager.RemoveTile(Vector3Int.RoundToInt(cellPos), tile.LayerType);
			SoundManager.PlayNetworkedAtPos("Crowbar", interaction.WorldPositionTarget, Random.Range(0.8f, 1.2f));
			tileChangeManager.SubsystemManager.UpdateAt(cellPos);
			DoSpawnOnDeconstruct(interaction);

		}
		else if (DeconstructionType == DeconstructionType.Wirecutters)
		{
			tileChangeManager.RemoveTile(Vector3Int.RoundToInt(cellPos), tile.LayerType);
			SoundManager.PlayNetworkedAtPos("WireCutter", interaction.WorldPositionTarget, Random.Range(0.8f, 1.2f));
			tileChangeManager.SubsystemManager.UpdateAt(cellPos);
			DoSpawnOnDeconstruct(interaction);
		}
		else if (DeconstructionType == DeconstructionType.Wrench)
		{
			tileChangeManager.RemoveTile(Vector3Int.RoundToInt(cellPos), tile.LayerType);
			SoundManager.PlayNetworkedAtPos("Wrench", interaction.WorldPositionTarget, Random.Range(0.8f, 1.2f));
			tileChangeManager.SubsystemManager.UpdateAt(cellPos);
			DoSpawnOnDeconstruct(interaction);
		}
		else if (DeconstructionType == DeconstructionType.NormalWall)
		{
			//unweld to a girder
			var progressFinishAction = new ProgressCompleteAction(
				() =>
				{
					SoundManager.PlayNetworkedAtPos("Weld", interaction.WorldPositionTarget, 0.8f);
					tileChangeManager.RemoveTile(cellPos, LayerType.Walls);
					SoundManager.PlayNetworkedAtPos("Deconstruct", interaction.WorldPositionTarget, 1f);

					//girder / metal always appears in place of deconstructed wall
					Spawn.ServerPrefab(CommonPrefabs.Instance.Girder, interaction.WorldPositionTarget);
					Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, interaction.WorldPositionTarget);
					tileChangeManager.SubsystemManager.UpdateAt(cellPos);
				}
			);

			//Start the progress bar:
			var bar = UIManager.ServerStartProgress(ProgressAction.Construction, interaction.WorldPositionTarget,
				10f, progressFinishAction, interaction.Performer);
			if (bar != null)
			{
				SoundManager.PlayNetworkedAtPos("Weld", interaction.WorldPositionTarget, Random.Range(0.9f, 1.1f));
			}
		}
		//TODO: Deconstruct reinforced wall, will spawn a prefab to handle deconstruction, not yet implemented
	}

	private void DoSpawnOnDeconstruct(PositionalHandApply interaction)
	{
		if (SpawnOnDeconstruct != null)
		{
			//always spawn at least one floor tile.
			var amount = TileType == TileType.Floor ? Math.Max(1, spawnAmountOnDeconstruct) : spawnAmountOnDeconstruct;
        	Spawn.ServerPrefab(SpawnOnDeconstruct, interaction.WorldPositionTarget, count: amount);
        }
	}

	private GameObject DefaultSpawnOnDeconstruct()
	{
		if (LayerType == LayerType.Floors)
		{
			return CommonPrefabs.Instance.FloorTile;
		}

		return null;
	}


	private DeconstructionType DefaultDeconstructionType()
	{
		if (LayerType == LayerType.Floors)
		{
			return DeconstructionType.Crowbar;
		}

		return DeconstructionType.None;
	}
}

/// <summary>
/// Identifies the deconstruction logic that should be used for this tile
/// </summary>
public enum DeconstructionType
{
	//can't deconstruct
	None = 0,
	//pry off with crowbar
	Crowbar = 1,
	//snip with wirecutters
	Wirecutters = 2,
	//dismantle with wrench
	Wrench = 3,
	//cut with to turn into girder
	NormalWall = 4,
	//elaborate reinforced wall logic
	ReinforcedWall = 5
}