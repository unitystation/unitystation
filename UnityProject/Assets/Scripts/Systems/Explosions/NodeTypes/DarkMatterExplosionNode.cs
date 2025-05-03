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
		public override OverlayType EffectOverlayType => OverlayType.DarkMatter;
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
			return 2f; //magic number
		}

		private void PullPushThings(Vector3Int worldPosition, float force)
		{
			float throwSpeed = Math.Max(0.5f, force * 0.25f);

			Vector2 direction = (ExplosionStartWorldPosition - worldPosition).normalized;
			foreach (var objectPhysics in MatrixManager.GetAt<UniversalObjectPhysics>(worldPosition, true).Distinct())
			{
				if (objectPhysics == false) continue;

				objectPhysics.NewtonianPush(direction, throwSpeed, inSlideTime: 0.35f);
			}
		}

		public override ExplosionNode GenInstance()
		{
			return new DarkMatterExplosionNode(ExplosionStartWorldPosition);
		}
	}
}
