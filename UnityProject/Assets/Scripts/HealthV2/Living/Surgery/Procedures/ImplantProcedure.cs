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


		public override void FinnishSurgeryProcedure(BodyPart OnBodyPart, HandApply interaction,
			Dissectible.PresentProcedure PresentProcedure)
		{
			base.FinnishSurgeryProcedure(OnBodyPart, interaction, PresentProcedure);

			var itemApp = interaction?.HandSlot?.Item.OrNull()?.GetComponent<ItemAttributesV2>();

			if (interaction?.HandSlot?.Item != null && itemApp.OrNull()?.HasTrait(RequiredImplantTrait) == true)
			{
				if (OnBodyPart != null)
				{
					OnBodyPart.OrganStorage.ServerTryTransferFrom(interaction.HandSlot);
				}
				else
				{
					var health = PresentProcedure.isOn.GetComponent<LivingHealthMasterBase>();

					if (itemApp.HasTrait(CommonTraits.Instance.CoreBodyPart))
					{
						if (health.HasCoreBodyPart()) return;
						health.BodyPartStorage.ServerTryTransferFrom(interaction.HandSlot);
						PresentProcedure.isOn.currentlyOn = null;
					}
					else
					{
						health.BodyPartStorage.ServerTryTransferFrom(interaction.HandSlot);
						PresentProcedure.isOn.currentlyOn = null;
					}


				}
			}
		}
	}
}