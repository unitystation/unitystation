using HealthV2;
using Items.Implants.Organs;
using UnityEngine;

namespace Items.Food
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