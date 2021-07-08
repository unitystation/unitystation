using Mirror;
using UnityEngine;
using Weapons;
using Weapons.Projectiles;
using Weapons.Projectiles.Behaviours;

namespace Messages.Server
{
	/// <summary>
	/// Informs all clients that a shot has been performed so they can display it (but they needn't
	/// perform any damage calculation, this is just displaying the shot that the server has already validated).
	/// </summary>
	public class ShootMessage : ServerMessage<ShootMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
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
		}

		///To be run on client
		public override void Process(NetMessage msg)
		{
			if (!MatrixManager.IsInitialized) return;

			if (msg.Shooter.Equals(NetId.Invalid))
			{
				//Failfast
				Logger.LogWarning($"Shoot request invalid, processing stopped: {ToString()}", Category.Firearms);
				return;
			}

			//Not even spawned don't show bullets
			if (PlayerManager.LocalPlayer == null) return;

			LoadMultipleObjects(new uint[] { msg.Shooter, msg.Weapon});

			if (NetworkObjects[0] == null || NetworkObjects[1] == null)
			{
				Debug.LogError($"Shoot message had null: {(NetworkObjects[0] == null ? "shooter" : "")} {(NetworkObjects[1] == null ? "weapon" : "")}");
				return;
			}

			Gun wep = NetworkObjects[1].GetComponent<Gun>();
			if (wep == null)
			{
				return;
			}

			//only needs to run on the clients other than the shooter
			if (!wep.isServer && PlayerManager.LocalPlayer != NetworkObjects[0])
			{
				wep.DisplayShot(NetworkObjects[0], msg.Direction, msg.DamageZone, msg.IsSuicideShot, msg.ProjectileName, msg.Quantity);
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
		public static NetMessage SendToAll(Vector2 direction, BodyPartType damageZone, GameObject shooter, GameObject weapon, bool isSuicide, string projectileName, int quantity)
		{
			var msg = new NetMessage
			{
				Weapon = weapon ? weapon.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
				Direction = direction,
				DamageZone = damageZone,
				Shooter = shooter ? shooter.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
				IsSuicideShot = isSuicide,
				ProjectileName = projectileName,
				Quantity = quantity
			};

			SendToAll(msg);
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
	public class CastProjectileMessage : ServerMessage<CastProjectileMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			/// <summary>
			/// GameObject performing the shot
			/// </summary>
			public uint Shooter;
			/// <summary>
			/// Projectile being shot
			/// </summary>
			public string ProjectilePrefabName;
			/// <summary>
			/// Direction of shot, originating from Shooter)
			/// </summary>
			public Vector2 Direction;
			/// <summary>
			/// targeted body part
			/// </summary>
			public BodyPartType DamageZone;
			/// <summary>
			/// Whether this bullet should have already moved a certain distance for the range check
			/// </summary>
			public float RangeChange;
		}

		///To be run on client
		public override void Process(NetMessage msg)
		{
			if (CustomNetworkManager.IsServer) return; // Processed serverside in SendToAll

			if (MatrixManager.IsInitialized == false) return;

			if (msg.Shooter.Equals(NetId.Invalid))
			{
				//Failfast
				Logger.LogWarning($"Shoot request invalid, processing stopped: {ToString()}", Category.Firearms);
				return;
			}

			//Not even spawned don't show bullets
			if (PlayerManager.LocalPlayer == null) return;

			LoadNetworkObject(msg.Shooter);
			GameObject shooter = NetworkObject;

			ShootProjectile(msg.ProjectilePrefabName, shooter, msg.Direction, msg.DamageZone, msg.RangeChange);
		}

		private static void ShootProjectile(string prefab, GameObject shooter, Vector2 direction, BodyPartType damageZone,
			float rangeChange)
		{
			GameObject projectile = Spawn.ClientPrefab(prefab, shooter.transform.position, shooter.transform.parent).GameObject;

			if (projectile == null) return;
			Bullet bullet = projectile.GetComponent<Bullet>();
			if (bullet == null) return;

			if (rangeChange >= 0)
			{
				if (bullet.TryGetComponent<ProjectileRangeLimited>(out var rangeLimited))
				{
					rangeLimited.SetDistance(rangeChange);
				}
			}

			bullet.Shoot(direction, shooter, null, damageZone);
		}

		/// <summary>
		/// Tell all clients + server to perform a shot with the specified parameters.
		/// </summary>
		/// <param name="direction">Direction of shot from shooter</param>
		/// <param name="damageZone">body part being targeted</param>
		/// <param name="shooter">gameobject of player making the shot</param>
		/// <param name="isSuicide">if the shooter is shooting themselves</param>
		/// <param name="rangeChange">If this bullet has used some of its range already</param>
		/// <returns></returns>
		public static NetMessage SendToAll(GameObject shooter, GameObject projectilePrefab, Vector2 direction, BodyPartType damageZone,
			float rangeChange = -1)
		{
			if (CustomNetworkManager.IsServer)
			{
				ShootProjectile(projectilePrefab.name, shooter, direction, damageZone, rangeChange);
			}

			var msg = new NetMessage
			{
				Shooter = shooter ? shooter.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
				ProjectilePrefabName = projectilePrefab.name,
				Direction = direction,
				DamageZone = damageZone,
				RangeChange = rangeChange
			};

			SendToAll(msg);
			return msg;
		}

		/// <summary>
		/// Tell all clients + server to perform a shot with the specified parameters.
		/// </summary>
		/// <param name="direction">Direction of shot from shooter</param>
		/// <param name="damageZone">body part being targeted</param>
		/// <param name="shooter">gameobject of player making the shot</param>
		/// <param name="isSuicide">if the shooter is shooting themselves</param>
		/// <param name="rangeChange">If this bullet has used some of its range already</param>
		/// <returns></returns>
		public static NetMessage SendToAll(GameObject shooter, string projectilePrefabName, Vector2 direction, BodyPartType damageZone,
			float rangeChange = -1)
		{
			if (CustomNetworkManager.IsServer)
			{
				ShootProjectile(projectilePrefabName, shooter, direction, damageZone, rangeChange);
			}

			var msg = new NetMessage
			{
				Shooter = shooter ? shooter.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
				ProjectilePrefabName = projectilePrefabName,
				Direction = direction,
				DamageZone = damageZone,
				RangeChange = rangeChange
			};

			SendToAll(msg);
			return msg;
		}
	}
}
