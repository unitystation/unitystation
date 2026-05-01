using UnityEngine;
using US13.Player;
using US13.Systems.Occupations;
using Util;

namespace US13.Items.Food.ConsumptionEffect.Conditions
{
	public class JobIsEqualToCondition: IConsumptionEffectCondition
	{
		[SerializeField] private JobType requiredJob;

		public bool IsValid(ConsumptionContext context)
		{
			var player = context.Eater.GetCachedComponent<PlayerScript>();

			if (player == null || player.Mind == null || player.Mind.occupation == null)
			{
				return false;
			}

			return player.Mind.occupation.JobType == requiredJob;
		}
	}
}