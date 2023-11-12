using System;
using Logs;
using UnityEngine;

namespace Objects.Kitchen
{
	/// <summary>
	/// Allows food processor to be interacted with. Player can put food in the processor to process it.
	/// The processor can be toggled on and off, eject contents, or have something Processable put inside it.
	/// </summary>
	[RequireComponent(typeof(FoodProcessor))]
	public class InteractableProcessor : MonoBehaviour, IExaminable, ICheckedInteractable<HandApply>,
			IRightClickable, ICheckedInteractable<ContextMenuApply>
	{
		const int TIMER_INCREMENT = 5; // Seconds


		private FoodProcessor foodProcessor;

		private void Start()
		{
			foodProcessor = GetComponent<FoodProcessor>();
		}

		public string Examine(Vector3 worldPos = default)
		{
			return $"It is currently {foodProcessor.currentState.StateMsgForExamine}."
				+ $" There is {(foodProcessor.IsFilled ? "something" : "nothing")} inside it.";
		}

		#region Interaction-HandApply

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) == false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			//If the interaction was an alt-click, empty the machine.
			if (interaction.IsAltClick)
			{
				foodProcessor.RequestEjectContents();
				return;
			}
			// If nothing's in hand, start machine.
			if (interaction.HandObject == null)
			{
				foodProcessor.RequestToggleActive();
			}
			else {
				// If there is something in hand and the machine is off, only accept the object if it has Processable.
				// Does nothing if the machine is currently active or unpowered.
				foodProcessor.RequestAddItem(interaction.HandSlot);
			}
		}

		#endregion Interaction-HandApply

		#region Interaction-ContextMenu

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();

			var activateInteraction = ContextMenuApply.ByLocalPlayer(gameObject, "ToggleActive");
			if (!WillInteract(activateInteraction, NetworkSide.Client)) return result;
			result.AddElement("Activate", () => ContextMenuOptionClicked(activateInteraction));

			var ejectInteraction = ContextMenuApply.ByLocalPlayer(gameObject, "Eject");
			result.AddElement("Eject", () => ContextMenuOptionClicked(ejectInteraction));


			return result;
		}

		private void ContextMenuOptionClicked(ContextMenuApply interaction)
		{
			InteractionUtils.RequestInteract(interaction, this);
		}

		public bool WillInteract(ContextMenuApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(ContextMenuApply interaction)
		{
			switch (interaction.RequestedOption)
			{
				case "ToggleActive":
					foodProcessor.RequestToggleActive();
					break;
				case "Eject":
					foodProcessor.RequestEjectContents();
					break;
				default:
					Loggy.LogError("Unexpected interaction request occurred in food processor context menu.", Category.Interaction);
					break;
			}
		}

		#endregion Interaction-ContextMenu
	}
}
