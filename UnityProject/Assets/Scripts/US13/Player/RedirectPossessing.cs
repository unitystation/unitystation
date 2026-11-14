using US13.HealthV2.Living;
using UnityEngine;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;
using US13.Systems.Inventory;
using Util;

namespace US13.Player
{
	public class RedirectPossessing : MonoBehaviour
	{
		public IPlayerPossessable IPlayerPossessable;

		public IPlayerPossessable ToPossessTo;

		public ItemStorage ItemStorage;

		public void Awake()
		{
			IPlayerPossessable = this.GetComponentOrBodyPartBehavior<IPlayerPossessable>();
			IPlayerPossessable.OnPossessedBy.AddListener(Possessing);

			ItemStorage.ServerInventoryItemSlotSet += BrainInTransfer;

			if (ToPossessTo != null)
			{
				var Slots = ItemStorage.GetItemSlots();

				foreach (var Slot in Slots)
				{
					if (Slot.Item != null)
					{
						ToPossessTo = Slot.Item.GetComponentOrBodyPartBehavior<IPlayerPossessable>();
						return;
					}
				}
			}
		}

		public void BrainInTransfer(Pickupable prevPart, Pickupable NewPart)
		{
			if (NewPart)
			{

				if (NewPart.TryGetBodyPartBehavior<SaturationComponent>(out var component))
				{
					component.Enabled = false;
				}

				if (NewPart.TryGetBodyPartBehavior<HungerComponent>(out var component1))
				{
					component1.Enabled = false;
				}
				if (NewPart.TryGetBodyPartBehavior<NaturalChemicalReleaseComponent>(out var component2))
				{
					component2.Enabled = false;
				}



			}
			else if (prevPart)
			{

				if (prevPart.TryGetBodyPartBehavior<SaturationComponent>(out var component))
				{
					component.Enabled = true;
				}

				if (prevPart.TryGetBodyPartBehavior<HungerComponent>(out var component1))
				{
					component1.Enabled = true;
				}
				if (prevPart.TryGetBodyPartBehavior<NaturalChemicalReleaseComponent>(out var component2))
				{
					component2.Enabled = true;
				}

			}
		}


		public void Possessing(Mind mind, IPlayerPossessable parent)
		{
			if (mind == null) return;
			if (parent != null) return;

			if (ToPossessTo == null)
			{
				var Slots = ItemStorage.GetItemSlots();

				foreach (var Slot in Slots)
				{
					if (Slot.Item != null)
					{
						ToPossessTo = Slot.Item.GetComponentOrBodyPartBehavior<IPlayerPossessable>();
						break;
					}
				}
			}


			mind.SetPossessingObject(ToPossessTo.GameObject);
			mind.StopGhosting();
			return;
		}
	}
}