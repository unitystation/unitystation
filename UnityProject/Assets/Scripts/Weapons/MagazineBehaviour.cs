using System;
using Mirror;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tracks the ammo in a magazine. Note that if you are referencing the ammo count stored in this
/// behavior, server and client ammo counts are stored separately but can be synced with SyncClientAmmoRemainsWithServer().
/// </summary>
public class MagazineBehaviour : NetworkBehaviour, IServerSpawn, IExaminable, ICheckedInteractable<InventoryApply>, IClientInteractable<HandActivate>
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

	[SerializeField]
	public GameObject containedCartridge = null;

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
	[HideInInspector, Tooltip("Defines if this can be used to reload other magazines, clips or be used as an internal mag"), HideIf(nameof(isCartridge))]
	public bool isClip;

	[HideInInspector, HideIf(nameof(isClip))]
	public bool isCartridge;

	public AmmoType ammoType; //SET IT IN INSPECTOR

	[HideInInspector, HideIf(nameof(isCartridge))]
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

	private void Init()
	{
		if (isClip)
		{
			isCartridge = false;
			magazineSize = 1;
		}
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
				if (isClip && serverAmmoRemains == 0 || isCartridge && serverAmmoRemains == 0)
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
		int toTransfer = Math.Min(magazineSize - serverAmmoRemains, clip.serverAmmoRemains);

		clip.ExpendAmmo(toTransfer);
		ServerSetAmmoRemains(serverAmmoRemains + toTransfer);

		return ("Loaded " + toTransfer + (toTransfer == 1 ? " piece" : " pieces") + " of ammunition.");
	}

	public String UnloadFromClip( MagazineBehaviour clip, int toTransfer)
	{
		if (clip == null) return "";
		clip.ExpendAmmo(toTransfer);
		ServerSetAmmoRemains(serverAmmoRemains - toTransfer);
		return ("Unloaded " + toTransfer + (toTransfer == 1 ? " piece" : " pieces") + " of ammunition.");
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
		if (mag == this) return false;
		if (mag.ammoType != ammoType || !isClip && !isCartridge) return false;

		return true;
	}

	public bool Interact(HandActivate interaction) //someone has pressed z or clicked on us
	{
		if (!isCartridge)
		{
			if (ServerAmmoRemains > 0)
			{
				//We are a clip/mag with atleast one cartridge inside us, unload it.
				return true;
			}
			else
			{
				//We are a clip/mag with no cartridges inside us, get them from the world
				return true;
			}
			
		}
		return false;
	}

	public bool Interact(InventoryApply interaction) // someone has used an object on us
	{
		if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot)
		{ 
			MagazineBehaviour UsedObjectMagScript = interaction.UsedObject.GetComponent<MagazineBehaviour>();
			MagazineBehaviour TargetObjectMagScript = interaction.TargetObject.GetComponent<MagazineBehaviour>();

			if (interaction.UsedObject == null)
			{
				//We are the target object and an empty hand has been used on us, unload cartridge into hand
				return true;
			}
			else if (UsedObjectMagScript.isCartridge || !TargetObjectMagScript.isCartridge)
			{
				//We are the target object which is a clip or mag and a cartridge has been used on us, load it.;
				return true;
			}
		}
		return false;	
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		if (interaction.UsedObject == null || interaction.Performer == null) return;

		MagazineBehaviour UsedObjectMagScript = interaction.UsedObject.GetComponent<MagazineBehaviour>();
		MagazineBehaviour TargetObjectMagScript = interaction.TargetObject.GetComponent<MagazineBehaviour>();

		if (interaction.UsedObject == null)
		{
			var yes = interaction.UsedObject.GetComponent<ItemStorage>();
			//We are the target object and an empty hand has been used on us, unload cartridge into hand
			RemoveCartridgeToHand(interaction.FromSlot);
		} // mag or clip to cartridge or clip to mag
		else if (UsedObjectMagScript.isCartridge && !TargetObjectMagScript.isCartridge || UsedObjectMagScript.isClip && !TargetObjectMagScript.isCartridge)
		{
			//We are a clip/mag with a clip or cartridge being used on us
			Chat.AddExamineMsg(interaction.Performer, LoadFromClip(UsedObjectMagScript));
		}
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (!isCartridge)
		{
			if (ServerAmmoRemains > 0)
			{
				//We are a clip/mag with atleast one cartridge inside us, unload it.
				RemoveCartridgeToWorld(interaction.UsedObject.GetComponent<MagazineBehaviour>(), interaction.Performer);
			}
			else
			{
				//We are a clip/mag with no cartridges inside us, get them from the world
				InsertCartridgeFromWorld(interaction.Performer);
			}
			
		}
	}

	public void RemoveCartridgeToHand(ItemSlot slot)
	{
		Inventory.ServerAdd(containedCartridge, slot, ReplacementStrategy.DropOther);
	}

	public void RemoveCartridgeToWorld(MagazineBehaviour clip, GameObject player)
	{
		UnloadFromClip(clip,1);
		SoundManager.PlayNetworkedAtPos("EmptyGunClick", transform.position, sourceObj: player);
		Spawn.ServerPrefab(containedCartridge, transform.position, transform.parent);
	}

	public void InsertCartridgeFromWorld(GameObject player)
	{

		Vector3Int pos = V3ToV3I(player.transform.position);
		var crossedItems = MatrixManager.GetAt<Pickupable>(pos, true);
			int ammoCount = 0;
			int maxadd = magazineSize - serverAmmoRemains;
		foreach (var item in crossedItems)
		{
			var script = item.GetComponent<MagazineBehaviour>();
			if (script.isCartridge && script.ammoType == ammoType)
			{
				if (ammoCount < maxadd)
				{
					Despawn.ServerSingle(gameObject);
					ammoCount++;
				}
				else
				{
					break;
				}
			}
			ServerSetAmmoRemains(serverAmmoRemains + ammoCount);
			Chat.AddExamineMsg(player, ("Loaded " + ammoCount + (ammoCount == 1 ? " piece" : " pieces") + " of ammunition."));
		}
	}

	public Vector3Int V3ToV3I(Vector3 V3)
	{
		int newX = (int)Math.Truncate(V3.x);
		int newY = (int)Math.Truncate(V3.y);
		int newZ = (int)Math.Truncate(V3.x);
		Vector3Int V3I = new Vector3Int(newX, newY, newZ);
		return V3I;
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
		if (!isCartridge)
		{
			return "Accepts " + ammoType + " rounds (" + (ServerAmmoRemains > 0 ? (ServerAmmoRemains.ToString() + " left") : "empty") + ")";
		}
		else
		{
			return "A single round of " + ammoType;
		}
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
	_50mm,
	_556mm,
	_38,
	_45,
	_50,
	_357,
	_762,
	A762,
	FusionCells,
	Slug,
	Syringe,
	Gasoline,
	Internal
}

#if UNITY_EDITOR
	[CustomEditor(typeof(MagazineBehaviour), true)]
	public class MagazineBehaviourEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			MagazineBehaviour script = (MagazineBehaviour)target;

			DrawDefaultInspector(); // for other non-HideInInspector fields
			if (!script.isClip) // show exclusive fields depending on whether magazine is internal
			{
				script.isCartridge = EditorGUILayout.Toggle("isCartridge", script.isCartridge);
			}
			if (!script.isCartridge)
			{
				script.magazineSize = EditorGUILayout.IntField("Magazine Size", script.magazineSize);
				script.isClip = EditorGUILayout.Toggle("isClip", script.isClip);			
			}

		}
	}
#endif
