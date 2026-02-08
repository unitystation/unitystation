using UnityEngine;
using US13.Core.Chat;
using US13.Health.Objects;
using US13.HealthV2;
using US13.Items.Implants.Organs;
using US13.Player;

namespace US13.Items.Food
{
	public class LeanDrink : DrinkableContainer
	{
		[SerializeField] private float lungDamage = 15f;

		public override void Drink(PlayerScript eater, PlayerScript feeder)
		{
			base.Drink(eater, feeder);
			eater.playerHealth.IndicatePain(lungDamage);
			foreach (var part in eater.playerHealth.BodyPartList)
			{
				foreach (var organ in part.OrganList)
				{
					if(organ is Lungs == false) continue;
					organ.RelatedPart.TakeDamage(null, lungDamage, AttackType.Internal, DamageType.Tox);
				}
			}

			Chat.AddExamineMsg(eater.gameObject, "You feel like your lungs are going to fail at some point because of this..");
		}
	}
}