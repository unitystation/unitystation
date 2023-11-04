using System;
using Logs;
using UnityEngine;

namespace Objects.Kitchen
{
	/// <summary>
	/// Allows food processor to be interacted with. Player can put food in the processor to process it.
	/// The processor can be toggled on and off, eject contents, or have something Processable put inside it.
	/// </summary>
	[RequireComponent(typeof(DryingRack))]
	public class InteractableDryingRack : MonoBehaviour, IExaminable, ICheckedInteractable<HandApply>,
			IRightClickable, ICheckedInteractable<ContextMenuApply>
	{
		private DryingRack dryingRack;

		private void Awake()
		{
			dryingRack = GetComponent<DryingRack>();
		}

		public string Examine(Vector3 worldPos = default)
		{
			return $"There is {(dryingRack.IsFilled ? "something" : "nothing")} inside it.";
		}

		#region Interaction-HandApply

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) == false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			dryingRack.RequestAddItem(interaction.HandSlot);
		}

		#endregion Interaction-HandApply

		#region Interaction-ContextMenu

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();
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
				case "Eject":
					dryingRack.RequestEjectContents();
					break;
				default:
					Loggy.LogError("Unexpected interaction request occurred in food processor context menu.", Category.Interaction);
					break;
			}
		}

		#endregion Interaction-ContextMenu
	}
}
