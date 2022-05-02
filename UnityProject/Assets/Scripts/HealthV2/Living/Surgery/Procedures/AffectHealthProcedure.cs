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
		[FormerlySerializedAs("RequiredImplantTrait")] public ItemTrait RequiredTrait;
		public DamageType Affects;
		public float HeelStrength;

		public AttackType FailAttackType = AttackType.Melee;

		public bool UseUpItem = false;

		public override void FinnishSurgeryProcedure(BodyPart OnBodyPart, HandApply interaction,
			Dissectible.PresentProcedure PresentProcedure)
		{
			if (interaction.HandSlot.Item != null && interaction.HandSlot.Item.GetComponent<ItemAttributesV2>().HasTrait(RequiredTrait))
			{
				OnBodyPart.HealDamage(interaction.UsedObject,HeelStrength,Affects);
				if (UseUpItem)
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
