using System;
using Mirror;
using UnityEngine;

/// <summary>
/// Tracks the ammo in a magazine. Note that if you are referencing the ammo count stored in this
/// behavior, server and client ammo counts are stored separately but can be synced with SyncClientAmmoRemainsWithServer().
/// </summary>
public class MagazineBehaviour : NetworkBehaviour, IServerSpawn, IExaminable, ICheckedInteractable<InventoryApply>
{
	/*
	We keep track of 2 ammo counts. The server's ammo count is authoritative, but when ammo is being
	rapidly expended, we cannot rely on it for an accurate count client-side. We could shoot three shots clientside
	before the server registers a single shot, and it would cause our ammo count to be set to a too-high value
	when it processes the shot due to the latency in setting the syncvar
	(server thinks we've only shot once when we've already shot thrice). So instead
	we keep our own private ammo count (clientAmmoRemains) and only sync it up with the server when we need it
	*/
	[SyncVar(hook = "SyncServerAmmo")]
	private int serverAmmoRemains;
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


	private double[] RNGContents;

	/// <summary>
	///	Whether this can be used to reload other (internal or external) magazines.
	/// </summary>
	public bool isClip;

	public AmmoType ammoType; //SET IT IN INSPECTOR
	public int magazineSize = 20;

	/// <summary>
	/// RNG whose seed is based on the netID of the magazine and which provides a random value based
	/// on how much ammo the magazine has left.
	/// </summary>
	private System.Random magSyncedRNG;

	public override void OnStartClient()
	{
		SetupRNG();
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
		//set to max ammo on initialization
		clientAmmoRemains = -1;
		SyncServerAmmo(magazineSize, magazineSize);
		SetupRNG();
	}

	/// <summary>
	/// Changes size of magazine and reloads it. Be sure to call this on every client and the server if you do, or face the consequences.
	/// Also sets the contained ammunition to full.
	/// </summary>
	/// <param name="newSize"></param>
	public void ChangeSize(int newSize)
	{
		magazineSize = newSize;
		clientAmmoRemains = -1;
		SyncServerAmmo(newSize, newSize);
		SetupRNG();
	}

	/// <summary>
	/// Creates the RNG table.
	/// </summary>
	public void SetupRNG()
	{
		RNGContents = new double[magazineSize + 1];
		magSyncedRNG = new System.Random(GetComponent<NetworkIdentity>().netId.GetHashCode());
		for (int i = 0; i <= magazineSize; i++)
		{
			RNGContents[magazineSize - i] = magSyncedRNG.NextDouble();
		}
	}

	/// <summary>
	/// Syncs server and client ammo.
	/// </summary>
	private void SyncServerAmmo(int oldAmmo, int newAmmo)
	{
		serverAmmoRemains = newAmmo;
		clientAmmoRemains = serverAmmoRemains;
	}

	/// <summary>
	/// Decrease ammo count by given number.
	/// </summary>
	/// <returns></returns>
	public virtual void ExpendAmmo(int amount = 1)
	{
		if (ClientAmmoRemains < amount)
		{
			Logger.LogWarning("Client ammo count is too low, cannot expend that much ammo. Make sure" +
							  " to check ammo count before expending it.", Category.Firearms);
		}
		else
		{
			clientAmmoRemains -= amount;
		}

		if (isServer)
		{
			if (ServerAmmoRemains < amount)
			{
				Logger.LogWarning("Server ammo count is too low, cannot expend that much ammo. Make sure" +
								  " to check ammo count before expending it.", Category.Firearms);
			}
			else
			{
				SyncServerAmmo(serverAmmoRemains, serverAmmoRemains - amount);
				if (isClip && serverAmmoRemains == 0)
				{
					Despawn.ServerSingle(gameObject);
				}
			}
		}

		Logger.LogTraceFormat("Expended {0} shots, now serverAmmo {1} clientAmmo {2}", Category.Firearms, amount, serverAmmoRemains, clientAmmoRemains);
	}

	/// <summary>
	/// Manually set remaining server-side ammo count
	/// </summary>
	/// <param name="remaining"></param>
	[Server]
	public void ServerSetAmmoRemains(int remaining)
	{
		SyncServerAmmo(serverAmmoRemains, remaining);
	}

	/// <summary>
	/// Loads as much ammo as possible from the given clip. Returns reloading message.
	/// </summary>
	public String LoadFromClip( MagazineBehaviour clip)
	{
		if (clip == null) return "";
		Logger.Log(magazineSize + "-" + serverAmmoRemains + "," + clip.serverAmmoRemains);

		int toTransfer = Math.Min(magazineSize - serverAmmoRemains, clip.serverAmmoRemains);

		clip.ExpendAmmo(toTransfer);
		ServerSetAmmoRemains(serverAmmoRemains + toTransfer);

		return ("Loaded " + toTransfer + (toTransfer == 1 ? " piece" : " pieces") + " of ammunition.");
	}

	/// <summary>
	/// Returns true if it is possible to fill this magazine with the interaction target object,
	/// which occurs when the interaction target is a clip of the same ammo type.
	/// </summary>
	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		MagazineBehaviour mag = interaction.TargetObject.GetComponent<MagazineBehaviour>();

		if (mag == null) return false;
		if (interaction.UsedObject == null) return false;
		if (mag.ammoType != ammoType || !isClip) return false;

		return true;
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		if (interaction.UsedObject == null || interaction.Performer == null) return;
		MagazineBehaviour clip = interaction.UsedObject.GetComponent<MagazineBehaviour>();
		MagazineBehaviour usedclip = interaction.TargetObject.GetComponent<MagazineBehaviour>();
		string message = usedclip.LoadFromClip(clip);
		Chat.AddExamineMsg(interaction.Performer, message);
	}


	/// <summary>
	/// Gets an RNG double which is based on the current ammo remaining and this mag's net ID so client
	///  can predict deviation / recoil based on how many shots.
	/// </summary>
	/// <returns></returns>
	public double CurrentRNG()

	{
		double currentRNG = RNGContents[clientAmmoRemains];
		Logger.LogTraceFormat("rng {0}, serverAmmo {1} clientAmmo {2}", Category.Firearms, currentRNG, serverAmmoRemains, clientAmmoRemains);
		return currentRNG;
	}

	public String Examine(Vector3 pos)
	{
		return "Accepts " + ammoType + " rounds (" + (ServerAmmoRemains > 0 ? (ServerAmmoRemains.ToString() + " left") : "empty") + ")";
	}
}

public enum AmmoType
{
	_9mm,
	uzi9mm,
	smg9mm,
	tommy9mm,
	_10mm,
	_46mm,
	_50Cal,
	_556mm,
	_38,
	_45,
	_44,
	_357,
	_762,
	_712x82mm,
	FusionCells,
	Slug,
	Syringe,
	Gasoline,
	Internal,
	_762x38mmR,
	_84mm,
	FoamForceDart,
	_75,
	_40mm
}