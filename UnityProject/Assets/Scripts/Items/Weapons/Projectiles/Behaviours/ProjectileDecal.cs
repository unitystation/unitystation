using Detective;
using Items;
using Logs;
using Mirror;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public class ProjectileDecal : NetworkBehaviour, IOnHit
	{
		[SerializeField] private GameObject decal = null;

		[Tooltip("Living time of decal.")]
		[SerializeField] private float animationTime = 0;

		public bool OnHit(MatrixManager.CustomPhysicsHit hit)
		{
			if (decal == null)
			{
				Loggy.LogError($"{this} on {gameObject} decal field not set in inspector!", Category.Firearms);
				return false;
			}

			RpcClientSpawn(hit.HitWorld);
			AppliedDetails AppliedDetails = null;
			if (hit.CollisionHit.GameObject == null)
			{
				var Matrix =  MatrixManager.AtPoint(hit.TileHitWorld, isServer);
				if (Matrix != null)
				{
					var Node = Matrix.MetaDataLayer.Get(hit.TileHitWorld.ToLocal(Matrix.Matrix).RoundToInt());
					AppliedDetails = Node.AppliedDetails;
				}
			}
			else
			{
				var Node = hit.CollisionHit.GameObject.GetComponent<Attributes>().OrNull();
				if (Node != null)
				{
					AppliedDetails = Node.AppliedDetails;
				}
			}

			AppliedDetails?.AddDetail(new Detail()
			{
				CausedByInstanceID = 0,
				Description = $"A bullet hole that looks like it was made by a {this.gameObject.name}",
				DetailType = DetailType.BulletHole
			});


			return false;
		}

		[ClientRpc]
		public void RpcClientSpawn(Vector3 WorldPosition)
		{
			var newDecal = Spawn.ClientPrefab(decal,
				WorldPosition).GameObject;
			var timeLimitedDecal = newDecal.GetComponent<TimeLimitedDecal>();
			timeLimitedDecal.SetUpDecal(animationTime);
		}
	}
}