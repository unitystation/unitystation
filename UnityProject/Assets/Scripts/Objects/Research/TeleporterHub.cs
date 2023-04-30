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
			return playerScript.PlayerTypeSettings.CanEnterPortals;
		}

		public override void OnObjectEnter(GameObject eventData)
		{
			TryTeleport(eventData);
		}

		public override bool WillAffectObject(GameObject eventData)
		{
			//Don't allow intangible stuff, like sparks as that will cause loop crashes
			if (eventData.TryGetComponent<UniversalObjectPhysics>(out var uop) && uop.Intangible)
			{
				return false;
			}

			return true;
		}

		private void TryTeleport(GameObject eventData)
		{
			if(AllowTeleport() == false) return;

			SparkUtil.TrySpark(gameObject, expose: false);

			TransportUtility.TeleportToObject(eventData, linkedBeacon.gameObject,
				linkedBeacon.CurrentBeaconPosition(), calibrated, false);
		}

		private bool AllowTeleport()
		{
			if(powered == false) return false;
			if(active == false) return false;

			//Shouldn't occur
			if (linkedBeacon == null) return false;

			return true;
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

			Chat.AddActionMsgToChat(gameObject, $"The {data.BulletName} enters through the active portal!");

			SparkUtil.TrySpark(gameObject, expose: false);
			SparkUtil.TrySpark(linkedBeacon.gameObject, expose: false);

			return false;
		}
	}
}
