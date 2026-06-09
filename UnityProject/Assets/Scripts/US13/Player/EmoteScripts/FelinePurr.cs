using System;
using UnityEngine;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.ScriptableObjects.RP;
using Random = System.Random;

namespace US13.Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "FelinePurr", menuName = "ScriptableObjects/RP/Emotes/FelinePurr")]
	public class FelinePurr : SpeciesSpecificEmote
	{
		[SerializeField] private int healAmount = 1;
		[SerializeField] private float totalDamageBeforeIneffective = 15f;
		private DamageType typeToHeal = DamageType.Brute;

		public override void Do(GameObject actor)
		{
			if (actor.TryGetComponent<LivingHealthMasterBase>(out var health) == false) return;
			if (health.GetTotalBruteDamage() < totalDamageBeforeIneffective)
			{
				Array partTypes = Enum.GetValues(typeof(BodyPartType));
				Random random = new Random();
				var randomPart = (BodyPartType)partTypes.GetValue(random.Next(partTypes.Length));
				health.HealDamage(null, healAmount, typeToHeal, randomPart);
			}
			base.Do(actor);
		}
	}
}