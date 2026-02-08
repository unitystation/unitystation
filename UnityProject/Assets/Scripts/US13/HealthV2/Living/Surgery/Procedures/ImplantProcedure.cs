using System.Linq;
using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.HealthV2.Living.CirculatorySystem;
using US13.Items;
using US13.Items.Traits;
using US13.Systems.Inventory;
using Util;

namespace US13.HealthV2.Living.Surgery.Procedures
{
	[CreateAssetMenu(fileName = "ImplantProcedure", menuName = "ScriptableObjects/Surgery/ImplantProcedure")]
	public class ImplantProcedure : SurgeryProcedureBase
	{

		public ItemTrait RequiredImplantTrait;


		public override void FinnishSurgeryProcedure(BodyPart OnBodyPart, HandApply interaction,
			PresentProcedure presentProcedure)
		{
			base.FinnishSurgeryProcedure(OnBodyPart, interaction, presentProcedure);

			var itemApp = interaction?.HandSlot?.Item.OrNull()?.GetComponent<ItemAttributesV2>();

			ItemSlot ToTakeFrom = null;
			if (interaction?.HandSlot?.Item != null )
			{
				if (itemApp.OrNull()?.HasTrait(RequiredImplantTrait) == true)
				{
					ToTakeFrom = interaction.HandSlot;
				}

				if (itemApp.OrNull()?.HasTrait(CommonTraits.Instance.ItemBag) == true)
				{
					var Slot = interaction?.HandSlot?.Item.GetComponent<ItemStorage>().GetItemSlots().First(); //It has baggy it should have item storage
					if (Slot.Item != null)
					{
						itemApp = Slot.Item.OrNull()?.GetComponent<ItemAttributesV2>();
						if (itemApp.OrNull()?.HasTrait(RequiredImplantTrait) == true)
						{
							ToTakeFrom = Slot;
						}
					}
				}
			}

			if (ToTakeFrom != null)
			{
				if (OnBodyPart != null)
				{
					OnBodyPart.OrganStorage.ServerTryTransferFrom(ToTakeFrom);
				}
				else
				{
					var health = presentProcedure.isOn.GetComponent<LivingHealthMasterBase>();

					if (itemApp.HasTrait(CommonTraits.Instance.CoreBodyPart))
					{
						if (health.HasCoreBodyPart()) return;
						health.BodyPartStorage.ServerTryTransferFrom(ToTakeFrom);
						presentProcedure.isOn.currentlyOn = null;
					}
					else
					{
						health.BodyPartStorage.ServerTryTransferFrom(ToTakeFrom);
						presentProcedure.isOn.currentlyOn = null;
					}
				}
			}
		}
	}
}