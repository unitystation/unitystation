using System;
using System.Linq;
using TileManagement;
using AddressableReferences;
using Core;
using Core.Physics;
using Cysharp.Threading.Tasks;
using Logs;
using UnityEngine;

namespace Systems.Explosions
{
	public class DarkMatterExplosionNode : ExplosionNode
	{
		public override AddressableAudioSource CustomSound => CommonSounds.Instance.GravHit;

		public DarkMatterExplosionNode(Vector3 _explosionStartWorldPosition) : base(_explosionStartWorldPosition) { }

		public override async UniTask Process()
		{
			float force = AngleAndIntensity.magnitude; //We don't ignore less than 0 for dark matter bombs, as <0 is a push

			if (matrix.MetaTileMap == false) return;

			var locationNoZ = new Vector3Int(Location.x, Location.y, 0);
			await  ProcessTiles(force, locationNoZ);
		}

		protected async UniTask ProcessTiles(float force, Vector3Int nodeLocation)
		{
			var energyExpended = DoDamageToTiles(matrix, force, nodeLocation, matrix.MetaTileMap);
			foreach (var line in PresentLines)
			{
				line.ExplosionStrength -= Math.Abs(energyExpended * (line.ExplosionStrength / force));
			}
			AngleAndIntensity = Vector2.zero;
		}

		public override float DoDamageToTiles(Matrix matrix, float explosionForce, Vector3Int nodeLocation, MetaTileMap tileMap)
		{
			PullPushThings(nodeLocation, explosionForce);
			return 10.0f; //magic number
		}

		private void PullPushThings(Vector3Int worldPosition, float force)
		{
			float throwSpeed = Math.Min(2, force * 2);

			foreach (var objectPhysics in MatrixManager.GetAt<UniversalObjectPhysics>(worldPosition, true).Distinct())
			{
				if (objectPhysics == false) continue;

				objectPhysics.NewtonianPush(AngleAndIntensity.normalized, throwSpeed, inSlideTime: throwSpeed / 4);
			}
		}

		public override ExplosionNode GenInstance()
		{
			return new DarkMatterExplosionNode(ExplosionStartWorldPosition);
		}
	}
}
