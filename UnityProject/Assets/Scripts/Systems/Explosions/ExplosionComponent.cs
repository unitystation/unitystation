using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
		public ExplosionType explosionType = ExplosionType.Regular;

		public void SetExplosionData(float _strength, ExplosionType _explosionType = ExplosionType.Regular, int _radius = 0, int _shaking = 0)
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
			Explosion.StartExplosion(GetComponentInChildren<Transform>().position.RoundToInt(), strength, nodeTypes[explosionType], radius, shaking);
        }

		public enum ExplosionType //add your explosion type here
        {
			Regular,
			EMP
        }

		private Dictionary<ExplosionType, ExplosionNode> nodeTypes = new Dictionary<ExplosionType, ExplosionNode>() //add your node type here
		{
			{ExplosionType.Regular, new ExplosionNode()},
			{ExplosionType.EMP, new ExplosionEmpNode()}
		};
	}
}
