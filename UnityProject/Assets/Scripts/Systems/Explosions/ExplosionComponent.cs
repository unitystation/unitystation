using UnityEngine;
using Items.Weapons;

namespace Systems.Explosions
{
	public class ExplosionComponent : MonoBehaviour
	{
		[TooltipAttribute("Explosion strength")]
		public float strength = 50;

		[Range(0, 150)]
		[TooltipAttribute("Explosion radius. If it's equal to 0, it will be calculated using strength")]
		public int radius = 0;

		[Range(0, 255)]
		[TooltipAttribute("Shaking strength. If it's equal to 0, it will be calculated using strength")]
		public int shaking = 0;

		[TooltipAttribute("Explosion type")]
		public ExplosionTypes.ExplosionType explosionType = ExplosionTypes.ExplosionType.Regular;

		public void SetExplosionData(float _strength, ExplosionTypes.ExplosionType _explosionType = ExplosionTypes.ExplosionType.Regular, int _radius = 0, int _shaking = 0)
		{
			if (0 > _shaking || _shaking > 255)
			{
				_shaking = 0;
			}
			if (0 > _radius || _radius > 150)
			{
				_radius = 0;
			}

			strength = _strength;
			radius = _radius;
			shaking = _shaking;
			explosionType = _explosionType;
		}

		public void Explode()
		{
			BlastData data = new BlastData();
			data.BlastYield = strength;

			var pos = GetComponentInChildren<Transform>().position.RoundToInt();

			ExplosiveBase.ExplosionEvent.Invoke(pos, data);
			Explosion.StartExplosion(pos, strength, ExplosionTypes.NodeTypes[explosionType], radius, shaking);
		}
	}
}
