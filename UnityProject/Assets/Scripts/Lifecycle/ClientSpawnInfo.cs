
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
	private static ClientSpawnInfo defaultInstance = new ClientSpawnInfo(null);
	//note: currently no data is needed but fields may be added later
	public readonly GameObject ClonedFrom;
	public bool IsCloned => ClonedFrom != null;

	private ClientSpawnInfo(GameObject clonedFrom)
	{
		ClonedFrom = clonedFrom;
	}

	public static ClientSpawnInfo Default()
	{
		return defaultInstance;
	}

	public static ClientSpawnInfo Cloned(GameObject clonedFrom)
	{
		return new ClientSpawnInfo(clonedFrom);
	}
}
