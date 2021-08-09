using System.Collections;
using System.Collections.Generic;
using Items;
using UnityEngine;

namespace HealthV2
{
	[CreateAssetMenu(fileName = "ImplantProcedure", menuName = "ScriptableObjects/Surgery/ImplantProcedure")]
	public class ImplantProcedure : SurgeryProcedureBase
	{

		public ItemTrait RequiredImplantTrait;


		public override void FinnishSurgeryProcedure(BodyPart OnBodyPart, PositionalHandApply interaction,
			Dissectible.PresentProcedure PresentProcedure)
		{
			base.FinnishSurgeryProcedure(OnBodyPart, interaction, PresentProcedure);

			if (interaction.HandSlot.Item != null && interaction.HandSlot.Item.GetComponent<ItemAttributesV2>().HasTrait(RequiredImplantTrait))
			{
				if (OnBodyPart != null)
				{
					OnBodyPart.OrganStorage.ServerTryTransferFrom(interaction.HandSlot);
				}
				else
				{
					PresentProcedure.ISon.GetComponent<LivingHealthMasterBase>().BodyPartStorage.ServerTryTransferFrom(interaction.HandSlot);
					PresentProcedure.ISon.currentlyOn = null;
				}
			}
		}
	}
}