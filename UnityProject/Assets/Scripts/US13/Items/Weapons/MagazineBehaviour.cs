using System;
using System.Collections.Generic;
using Logs;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Systems.Inventory;
using Util;

namespace US13.Items.Weapons
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
		/// Subscribers can listen to this to be notified when server ammo is updated.
		/// Parameters: (oldAmmo, newAmmo).
		/// </summary>
		public event Action<int, int> OnServerAmmoChanged;

		/// <summary>
		/// Remaining ammo, latest value synced from server. There will be lag in this while shooting a burst.
		/// </summary>
		public int ServerAmmoRemains => serverAmmoRemains;

		/// <summary>
		/// Remaining ammo, incorporating client side prediction. Never allowed to be more
		/// than the latest value received from server.
		/// </summary>
		public int ClientAmmoRemains => Math.Min(clientAmmoRemains, serverAmmoRemains);

		/// <summary>
		///	The type of magazine. This effects various behaviours depending on its setting
		/// </summary>
		public MagType magType;

		[Tooltip("If true, a clip type magazine despawns when its ammo reaches zero. For keeping empty speedloaders but deleting stripper clips.")]
		public bool clipDespawnWhenEmpty = true;

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
		}

		public virtual void InitLists()
		{
			containedProjectilesFired = new List<int>(magazineSize);
			containedBullets = new List<GameObject>(magazineSize);
			for (int i = 0; i < magazineSize; i++)
			{
				LoadProjectile(initalProjectile, ProjectilesFired);
			}
		}

		/// <summary>
		/// Syncs server and client ammo.
		/// </summary>
		private void SyncServerAmmo(int oldAmmo, int newAmmo)
		{
			serverAmmoRemains = newAmmo;
			clientAmmoRemains = serverAmmoRemains;
			OnServerAmmoChanged?.Invoke(oldAmmo, newAmmo);

			// If a clip becomes empty on the server and the clip type is configured to despawn,
			if (isServer && magType == MagType.Clip && serverAmmoRemains == 0 && clipDespawnWhenEmpty)
			{
				_ = Despawn.ServerSingle(gameObject);
			}
		}

		/// <summary>
		/// Decrease ammo count by given number.
		/// </summary>
		/// <returns></returns>
		public virtual void ExpendAmmo(int amount = 1)
		{
			if (amount < 0)
			{
				Loggy.Warning("Attempted to expend a negative amount of ammo", Category.Firearms);
				return;
			}

			if (ClientAmmoRemains < amount)
			{
				Loggy.Warning("Client ammo count is too low, cannot expend that much ammo. Make sure" +
				              " to check ammo count before expending it.", Category.Firearms);
			}
			else
			{
				clientAmmoRemains -= amount;
			}

			if (!isServer) return;

			if (ServerAmmoRemains < amount)
			{
				Loggy.Warning("Server ammo count is too low, cannot expend that much ammo. Make sure" +
				              " to check ammo count before expending it.", Category.Firearms);
				return;
			}

			var remaining = serverAmmoRemains - amount;
			SyncServerAmmo(serverAmmoRemains, remaining);

			if (magType == MagType.Standard)
			{
				for (int i = 0; i < amount; i++)
				{
					containedBullets.RemoveAt(0);
					containedProjectilesFired.RemoveAt(0);
				}
			}

			Loggy.Trace().Format("Expended {0} shots, now serverAmmo {1} clientAmmo {2}", Category.Firearms, amount, serverAmmoRemains, clientAmmoRemains);
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
		public string LoadFromClip(MagazineBehaviour clip)
		{
			if (clip == null) return "";

			int toTransfer = Math.Min(magazineSize - serverAmmoRemains, clip.serverAmmoRemains);

			if (clip.serverAmmoRemains == 0)
			{
				return $"{clip.gameObject.ExpensiveName()} is empty!";
			}
			if (toTransfer == 0)
			{
				return $"{gameObject.ExpensiveName()} is full";
			}

			clip.ExpendAmmo(toTransfer);

			if (magType == MagType.Standard)
			{
				for (int i = 0; i < toTransfer; i++)
				{
					LoadProjectile(clip.initalProjectile, clip.ProjectilesFired);
				}
			}

			ServerSetAmmoRemains(serverAmmoRemains + toTransfer);

			var plural = toTransfer == 1 ? "" : "s";
			return $"Loaded {toTransfer} round{plural}";
		}

		//I need more bullets!
		public string LoadBullet(BulletAmmo bullet)
		{
			if (serverAmmoRemains >= magazineSize)
			{
				return $"{gameObject.ExpensiveName()} is full";
			}

			if (magType == MagType.Standard)
			{
				var projectile = bullet.initalProjectile != null ? bullet.initalProjectile : initalProjectile;
				var projFired = bullet.initalProjectile != null ? bullet.ProjectilesFired : ProjectilesFired;
				LoadProjectile(projectile, projFired);
			}

			ServerSetAmmoRemains(serverAmmoRemains + 1);
			return "Loaded 1 round";
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
			if (interaction.UsedObject == null) return false;

			// Check for bullet loading (bullet can be in either hand)
			BulletAmmo bul = interaction.TargetObject.GetComponent<BulletAmmo>();
			if (bul == null) bul = interaction.UsedObject.GetComponent<BulletAmmo>();
			if (bul != null)
			{
				if (bul.ammoType != ammoType) return false;
				return true;
			}

			// Check for clip/mag-to-mag loading
			MagazineBehaviour mag = interaction.TargetObject.GetComponent<MagazineBehaviour>();
			if (mag != null)
			{
				if (mag.ammoType != ammoType || magType != MagType.Clip) return false;
				return true;
			}

			return false;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (interaction.UsedObject == null || interaction.Performer == null) return;

			// Check for bullet loading first (bullet can be in either hand)
			BulletAmmo bul = interaction.TargetObject.GetComponent<BulletAmmo>();
			GameObject bulletObj = interaction.TargetObject;
			if (bul == null)
			{
				bul = interaction.UsedObject.GetComponent<BulletAmmo>();
				bulletObj = interaction.UsedObject;
			}

			if (bul != null)
			{
				string message = LoadBullet(bul);
				Chat.AddExamineMsg(interaction.Performer, message);
				_ = Inventory.ServerDespawn(bulletObj);
			}
			else
			{
				MagazineBehaviour clip = interaction.UsedObject.GetComponent<MagazineBehaviour>();
				MagazineBehaviour usedclip = interaction.TargetObject.GetComponent<MagazineBehaviour>();
				string message = usedclip.LoadFromClip(clip);
				Chat.AddExamineMsg(interaction.Performer, message);
			}
		}

		public virtual string Examine(Vector3 pos)
		{
			return $"Accepts {ammoType}\nIt has {ServerAmmoRemains} out of {magazineSize} rounds within";
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