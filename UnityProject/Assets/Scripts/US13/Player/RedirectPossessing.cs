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
			IPlayerPossessable = this.GetComponent<IPlayerPossessable>();
			IPlayerPossessable.OnPossessedBy.AddListener(Possessing);

			ItemStorage.ServerInventoryItemSlotSet += BrainInTransfer;

			if (ToPossessTo != null)
			{
				var Slots = ItemStorage.GetItemSlots();

				foreach (var Slot in Slots)
				{
					if (Slot.Item != null)
					{
						ToPossessTo = Slot.Item.GetComponent<IPlayerPossessable>();
						return;
					}
				}
			}
		}

		public void BrainInTransfer(Pickupable prevPart, Pickupable NewPart)
		{
			if (NewPart)
			{

				if (NewPart.TryGetCachedComponent<SaturationComponent>(out var component))
				{
					component.enabled = false;
				}

				if (NewPart.TryGetCachedComponent<HungerComponent>(out var component1))
				{
					component.enabled = false;
				}
				if (NewPart.TryGetCachedComponent<NaturalChemicalReleaseComponent>(out var component2))
				{
					component.enabled = false;
				}



			}
			else if (prevPart)
			{

				if (NewPart.TryGetCachedComponent<SaturationComponent>(out var component))
				{
					component.enabled = true;
				}

				if (NewPart.TryGetCachedComponent<HungerComponent>(out var component1))
				{
					component.enabled = true;
				}
				if (NewPart.TryGetCachedComponent<NaturalChemicalReleaseComponent>(out var component2))
				{
					component.enabled = true;
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
						ToPossessTo = Slot.Item.GetComponent<IPlayerPossessable>();
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