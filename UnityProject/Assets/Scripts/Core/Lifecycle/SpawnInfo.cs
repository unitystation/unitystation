
using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

/// <summary>
/// Describes (but does not actually perform) an attempt to spawn things.
/// This is used to perform the described spawning (in Spawn) as well as pass the information to
/// lifecycle hook interface implementers
/// </summary>
public class SpawnInfo
{

	/// <summary>
	/// Type of spawn being performed. Based on this value, extra information
	/// will be available in this spawn info to describe the details of the spawn,
	/// </summary>
	public readonly SpawnType SpawnType;

	/// <summary>
	/// GameObject to clone if SpawnType.Clone
	/// </summary>
	public readonly GameObject ClonedFrom;


	/// <summary>
	/// Spawnable which will be spawned.
	/// </summary>
	public readonly ISpawnable SpawnableToSpawn;

	/// <summary>
	/// Destination to spawn at
	/// </summary>
	public readonly SpawnDestination SpawnDestination;

	/// <summary>
	/// If spawning not hidden, applies this slight amount of random scattering to the spawned objects. Null
	/// if no scattering should be done.
	/// </summary>
	public readonly float? ScatterRadius;

	/// <summary>
	/// Number of instances to spawn.
	/// </summary>
	public readonly int Count;

	/// <summary>
	/// If SpawnType.Player, occupation the player is being spawned with.
	/// </summary>
	public readonly Occupation Occupation;

	/// <summary>
	/// If SpawnType.Player, character settings the player is being spawned with.
	/// </summary>
	/// <returns></returns>
	public readonly CharacterSettings CharacterSettings;

	/// <summary>
	/// The gear or items that spawn on creation will be enabled or not. ex: equipment on characters.
	/// </summary>
	public readonly bool SpawnItems;

	private SpawnInfo(SpawnType spawnType, ISpawnable spawnable, SpawnDestination spawnDestination, float? scatterRadius, int count, Occupation occupation,
		GameObject clonedFrom = null,
		CharacterSettings characterSettings = null, bool spawnItems = true)
	{
		SpawnType = spawnType;
		SpawnableToSpawn = spawnable;
		SpawnDestination = spawnDestination;
		ScatterRadius = scatterRadius;
		Count = count;
		Occupation = occupation;
		ClonedFrom = clonedFrom;
		CharacterSettings = characterSettings;
		SpawnItems = spawnItems;
	}

	/// <summary>
	/// Spawn a player with the specified occupation
	/// </summary>
	/// <param name="occupation">Occupation details to use to spawn this player</param>
	/// <param name="characterSettings">settings to use for this player</param>
	/// <param name="playerPrefab">Prefab to use to spawn this player</param>
	/// <param name="spawnDestination">destinaton to spawn at</param>
	/// <param name="spawnItems">whether player should spawn naked or with their default loadout</param>
	/// <returns>the newly created GameObject</returns>
	/// <returns></returns>
	public static SpawnInfo Player(Occupation occupation, CharacterSettings characterSettings, GameObject playerPrefab, SpawnDestination spawnDestination,
		bool spawnItems = false)
	{
		return new SpawnInfo(SpawnType.Player, SpawnablePrefab.For(playerPrefab), spawnDestination, null, 1, occupation, characterSettings: characterSettings, spawnItems: spawnItems);
	}

	/// <summary>
	/// Spawn a ghost with the specified occupation
	/// </summary>
	/// <param name="occupation">Occupation details to use to spawn this ghost</param>
	/// <param name="characterSettings">settings to use for this ghost</param>
	/// <param name="ghostPrefab">Prefab to use to spawn this ghost</param>
	/// <param name="spawnDestination">destinaton to spawn at</param>
	/// <returns>the newly created GameObject</returns>
	/// <returns></returns>
	public static SpawnInfo Ghost(Occupation occupation, CharacterSettings characterSettings, GameObject ghostPrefab,
		SpawnDestination spawnDestination)
	{
		return new SpawnInfo(SpawnType.Ghost, SpawnablePrefab.For(ghostPrefab), spawnDestination,
			null, 1, occupation, characterSettings: characterSettings);
	}

	/// <summary>
	/// Spawn the specified spawnable
	/// </summary>
	/// <param name="spawnable">Spawnable to spawn.</param>
	/// <param name="spawnDestination">destinaton to spawn at</param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <param name="cancelIfImpassable">If true, the spawn will be cancelled if the location being spawned into is totally impassable.</param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnInfo Spawnable(ISpawnable spawnable, SpawnDestination spawnDestination, int count = 1, float? scatterRadius = null, bool spawnItems = true)
	{
		return new SpawnInfo(SpawnType.Default, spawnable, spawnDestination, scatterRadius, count, null, spawnItems: spawnItems);
	}

	/// <summary>
	/// Clones the specified object
	/// </summary>
	/// <param name="toClone">gameobject to clone.</param>
	/// <param name="spawnDestination">destinaton to spawn the clone at</param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnInfo Clone(GameObject toClone, SpawnDestination spawnDestination, int count = 1, float? scatterRadius = null)
	{
		return new SpawnInfo(SpawnType.Clone, SpawnableClone.Of(toClone), spawnDestination, scatterRadius, count, null, toClone);
	}

	/// <summary>
	/// Special type of spawn, performed on each object mapped in the scene once the scene is done loading.
	/// </summary>
	/// <param name="mappedObject">object which was mapped into the scene.</param>
	/// <returns></returns>
	public static SpawnInfo Mapped(GameObject mappedObject)
	{
		var destination = SpawnDestination.At(mappedObject);
		//assume prefab
		var prefab = Spawn.DeterminePrefab(mappedObject);
		var spawnable = SpawnablePrefab.For(prefab);
		return new SpawnInfo(SpawnType.Mapped, spawnable, destination, null, 1, null);

	}

	private static Transform DefaultParent(Transform parent, Vector3? worldPos)
	{
		return parent != null ? parent : MatrixManager.GetDefaultParent(worldPos, true);
	}

	public override string ToString()
	{
		return $"{nameof(SpawnType)}: {SpawnType}, {nameof(ClonedFrom)}: {ClonedFrom}, {nameof(SpawnableToSpawn)}: " +
		       $"{SpawnableToSpawn}, {nameof(SpawnDestination)}: {SpawnDestination}, {nameof(ScatterRadius)}: " +
		       $"{ScatterRadius}, {nameof(Count)}: {Count}, {nameof(Occupation)}: {Occupation}, " +
		       $"{nameof(CharacterSettings)}: {CharacterSettings}, {nameof(SpawnItems)}: {SpawnItems}";
	}
}

/// <summary>
/// Type of spawn being performed. This enum helps users to know what data
/// will be available.
/// </summary>
public enum SpawnType
{
	/// <summary>
	/// Normal spawning, no extra data will be in this spawn info
	/// </summary>
	Default = 0,
	/// <summary>
	/// Spawning a player, extra info related to this will be populated.
	/// </summary>
	Player = 1,
	/// <summary>
	/// Object was already mapped into the scene and scene has loaded.
	/// </summary>
	Mapped = 2,
	/// <summary>
	/// Spawning an NPC
	/// </summary>
	Mob = 3,
	/// <summary>
	/// Spawning a ghost
	/// </summary>
	Ghost = 4,
	/// <summary>
	/// Cloning something
	/// </summary>
	Clone = 5,
}
