
using UnityEngine;

/// <summary>
/// Holds details about how an object is being despawned on the client. This is mostly
/// a placeholder which prevents having to change the signature of the lifecycle interfaces in the event
/// that some component ends up needing some extra info about how it is being spawned.
///
/// This is different from DespawnInfo because we don't necesarilly want the client to know the full extent
/// of why something was despawned.
/// </summary>
public class ClientDespawnInfo
{
	private static ClientDespawnInfo defaultInstance = new ClientDespawnInfo(null);
	//note: currently no data is needed but fields may be added lat

	private ClientDespawnInfo(GameObject clonedFrom)
	{

	}

	public static ClientDespawnInfo Default()
	{
		return defaultInstance;
	}
}
