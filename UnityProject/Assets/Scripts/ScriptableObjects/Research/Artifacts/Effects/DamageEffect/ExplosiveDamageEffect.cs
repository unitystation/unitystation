using Core;
using UnityEngine;
using Systems.Explosions;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "ExplosiveDamageEffect", menuName = "ScriptableObjects/Systems/Artifacts/ExplosiveDamageEffect")]
	public class ExplosiveDamageEffect : DamageEffectBase
	{
		[SerializeField, Tooltip("The higher the multiplier, the stronger the explosion")]
		private int Multiplier = 10;

		public override void DoEffect(DamageInfo damageInfo, UniversalObjectPhysics objectPhysics)
		{
			int explosiveStrength = Multiplier;
			int explosiveRadius = Multiplier;

			if(damageInfo != null)
			{
				explosiveStrength = (int)(damageInfo.Damage * Multiplier * 5);
				explosiveRadius = (int)(damageInfo.Damage * Multiplier / 100);
			}

			var worldPos = objectPhysics.registerTile.WorldPosition;

			Explosion.StartExplosion(worldPos, explosiveStrength, null, explosiveRadius, stunNearbyPlayers: true);
		}
	}
}
