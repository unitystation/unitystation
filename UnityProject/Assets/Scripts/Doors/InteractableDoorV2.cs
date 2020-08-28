using Doors.DoorFSM;
using UnityEngine;

namespace Doors
{
	public class InteractableDoorV2: MonoBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		private DoorControllerV2 doorControl;
		private StateMachine fsm;

		private void Awake()
		{
			doorControl = GetComponent<DoorControllerV2>();
			fsm = doorControl.DoorFsm;
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
				doorControl.TryCrowbar(interaction.Performer);
				return;
			}

			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				doorControl.TryPanel(interaction.Performer);
				return;
			}

			if (Validations.HasUsedActiveWelder(interaction))
			{
				doorControl.TryWelder(interaction.Intent, interaction.Performer);
				return;
			}

			if (interaction.HandObject != null)
			{
				return;
			}

			if (!doorControl.IsClosed)
			{
				doorControl.TryClose(interaction.Performer);
			}
			else
			{
				doorControl.TryOpen(interaction.Performer);
			}
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			string examineMessage = $"The {gameObject.ExpensiveName()} is ";

			if (!doorControl.IsClosed)
			{
				examineMessage += "open. ";
			}
			else
			{
				examineMessage += "closed. ";
			}

			if (fsm.Properties.IsWeld)
			{
				examineMessage += "Seems like someone welded it. ";
			}

			if (fsm.Properties.HasPanelExposed)
			{
				examineMessage += "The maintenance panel is open and it has all the wiring exposed. ";
			}

			if (fsm.Properties.HasPower && fsm.Properties.HasBoltsDown && fsm.Properties.HasBoltLights)
			{
				examineMessage += "The bolts lights are on. ";
			}

			if (!fsm.Properties.HasPower)
			{
				examineMessage += "Doesn't look like it is powered. ";
			}

			return examineMessage;
		}
	}
}