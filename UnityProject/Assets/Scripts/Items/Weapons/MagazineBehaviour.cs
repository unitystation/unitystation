using System;
using System.Collections.Generic;
using Logs;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

namespace Weapons
{
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
		[SyncVar(hook = nameof(SyncServerAmmo))]
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
		///	The type of magazine. This effects various behaviours depending on its setting
		/// </summary>
		public MagType magType;

		[SerializeField, FormerlySerializedAs("Projectile")]
		public GameObject initalProjectile;
		public int ProjectilesFired = 1;

		[NonSerialized]
		public List<int> containedProjectilesFired;

		[NonSerialized]
		public List<GameObject> containedBullets;
		public AmmoType ammoType; //SET IT IN INSPECTOR
		public int magazineSize = 20;

		public override void OnStartClient()
		{
			InitLists();
			SetupRng();
		}

		public override void OnStartServer()
		{
			SetupRng();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			ServerInit();
		}

		private void ServerInit()
		{
			//set to max ammo on initialization
			clientAmmoRemains = -1;
			if (magType != MagType.Clip)
			{
				InitLists();
			}
			SyncServerAmmo(magazineSize, magazineSize);
			SetupRng();
		}

		public virtual void InitLists()
		{
			containedProjectilesFired  = new List<int>(magazineSize);
			containedBullets  = new List<GameObject>(magazineSize);
			for (int i = magazineSize; i != 0; i--)
			{
				containedBullets.Add(initalProjectile);
				containedProjectilesFired.Add(ProjectilesFired);
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
			SetupRng();
		}

		/// <summary>
		/// Creates the RNG table.
		/// </summary>
		public void SetupRng()
		{
			RNGContents = new double[magazineSize + 1];
			System.Random magSyncedRNG = new System.Random(GetComponent<NetworkIdentity>().netId.GetHashCode());
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
			if (amount < 0)
			{
				Loggy.LogWarning("Attempted to expend a negitive amount of ammo", Category.Firearms); // dont use this method to replenish ammo
			}

			if (ClientAmmoRemains < amount)
			{
				Loggy.LogWarning("Client ammo count is too low, cannot expend that much ammo. Make sure" +
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
					Loggy.LogWarning("Server ammo count is too low, cannot expend that much ammo. Make sure" +
									  " to check ammo count before expending it.", Category.Firearms);
				}
				else
				{
					var remaining = serverAmmoRemains - amount;
					SyncServerAmmo(remaining, remaining);
					if (magType == MagType.Standard)
					{
						for (int i = amount;i != 0;i--)
						{
							containedBullets.RemoveAt(0); //remove shot projectile
							containedProjectilesFired.RemoveAt(0);
						}
					}
					if (magType == MagType.Clip && serverAmmoRemains == 0)
					{
						_ = Despawn.ServerSingle(gameObject);
					}
				}

				Loggy.LogTraceFormat("Expended {0} shots, now serverAmmo {1} clientAmmo {2}", Category.Firearms, amount, serverAmmoRemains, clientAmmoRemains);
			}
		}

		/// <summary>
		/// Manually set remaining server-side ammo count
		/// </summary>
		/// <param name="remaining"></param>
		[Server]
		public void ServerSetAmmoRemains(int remaining)
		{
			SyncServerAmmo(remaining, remaining);
		}

		/// <summary>
		/// Loads as much ammo as possible from the given clip. Returns reloading message.
		/// </summary>
		public String LoadFromClip(MagazineBehaviour clip)
		{
			if (clip == null) return "";

			int toTransfer = Math.Min(magazineSize - serverAmmoRemains, clip.serverAmmoRemains);

			if (clip.serverAmmoRemains == 0)
			{
				return $"{clip.gameObject.ExpensiveName()} is empty!";
			}
			else if (toTransfer == 0)
			{
				return $"{gameObject.ExpensiveName()} is full";
			}

			clip.ExpendAmmo(toTransfer);

			if (magType == MagType.Standard)
			{
				for (int i = toTransfer;i != 0;i--)
				{
					containedBullets.Add(clip.initalProjectile);
					containedProjectilesFired.Add(clip.ProjectilesFired);
				}
			}

			ServerSetAmmoRemains(serverAmmoRemains + toTransfer);

			var plural = toTransfer == 1 ? "" : "s";
			return $"Loaded {toTransfer} round{plural}";
		}

		/// <summary>
		/// method to add info to the projectile array,
		/// should be used when ammo is being increased outside of the reload logic
		/// </summary>
		public void LoadProjectile(GameObject projectile,int projectilesfired)
		{
			containedBullets.Add(projectile);
			containedProjectilesFired.Add(projectilesfired);
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
			if (mag.ammoType != ammoType || magType != MagType.Clip) return false;

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
		public double CurrentRng()
		{
			double CurrentRng = 1.0;
			if (clientAmmoRemains <= RNGContents.Length - 1)
			{
				CurrentRng = RNGContents[clientAmmoRemains];
			}

			Loggy.LogTraceFormat("rng {0}, serverAmmo {1} clientAmmo {2}", Category.Firearms, CurrentRng, serverAmmoRemains, clientAmmoRemains);
			return CurrentRng;
		}

		public virtual String Examine(Vector3 pos)
		{
			return $"Accepts {ammoType}\n It has {ServerAmmoRemains} out of {magazineSize} rounds within";
		}
	}

	public enum MagType
	{
		Standard,
		Clip,
		Cell
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
		_40mm,
		Arrow
	}
}