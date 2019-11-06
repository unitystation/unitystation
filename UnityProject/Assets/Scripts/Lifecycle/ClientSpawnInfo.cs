
using UnityEngine;

/// <summary>
/// Holds details about how an object is being spawned on the client. This is mostly
/// a placeholder which prevents having to change the signature of the lifecycle interfaces in the event
/// that some component ends up needing some extra info about how it is being spawned.
///
/// This is different from SpawnInfo because we don't necesarilly want the client to know the full extent
/// of why something was spawned.
/// </summary>
public class ClientSpawnInfo
{
	private static ClientSpawnInfo defaultInstance = new ClientSpawnInfo(null,  ClientSpawnType.Default);
	private static ClientSpawnInfo mappedInstance = new ClientSpawnInfo(null,  ClientSpawnType.Mapped);
	//note: currently no data is needed but fields may be added later
	public readonly GameObject ClonedFrom;

	/// <summary>
	/// Type of spawn being performed. Based on this value, extra information
	/// will be available in this spawn info to describe the details of the spawn,
	/// </summary>
	public ClientSpawnType SpawnType;
	public bool IsCloned => ClonedFrom != null;

	private ClientSpawnInfo(GameObject clonedFrom, ClientSpawnType spawnType)
	{
		ClonedFrom = clonedFrom;
		SpawnType = spawnType;
	}

	/// <summary>
	/// Regular spawn - server telling client to spawn something.
	/// </summary>
	/// <returns></returns>
	public static ClientSpawnInfo Default()
	{
		return defaultInstance;
	}

	/// <summary>
	/// Special type of spawn, performed on each object mapped in the scene once the scene is done loading.
	/// </summary>
	/// <returns></returns>
	public static ClientSpawnInfo Mapped()
	{
		return mappedInstance;
	}

	public static ClientSpawnInfo Cloned(GameObject clonedFrom)
	{
		return new ClientSpawnInfo(clonedFrom, ClientSpawnType.Default);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="clonedFrom">object cloned from, null if not a clone</param>
	/// <returns></returns>
	public static ClientSpawnInfo Create(GameObject clonedFrom)
	{
		if (clonedFrom)
		{
			return ClientSpawnInfo.Cloned(clonedFrom);
		}
		else
		{
			return ClientSpawnInfo.Default();
		}
	}
}

/// <summary>
/// Defines the type / manner in which something is being spawned
/// </summary>
public enum ClientSpawnType
{
	/// <summary>
	/// Normal spawning, no extra data will be in this spawn info
	/// </summary>
	Default = 0,
	/// <summary>
	/// Object was already mapped into the scene and scene has loaded.
	/// </summary>
	Mapped = 2
}
