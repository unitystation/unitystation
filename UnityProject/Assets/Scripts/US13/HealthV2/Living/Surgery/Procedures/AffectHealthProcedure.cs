using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Lifecycle;
using US13.Health.Objects;
using US13.HealthV2.Living.CirculatorySystem;
using US13.Items;

namespace US13.HealthV2.Living.Surgery.Procedures
{
	[CreateAssetMenu(fileName = "AffectHealthProcedure", menuName = "ScriptableObjects/Surgery/AffectHealthProcedure")]
	public class AffectHealthProcedure : SurgeryProcedureBase
	{
		public DamageType Affects;
		public float HeelStrength;

		public bool ConsumeItem;

		public AttackType FailAttackType = AttackType.Melee;

		public override void FinnishSurgeryProcedure(BodyPart OnBodyPart, HandApply interaction,
			PresentProcedure presentProcedure)
		{
			if (presentProcedure.RelatedBodyPart.ContainedIn != null && presentProcedure.RelatedBodyPart.ContainedIn.IsOpenAir == false)
			{
				presentProcedure.isOn.currentlyOn = presentProcedure.RelatedBodyPart.ContainedIn.gameObject;
				presentProcedure.RelatedBodyPart = presentProcedure.RelatedBodyPart.ContainedIn;
			}
			else
			{
				presentProcedure.isOn.currentlyOn = null;
			}

			if (interaction.HandSlot.Item != null)
			{
				OnBodyPart.HealDamage(interaction.UsedObject,HeelStrength,Affects);

				if (ConsumeItem)
				{
					var stackable = interaction.UsedObject.GetComponent<Stackable>();
					if (stackable != null)
					{
						stackable.ServerConsume(1);
					}
					else
					{
						_ = Despawn.ServerSingle(interaction.UsedObject);
					}
				}

			}
		}

		public override void UnsuccessfulStep(BodyPart OnBodyPart, HandApply interaction,
			PresentProcedure presentProcedure)
		{
			OnBodyPart.TakeDamage(interaction.UsedObject,HeelStrength*0.1f,FailAttackType,Affects);
		}
	}
}
