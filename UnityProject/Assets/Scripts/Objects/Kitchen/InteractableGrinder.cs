using System.Collections.Generic;
using UnityEngine;
using Systems.Chemistry.Components;
using Items;
using Systems.Chemistry;

namespace Objects.Kitchen
{
	/// <summary>
	/// Allows Microwave to be interacted with. Player can put food in the microwave to cook it.
	/// The microwave can be interacted with to, for example, check the remaining time.
	/// </summary>
	[RequireComponent(typeof(AIOGrinder))]
	public class InteractableGrinder : MonoBehaviour, IExaminable, ICheckedInteractable<HandApply>,
			IRightClickable, ICheckedInteractable<ContextMenuApply>
	{
		private AIOGrinder grinder;
		private ReagentContainer grinderStorage;

		private void Start()
		{
			grinderStorage = GetComponent<ReagentContainer>();
			grinder = GetComponent<AIOGrinder>();
		}
		public string Examine(Vector3 worldPos = default)
		{
			return $"It is currently in {(grinder.GrindOrJuice ? "grind" : "juice")} mode.";
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver) == false;
		}

		/// <summary>
		/// Players can change the grinding mode, grind or juice an item, or add an item to the grinder.
		/// </summary>
		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.IsAltClick)
			{
				// Check if the player is holding food that can be ground up
				ItemAttributesV2 attr = interaction.HandObject.GetComponent<ItemAttributesV2>();
				Ingredient ingredient = new Ingredient(attr.ArticleName);
				Reagent meal = CraftingManager.Grind.FindReagentRecipe(new List<Ingredient> { ingredient });
				int count = CraftingManager.Grind.FindReagentAmount(new List<Ingredient> { ingredient });
				if (meal)
				{
					grinder.SetServerStackAmount(count);
					grinder.ServerSetOutputMeal(meal.name);
					_ = Despawn.ServerSingle(interaction.HandObject);
					Chat.AddExamineMsgFromServer(interaction.Performer, $"You grind the {attr.ArticleName}.");
					GetComponent<AIOGrinder>().GrindFood();
				}
				else
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, $"Your {attr.ArticleName} can not be ground up.");
				}
			}
			// If nothing's in hand, start machine.
			if (interaction.HandObject == null)
			{
				grinder.Activate();
			}
			else {
				// If there is something in hand and the machine is off, only accept the object if it has Grindable.
				grinder.AddItem(interaction.HandSlot);
			}
		}


		#region Interaction-ContextMenu

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();

			var activateInteraction = ContextMenuApply.ByLocalPlayer(gameObject, "Activate");
			if (!WillInteract(activateInteraction, NetworkSide.Client)) return result;
			result.AddElement("Activate", () => ContextMenuOptionClicked(activateInteraction));

			var switchModeInteraction = ContextMenuApply.ByLocalPlayer(gameObject, "Switch Mode");
			result.AddElement("Switch Mode", () => ContextMenuOptionClicked(switchModeInteraction));

			var ejectInteraction = ContextMenuApply.ByLocalPlayer(gameObject, "Eject Beaker");
			result.AddElement("Eject Beaker", () => ContextMenuOptionClicked(ejectInteraction));


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
				case "Activate":
					grinder.Activate();
					break;
				case "Eject Beaker":
					grinder.EjectContainer();
					break;
				case "Switch Mode":
					grinder.SwitchMode();
					break;
				default:
					Logger.LogError("Unexpected interaction request occurred in grinder context menu.", Category.Interaction);
					break;
			}
		}

		#endregion Interaction-ContextMenu
	}
}
