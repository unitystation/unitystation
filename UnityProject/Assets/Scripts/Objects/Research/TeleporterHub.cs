using Systems.Explosions;
using Gateway;
using Items;
using UnityEngine;
using Weapons.Projectiles;
using Weapons.Projectiles.Behaviours;

namespace Objects.Research
{
	/// <summary>
	/// Teleporter gateway
	/// </summary>
	public class TeleporterHub : TeleporterBase, IServerSpawn, IOnPreHitDetect
	{
		private bool calibrated;

		public override void OnPlayerStep(PlayerScript playerScript)
		{
			TryTeleport(playerScript.gameObject);
		}

		public override bool WillAffectPlayer(PlayerScript playerScript)
		{
			//Allow players or ghosts to enter
			return playerScript.PlayerState == PlayerScript.PlayerStates.Normal ||
			       playerScript.PlayerState == PlayerScript.PlayerStates.Ghost;
		}

		public override void OnObjectEnter(GameObject eventData)
		{
			TryTeleport(eventData);
		}

		public override bool WillAffectObject(GameObject eventData)
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

			var newWorldPosition = linkedBeacon.CurrentBeaconPosition();
			var isGhost = false;

			if (objectToTeleport.TryGetComponent<UniversalObjectPhysics>(out var uop))
			{
				//Transport objects and players
				TransportUtility.TransportObjectAndPulled(uop, newWorldPosition);
			}
			//Ghosts dont have uop so check for ghost move
			else if (objectToTeleport.TryGetComponent<GhostMove>(out var ghost))
			{
				isGhost = true;
				ghost.ForcePositionClient(newWorldPosition);
			}

			if (calibrated == false && hasQuantum)
			{
				reaction.OnTeleportEnd();
			}

			//Dont spark for ghosts :(
			if (isGhost) return;

			SparkUtil.TrySpark(linkedBeacon.gameObject, expose: false);
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

		public bool OnPreHitDetect(OnHitDetectData data)
		{
			if(AllowTeleport() == false) return true;

			var range = -1f;

			if (data.BulletObject.TryGetComponent<ProjectileRangeLimited>(out var rangeLimited))
			{
				range = rangeLimited.CurrentDistance;
			}

			ProjectileManager.InstantiateAndShoot(data.BulletObject.GetComponent<Bullet>().PrefabName,
				data.BulletShootDirection, linkedBeacon.gameObject, null, BodyPartType.None, range);

			Chat.AddLocalMsgToChat($"The {data.BulletName} enters through the active portal!", gameObject);

			SparkUtil.TrySpark(linkedBeacon.gameObject, expose: false);

			return false;
		}
	}
}
