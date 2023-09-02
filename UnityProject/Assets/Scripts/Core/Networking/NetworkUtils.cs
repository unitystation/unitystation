
using Logs;
using Mirror;
using UnityEngine;

/// <summary>
/// Utils for working with networking.
/// </summary>
public static class NetworkUtils
{
	/// <summary>
	/// Tries to find the object with the given net ID, returns null if unsuccessful. Logs
	/// a warning only if the netID is not empty and not invalid but not found.
	/// </summary>
	/// <param name="netId"></param>
	/// <returns></returns>
	public static GameObject FindObjectOrNull(uint netId)
	{
		if (netId == NetId.Invalid || netId == NetId.Empty)
		{
			return null;
		}

		var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;

		if (spawned.TryGetValue(netId, out var networkIdentity))
		{
			if (networkIdentity == null)
			{
				Loggy.LogWarningFormat("Network(Server/Client).spawned.TryGetValue was true but networkIdentity var is null.", Category.Server);
				return null;
			}

			return networkIdentity.gameObject;
		}

		Loggy.LogWarningFormat("Unable to find object with id {0}.", Category.Server, netId);
		return null;
	}
}
