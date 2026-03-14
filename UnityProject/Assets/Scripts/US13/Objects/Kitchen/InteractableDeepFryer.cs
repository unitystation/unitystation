using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Health.Objects;
using US13.Objects.Machines;
using US13.UI.Core.RightClick;
using Util;

namespace US13.Objects.Kitchen
{
	[RequireComponent(typeof(DeepFryer))]
	public class InteractableDeepFryer : MonoBehaviour, IExaminable, IFirstInteractable<PositionalHandApply>,
		IRightClickable, ICheckedInteractable<ContextMenuApply>
	{
		[SerializeField]
		[Tooltip("Click region for basket 0 (left basket).")]
		private SpriteClickRegion basket0Region;

		[SerializeField]
		[Tooltip("Click region for basket 1 (right basket).")]
		private SpriteClickRegion basket1Region;

		private DeepFryer deepFryer;

		private void Start()
		{
			deepFryer = this.GetComponentCustom<DeepFryer>();
		}

		public string Examine(Vector3 worldPos = default)
		{
			int basketIndex = GetBasketIndex(worldPos);
			if (basketIndex < 0) return "";

			var basket = deepFryer.GetBasket(basketIndex);
			string state = basket.IsDown ? "lowered" : "raised";
			string contents = basket.HasItem
				? $"<b>{basket.StorageSlot.ItemObject.ExpensiveName()}</b>"
				: "nothing";
			return $"Basket {basketIndex + 1} is {state} with {contents} inside.";
		}

		#region Interaction-PositionalHandApply

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			bool handIsNotEmpty = interaction.HandSlot != null;

			// Indestructible items can't be deep-fried.
			if (handIsNotEmpty
			    && interaction.HandObject.TryGetComponentCustom(out Integrity integrity)
			    && integrity.Resistances.Indestructable)
			{
				return false;
			}

			int basketIndex = GetBasketIndex(interaction.WorldPositionTarget);
			if (basketIndex < 0) return false;

			// Empty hand is only valid when the basket is down (to raise it).
			return handIsNotEmpty || deepFryer.GetBasket(basketIndex).IsDown;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			int basketIndex = GetBasketIndex(interaction.WorldPositionTarget);
			if (basketIndex < 0) return;

			InteractWithBasket(basketIndex, interaction);
		}

		#endregion

		#region Interaction-ContextMenu

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();

			var basket0Interaction = ContextMenuApply.ByLocalPlayer(gameObject, "Basket0");
			if (WillInteract(basket0Interaction, NetworkSide.Client) == false) return result;
			result.AddElement("Basket 1", () => ContextMenuOptionClicked(basket0Interaction));

			var basket1Interaction = ContextMenuApply.ByLocalPlayer(gameObject, "Basket1");
			result.AddElement("Basket 2", () => ContextMenuOptionClicked(basket1Interaction));

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
			int basketIndex = interaction.RequestedOption switch
			{
				"Basket0" => 0,
				"Basket1" => 1,
				_ => -1,
			};

			if (basketIndex < 0) return;

			InteractWithBasket(basketIndex, null);
		}

		#endregion

		private int GetBasketIndex(Vector2 worldPosition)
		{
			if (basket0Region.Contains(worldPosition)) return 0;
			if (basket1Region.Contains(worldPosition)) return 1;
			return -1;
		}

		private void InteractWithBasket(int basketIndex, PositionalHandApply interaction)
		{
			if (deepFryer.IsPowered == false)
			{
				if (interaction != null)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer,
						$"The {gameObject.ExpensiveName()} has no power.");
				}
				return;
			}

			var basket = deepFryer.GetBasket(basketIndex);

			if (basket.IsUp && interaction?.HandSlot is { IsOccupied: true })
			{
				if (deepFryer.HasEnoughOil() == false)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer,
						$"You lower the basket but it barely dips in. The {gameObject.ExpensiveName()} needs to be refilled.");
					return;
				}

				var region = basketIndex == 0 ? basket0Region : basket1Region;
				basket.Lower(interaction.HandSlot, region.SpritePosLocal);
			}
			else if (basket.IsDown)
			{
				basket.Raise();
			}
		}
	}
}
