using UnityEngine;

namespace Doors
{
	public class InteractableDoorV2: MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private DoorControllerV2 doorControllerV2;
		private void Awake()
		{
			doorControllerV2 = GetComponent<DoorControllerV2>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.TargetObject != gameObject) return false;
			if (Validations.HasUsedActiveWelder(interaction)) return true;
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar)) return true;
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver)) return true;
			if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false; // False to allow melee

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar) ||
			    Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.CanPryDoor))
			{
				doorControllerV2.TryCrowbar(interaction.Performer);
				return;
			}

			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				doorControllerV2.TryPanel(interaction.Performer);
				return;
			}

			if (Validations.HasUsedActiveWelder(interaction))
			{
				doorControllerV2.TryWelder(interaction.Intent, interaction.Performer);
				return;
			}

			if (interaction.HandObject != null)
			{
				return;
			}

			if (!doorControllerV2.IsClosed)
			{
				doorControllerV2.TryClose(interaction.Performer);
			}
			else
			{
				doorControllerV2.TryOpen(interaction.Performer);
			}
		}
	}
}