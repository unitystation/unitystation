using System.Collections.Generic;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Health.Objects;
using US13.Objects.Machines;
using US13.Player;
using US13.UI.Core.RightClick;
using US13.UI.Systems.Tooltips.HoverTooltips;
using Util;

namespace US13.Objects.Kitchen
{
	[RequireComponent(typeof(DeepFryer))]
	public class InteractableDeepFryer : MonoBehaviour, IExaminable, IFirstInteractable<PositionalHandApply>,
		IRightClickable, ICheckedInteractable<ContextMenuApply>, IHoverTooltip
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

			if (IsBasketInteractable(0))
			{
				result.AddElement("Basket 1", () => ContextMenuOptionClicked(basket0Interaction));
			}

			if (IsBasketInteractable(1))
			{
				var basket1Interaction = ContextMenuApply.ByLocalPlayer(gameObject, "Basket1");
				result.AddElement("Basket 2", () => ContextMenuOptionClicked(basket1Interaction));
			}

			return result;
		}

		private bool IsBasketInteractable(int index)
		{
			if (deepFryer.IsPowered == false) return false;

			var basket = deepFryer.GetBasket(index);

			// Can raise a lowered basket.
			if (basket.IsDown) return true;

			// Can place an item in a raised empty basket.
			return basket.IsUp && basket.HasItem == false;
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

		#region IHoverTooltip

		public string HoverTip()
		{
			if (deepFryer.IsPowered == false)
			{
				return "The machine is dead, no power.";
			}

			if (deepFryer.HasEnoughOil() == false)
			{
				return "A red LED blinks, not enough oil.";
			}

			return "A green LED blinks, ready to use!";
		}

		public string CustomTitle() => null;

		public Sprite CustomIcon() => null;

		public List<Sprite> IconIndicators() => null;

		public List<TextColor> InteractionsStrings()
		{
			var activeHand = PlayerManager.Equipment?.ItemStorage?.GetActiveHandSlot();
			bool handIsEmpty = activeHand == null || activeHand.IsOccupied == false;
			var interactions = new List<TextColor>();

			if (deepFryer.IsPowered == false || deepFryer.HasEnoughOil() == false)
			{
				return interactions;
			}

			for (int i = 0; i < 2; i++)
			{
				var basket = deepFryer.GetBasket(i);

				if (basket.IsUp && basket.HasItem == false && handIsEmpty == false)
				{
					interactions.Add(new TextColor
					{
						Color = IntentColors.Help,
						Text = $"Click basket {i + 1} to fry held item."
					});
				}
				else if (basket.IsDown && handIsEmpty)
				{
					interactions.Add(new TextColor
					{
						Color = IntentColors.Help,
						Text = $"Click basket {i + 1} to raise it."
					});
				}
			}

			return interactions;
		}

		#endregion

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
