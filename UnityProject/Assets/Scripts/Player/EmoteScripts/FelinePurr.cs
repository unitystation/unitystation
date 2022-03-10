using System;
using HealthV2;
using NUnit.Framework.Internal;
using ScriptableObjects.RP;
using UnityEngine;
using Random = System.Random;

namespace Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "FelinePurr", menuName = "ScriptableObjects/RP/Emotes/FelinePurr")]
	public class FelinePurr : SpeciesSpecificEmote
	{
		[SerializeField] private int healAmount = 1;
		[SerializeField] private float totalDamageBeforeIneffective = 15f;
		private DamageType typeToHeal = DamageType.Brute;

		public override void Do(GameObject player)
		{
			if (player.TryGetComponent<LivingHealthMasterBase>(out var health) == false) return;
			if (health.GetTotalBruteDamage() < totalDamageBeforeIneffective)
			{
				Array partTypes = Enum.GetValues(typeof(BodyPartType));
				Random random = new Random();
				var randomPart = (BodyPartType)partTypes.GetValue(random.Next(partTypes.Length));
				health.HealDamage(null, healAmount, typeToHeal, randomPart);
			}
			base.Do(player);
		}
	}
}