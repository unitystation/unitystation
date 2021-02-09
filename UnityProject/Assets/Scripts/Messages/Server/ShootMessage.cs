using System;
using System.Collections;
using UnityEngine;
using Mirror;
using Weapons.Projectiles;

namespace Weapons
{
	/// <summary>
	/// Informs all clients that a shot has been performed so they can display it (but they needn't
	/// perform any damage calculation, this is just displaying the shot that the server has already validated).
	/// </summary>
	public class ShootMessage : ServerMessage
	{
		/// <summary>
		/// GameObject of the player performing the shot
		/// </summary>
		public uint Shooter;
		/// <summary>
		/// Weapon being used to perform the shot
		/// </summary>
		public uint Weapon;
		/// <summary>
		/// Direction of shot, originating from Shooter)
		/// </summary>
		public Vector2 Direction;
		/// <summary>
		/// targeted body part
		/// </summary>
		public BodyPartType DamageZone;
		/// <summary>
		/// If the shot is aimed at the shooter
		/// </summary>
		public bool IsSuicideShot;
		/// <summary>
		/// Name of the projectile
		/// </summary>
		public string ProjectileName;
		/// <summary>
		/// Amount of projectiles
		/// </summary>
		public int Quantity;

		///To be run on client
		public override void Process()
		{
			if (!MatrixManager.IsInitialized) return;

			if (Shooter.Equals(NetId.Invalid))
			{
				//Failfast
				Logger.LogWarning($"Shoot request invalid, processing stopped: {ToString()}", Category.Firearms);
				return;
			}

			//Not even spawned don't show bullets
			if (PlayerManager.LocalPlayer == null) return;

			LoadMultipleObjects(new uint[] { Shooter, Weapon});

			Gun wep = NetworkObjects[1].GetComponent<Gun>();
			if (wep == null)
			{
				return;
			}

			//only needs to run on the clients other than the shooter
			if (!wep.isServer && PlayerManager.LocalPlayer.gameObject != NetworkObjects[0])
			{
				wep.DisplayShot(NetworkObjects[0], Direction, DamageZone, IsSuicideShot, ProjectileName, Quantity);
			}
		}

		/// <summary>
		/// Tell all clients + server to perform a shot with the specified parameters.
		/// </summary>
		/// <param name="direction">Direction of shot from shooter</param>
		/// <param name="damageZone">body part being targeted</param>
		/// <param name="shooter">gameobject of player making the shot</param>
		/// <param name="isSuicide">if the shooter is shooting themselves</param>
		/// <returns></returns>
		public static ShootMessage SendToAll(Vector2 direction, BodyPartType damageZone, GameObject shooter, GameObject weapon, bool isSuicide, string projectileName, int quantity)
		{
			var msg = new ShootMessage
			{
				Weapon = weapon ? weapon.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
				Direction = direction,
				DamageZone = damageZone,
				Shooter = shooter ? shooter.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
				IsSuicideShot = isSuicide,
				ProjectileName = projectileName,
				Quantity = quantity
			};
			msg.SendToAll();
			return msg;
		}

		public override string ToString()
		{
			return " ";
		}
	}

	/// <summary>
	/// Informs all clients that a shot has been performed so they can display it (but they needn't
	/// perform any damage calculation, this is just displaying the shot that the server has already validated).
	/// Different from ShootMessage in that it allows for projectile shots without a gun (e.g. fireball).
	/// </summary>
	public class CastProjectileMessage : ServerMessage
	{
		/// <summary>
		/// GameObject performing the shot
		/// </summary>
		public uint Shooter;
		/// <summary>
		/// Projectile being shot
		/// </summary>
		public Guid ProjectilePrefab;
		/// <summary>
		/// Direction of shot, originating from Shooter)
		/// </summary>
		public Vector2 Direction;
		/// <summary>
		/// targeted body part
		/// </summary>
		public BodyPartType DamageZone;

		///To be run on client
		public override void Process()
		{
			if (CustomNetworkManager.IsServer) return; // Processed serverside in SendToAll

			if (MatrixManager.IsInitialized == false) return;

			if (Shooter.Equals(NetId.Invalid))
			{
				//Failfast
				Logger.LogWarning($"Shoot request invalid, processing stopped: {ToString()}", Category.Firearms);
				return;
			}

			//Not even spawned don't show bullets
			if (PlayerManager.LocalPlayer == null) return;

			LoadNetworkObject(Shooter);
			GameObject shooter = NetworkObject;

			if (ClientScene.prefabs.TryGetValue(ProjectilePrefab, out var prefab) == false)
			{
				Logger.LogError($"Couldn't cast {ProjectilePrefab}; it is probably missing {nameof(NetworkIdentity)} component.", Category.Firearms);
				return;
			}

			ShootProjectile(prefab, shooter, Direction, DamageZone);
		}

		private static void ShootProjectile(GameObject prefab, GameObject shooter, Vector2 direction, BodyPartType damageZone)
		{
			GameObject projectile = UnityEngine.Object.Instantiate(prefab, shooter.transform.position, Quaternion.identity);

			if (projectile == null) return;
			Bullet bullet = projectile.GetComponent<Bullet>();
			if (bullet == null) return;

			bullet.Shoot(direction, shooter, null, damageZone);
		}

		/// <summary>
		/// Tell all clients + server to perform a shot with the specified parameters.
		/// </summary>
		/// <param name="direction">Direction of shot from shooter</param>
		/// <param name="damageZone">body part being targeted</param>
		/// <param name="shooter">gameobject of player making the shot</param>
		/// <param name="isSuicide">if the shooter is shooting themselves</param>
		/// <returns></returns>
		public static CastProjectileMessage SendToAll(GameObject shooter, GameObject projectilePrefab, Vector2 direction, BodyPartType damageZone)
		{
			if (CustomNetworkManager.IsServer)
			{
				ShootProjectile(projectilePrefab, shooter, direction, damageZone);
			}

			Guid assetID;
			if (projectilePrefab.TryGetComponent<NetworkIdentity>(out var networkIdentity))
			{
				assetID = networkIdentity.assetId;
			}
			else
			{
				Logger.LogError($"{projectilePrefab} doesn't have a network identity!", Category.NetMessage);
			}

			var msg = new CastProjectileMessage
			{
				Shooter = shooter ? shooter.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
				ProjectilePrefab = assetID,
				Direction = direction,
				DamageZone = damageZone,
			};
			msg.SendToAll();
			return msg;
		}
	}
}
