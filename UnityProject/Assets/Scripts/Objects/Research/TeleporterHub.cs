using System.Collections.Generic;
using Systems.Electricity;
using Systems.Explosions;
using Gateway;
using Items;
using Messages.Server;
using UnityEngine;
using Weapons.Projectiles;
using Weapons.Projectiles.Behaviours;

namespace Objects.Research
{
	/// <summary>
	/// Teleporter gateway
	/// </summary>
	public class TeleporterHub : TeleporterBase, IPlayerEntersTile, IObjectEntersTile, IServerSpawn, IOnHitDetect
	{
		private bool calibrated;

		public void OnStep(GameObject eventData)
		{
			TryTeleport(eventData);
		}

		public void OnEnter(GameObject eventData)
		{
			TryTeleport(eventData);
		}

		public bool WillStep(GameObject eventData)
		{
			return true;
		}

		public bool CanEnter(GameObject eventData)
		{
			return true;
		}

		private void TryTeleport(GameObject eventData)
		{
			if(AllowTeleport() == false) return;

			InternalTeleport(eventData);
		}

		private bool AllowTeleport()
		{
			if(powered == false) return false;
			if(active == false) return false;

			//Shouldn't occur
			if (linkedBeacon == null) return false;

			return true;
		}

		private void InternalTeleport(GameObject objectToTeleport)
		{
			//TODO more uncalibrated accidents, e.g turn into fly people, mutate animals? (See IQuantumReaction)

			//Prevent teleporting loops from teleporting connected tracking device
			if (objectToTeleport == linkedBeacon.gameObject) return;

			var hasQuantum = objectToTeleport.TryGetComponent(out IQuantumReaction reaction);

			if (calibrated == false && hasQuantum)
			{
				reaction.OnTeleportStart();
			}

			//Transport object
			TransportUtility.TransportObjectAndPulled(objectToTeleport, linkedBeacon.CurrentBeaconPosition());

			if (calibrated == false && hasQuantum)
			{
				reaction.OnTeleportEnd();
			}

			//Dont spark for ghosts :(
			if (objectToTeleport.TryGetComponent(out PlayerScript playerScript) && playerScript.IsGhost) return;

			SparkUtil.TrySpark(objectToTeleport, expose: false);
		}

		public override void SetBeacon(TrackingBeacon newBeacon)
		{
			calibrated = false;

			base.SetBeacon(newBeacon);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			SetHub(this);
		}

		public void OnHitDetect(OnHitDetectData data)
		{
			if(AllowTeleport() == false) return;

			var range = -1f;

			if (data.BulletObject.TryGetComponent<ProjectileRangeLimited>(out var rangeLimited))
			{
				range = rangeLimited.CurrentDistance;
			}

			CastProjectileMessage.SendToAll(linkedBeacon.gameObject, data.BulletObject.GetComponent<Bullet>().PrefabName,
				data.BulletShootDirection, default, range);

			//TODO this still damages the object, increase resistances? or make it heal up the damage and only get destroyed if its 100% dead?
		}
	}
}
