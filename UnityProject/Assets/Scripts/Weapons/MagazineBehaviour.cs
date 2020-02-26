using System;
using Mirror;
using UnityEngine;

/// <summary>
/// Tracks the ammo in a magazine. Note that if you are referencing the ammo count stored in this
/// behavior, server and client ammo counts are stored separately but can be synced with SyncClientAmmoRemainsWithServer().
/// </summary>
public class MagazineBehaviour : NetworkBehaviour, IServerSpawn, IExaminable
{
	/*
	We keep track of 2 ammo counts. The server's ammo count is authoritative, but when ammo is being
	rapidly expended, we cannot rely on it for an accurate count client-side. We could shoot three shots clientside
	before the server registers a single shot, and it would cause our ammo count to be set to a too-high value
	when it processes the shot due to the latency in setting the syncvar
	(server thinks we've only shot once when we've already shot thrice). So instead
	we keep our own private ammo count (clientAmmoRemains) and only sync it up with the server when we need it
	*/
	[SyncVar]
	private int serverAmmoRemains = -1;
	private int clientAmmoRemains;

	/// <summary>
	/// Remaining ammo, latest value synced from server. There will be lag in this while shooting a burst.
	/// </summary>
	public int ServerAmmoRemains => serverAmmoRemains;

	/// <summary>
	/// Remaining ammo, incorporating client side prediction. Never allowed to be more
	/// than the latest value received from server.
	/// </summary>
	public int ClientAmmoRemains => Math.Min(clientAmmoRemains, serverAmmoRemains);

	public AmmoType ammoType; //SET IT IN INSPECTOR
	public int magazineSize = 20;

	/// <summary>
	/// RNG whose seed is based on the netID of the magazine and which provides a random value based
	/// on how much ammo the magazine has left.
	/// </summary>
	private System.Random magSyncedRNG;

	private double currentRNG;

	public override void OnStartClient()
	{
		SyncPredictionWithServer();
	}

	public override void OnStartServer()
	{
		ServerInit();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		ServerInit();
	}

	private void ServerInit()
	{
		//set to max ammo if uninitialized
		if (serverAmmoRemains == -1)
		{
			serverAmmoRemains = magazineSize;
			SyncPredictionWithServer();
		}
	}

	/// <summary>
	/// Force client count to sync with server regardless of any prediction
	/// </summary>
	public void SyncPredictionWithServer()
	{
		magSyncedRNG = new System.Random(GetComponent<NetworkIdentity>().netId.GetHashCode());
		currentRNG = magSyncedRNG.NextDouble();
		//fast forward RNG based on how many shots are spent
		var shots = magazineSize - serverAmmoRemains;
		for (int i = 0; i < shots; i++)
		{
			currentRNG = magSyncedRNG.NextDouble();
		}
		clientAmmoRemains = serverAmmoRemains;
	}

	/// <summary>
	/// Decrease server's ammo count by one.
	/// </summary>
	/// <returns></returns>
	public void ExpendAmmo()
	{
		if (clientAmmoRemains > serverAmmoRemains)
		{
			//unusual situation, we think we have more ammo than the server thinks we do, so we should reset our client predicted
			//value to what the server thinks. This can happen when client has just picked up a gun that already has
			//spent ammo, as their count is not synced unless they are the one shooting.
			Logger.LogWarningFormat("Unusual ammo mismatch client {0} server {1}, syncing back to server" +
									" value.", Category.Firearms, clientAmmoRemains, serverAmmoRemains);
			SyncPredictionWithServer();
		}

		if (isServer)
		{
			if (ServerAmmoRemains <= 0)
			{
				Logger.LogWarning("Server ammo count is already zero, cannot expend more ammo. Make sure" +
								  " to check ammo count before expending it.", Category.Firearms);
			}
			else
			{
				serverAmmoRemains--;
			}
		}

		if (ClientAmmoRemains <= 0)
		{
			Logger.LogWarning("Client ammo count is already zero, cannot expend more ammo. Make sure" +
							  " to check ammo count before expending it.", Category.Firearms);
		}
		else
		{
			clientAmmoRemains--;
			currentRNG = magSyncedRNG.NextDouble();
		}

		Logger.LogTraceFormat("Expended shot, now serverAmmo {0} clientAmmo {1}", Category.Firearms, serverAmmoRemains, clientAmmoRemains);
	}

	/// <summary>
	/// Manually set remaining server-side ammo count
	/// </summary>
	/// <param name="remaining"></param>
	[Server]
	public void ServerSetAmmoRemains(int remaining)
	{
		serverAmmoRemains = remaining;
		//fast forward RNG based on how many shots are spent
		SyncPredictionWithServer();
	}

	/// <summary>
	/// Gets an RNG double which is based on the current ammo remaining and this mag's net ID so client
	///  can predict deviation / recoil based on how many shots.
	/// </summary>
	/// <returns></returns>
	public double CurrentRNG()
	{
		Logger.LogTraceFormat("rng {0}, serverAmmo {1} clientAmmo {2}", Category.Firearms, currentRNG, serverAmmoRemains, clientAmmoRemains);
		return currentRNG;
	}

	public String Examine(Vector3 pos)
	{
		return "Accepts " + ammoType + " rounds (" + (ServerAmmoRemains > 0?(ServerAmmoRemains.ToString() + " left"):"empty") + ")";
	}
}

public enum AmmoType
{
	_12mm,
	_5Point56mm,
	_9mm,
	_38,
	_46x30mmtT,
	_50mm,
	_357mm,
	A762,
	FusionCells,
	Slug,
	smg9mm,
	Syringe,
	uzi9mm
}