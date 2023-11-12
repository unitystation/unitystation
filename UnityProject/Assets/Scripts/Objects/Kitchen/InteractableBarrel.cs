using System.Collections.Generic;
using UnityEngine;
using Chemistry.Components;
using Items;
using Logs;

namespace Objects.Kitchen
{
	/// <summary>
	/// Allows Microwave to be interacted with. Player can put food in the microwave to cook it.
	/// The microwave can be interacted with to, for example, check the remaining time.
	/// </summary>
	[RequireComponent(typeof(FermentingBarrel))]
	public class InteractableBarrel : MonoBehaviour, IExaminable, ICheckedInteractable<HandApply>,
			IRightClickable, ICheckedInteractable<ContextMenuApply>
	{
		private FermentingBarrel barrel;

		private void Awake()
		{
			barrel = GetComponent<FermentingBarrel>();
		}

		public string Examine(Vector3 worldPos = default)
		{
			return $"It is currently {(barrel.Closed ? "closed, meaning the items inside are fermenting." : "open, meaning you can put things inside.")}"
				+ $" There is {(barrel.HasContents ? "something" : "nothing")} inside it.";
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench) == false;
		}

		/// <summary>
		/// Players can open or close the barrel, or add an item to ferment it.
		/// </summary>
		public void ServerPerformInteraction(HandApply interaction)
		{
			// If nothing's in hand, open or close the barrel.
			if (interaction.HandObject == null)
			{
				barrel.OpenClose();
			}
			else {
				// If there is something in hand and the barrel is open, only accept the object if it has Fermentable.
				barrel.AddItem(interaction.HandSlot);
			}
		}

		#region Interaction-ContextMenu

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();

			var activateInteraction = ContextMenuApply.ByLocalPlayer(gameObject, "Open/Close");
			if (!WillInteract(activateInteraction, NetworkSide.Client)) return result;
			result.AddElement("Open/Close", () => ContextMenuOptionClicked(activateInteraction));

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
				case "Open/Close":
					barrel.OpenClose();
					break;
				default:
					Loggy.LogError("Unexpected interaction request occurred in barrel context menu.", Category.Interaction);
					break;
			}
		}

		#endregion Interaction-ContextMenu
	}
}
