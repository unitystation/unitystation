using System.Collections;
using System.Collections.Generic;
using Items;
using UnityEngine;
using UnityEngine.Serialization;

namespace HealthV2
{
	[CreateAssetMenu(fileName = "AffectHealthProcedure", menuName = "ScriptableObjects/Surgery/AffectHealthProcedure")]
	public class AffectHealthProcedure : SurgeryProcedureBase
	{
		public DamageType Affects;
		public float HeelStrength;

		public bool ConsumeItem;

		public AttackType FailAttackType = AttackType.Melee;

		public override void FinnishSurgeryProcedure(BodyPart OnBodyPart, HandApply interaction,
			Dissectible.PresentProcedure PresentProcedure)
		{
			if (PresentProcedure.RelatedBodyPart.ContainedIn != null)
			{
				PresentProcedure.isOn.currentlyOn = PresentProcedure.RelatedBodyPart.ContainedIn.gameObject;
				PresentProcedure.RelatedBodyPart = PresentProcedure.RelatedBodyPart.ContainedIn;
			}
			else
			{
				PresentProcedure.isOn.currentlyOn = null;
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
			Dissectible.PresentProcedure PresentProcedure)
		{
			OnBodyPart.TakeDamage(interaction.UsedObject,HeelStrength*0.1f,FailAttackType,Affects);
		}
	}
}
