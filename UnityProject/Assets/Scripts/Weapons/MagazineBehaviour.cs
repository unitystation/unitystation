using UnityEngine.Networking;

/// <summary>
/// Tracks the ammo in a magazine. Note that if you are referencing the ammo count stored in this
/// behavior, server and client ammo counts are stored separately but can be synced with SyncClientAmmoRemainsWithServer().
/// </summary>
public class MagazineBehaviour : NetworkBehaviour
{
	/*
	We keep track of 2 ammo counts. The server's ammo count is authoritative, but when ammo is being
	rapidly expended, we cannot rely on it for an accurate count client-side. We could shoot three shots clientside
	before the server registers a single shot, and it would cause our ammo count to be set to a too-high value
	when it processes the shot due to the latency in setting the syncvar
	(server thinks we've only shot once when we've already shot thrice). So instead
	we keep our own private ammo count (clientAmmoRemains) and only sync it up with the server when we need it
	(such as when reloading or when picking up a weapon with this mag in it).
	*/
	[SyncVar] private int serverAmmoRemains;
	private int clientAmmoRemains;
	/// <summary>
	/// Remaining ammo in the clip. On the server, this will always be get/set using the server
	/// authoritative value. On the client, this will use our local ammoRemains count, which can be synced
	/// with the server-authoritative count using SyncClientAmmoRemainsWithServer()
	/// </summary>
	public int ammoRemains
	{
		get
		{
			if (isServer)
			{
				return serverAmmoRemains;
			}
			else
			{
				return clientAmmoRemains;
			}
		}
		set
		{
			if (isServer)
			{
				serverAmmoRemains = value;
			}
			clientAmmoRemains = value;
		}
	}

	public string ammoType; //SET IT IN INSPECTOR
	public int magazineSize = 20;

	private void Start()
	{
		if (isServer)
		{
			ammoRemains = magazineSize;
		}

	}

	public override void OnStartClient()
	{
		SyncClientAmmoRemainsWithServer();
	}

	/// <summary>
	/// Sets our local ammo count to match the server's latest ammo count.
	/// </summary>
	public void SyncClientAmmoRemainsWithServer()
	{
		clientAmmoRemains = serverAmmoRemains;
	}
}
