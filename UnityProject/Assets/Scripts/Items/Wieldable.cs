using Messages.Server;
using Mirror;
using UnityEngine;

namespace Items
{
	public enum HiddenHandValue
	{
		bothHands = 0,
		leftHand = 1,
		rightHand = 2,
		none = 3
	}

	public class Wieldable : NetworkBehaviour, IServerInventoryMove, ICheckedInteractable<HandActivate>
	{
		[SerializeField]
		private int damageUnwielded;

		[SerializeField]
		private int damageWielded;

		public ItemsSprites Wielded = new ItemsSprites();
		public ItemsSprites Unwielded = new ItemsSprites();

		[SyncVar(hook = nameof(SyncState))]
		private bool isWielded;

		private ItemAttributesV2 itemAttributes;

		private void Awake()
		{
			itemAttributes = GetComponent<ItemAttributesV2>();
		}

		private void OnEnable()
		{
			HandsController.OnSwapHand.AddListener(OnSwapHands);
		}

		private void OnDisable()
		{
			HandsController.OnSwapHand.RemoveListener(OnSwapHands);
		}

		private void OnSwapHands()
		{
			if (isWielded)
			{
				Chat.AddExamineMsgFromServer(PlayerManager.LocalPlayerScript.gameObject, $"Your other hand is too busy holding {gameObject.ExpensiveName()}!");
				HandsController.OnSwapHand.RemoveListener(OnSwapHands);
				HandsController.SwapHand();
				HandsController.OnSwapHand.AddListener(OnSwapHands);
			}
		}

		private void SyncState(bool oldState, bool newState)
		{
			isWielded = newState;

			if (isWielded)
			{
				itemAttributes.SetSprites(Wielded);
			}
			else
			{
				itemAttributes.SetSprites(Unwielded);
			}
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (this.gameObject != info.MovedObject.gameObject) return;

			if (info.InventoryMoveType == InventoryMoveType.Remove)
			{
				isWielded = false;
				itemAttributes.ServerHitDamage = damageUnwielded;
				itemAttributes.SetSprites(Unwielded);
				HideHand( HiddenHandValue.none, info.FromPlayer.PlayerScript);
			}
			else if (info.InventoryMoveType == InventoryMoveType.Transfer)
			{
				isWielded = false;
				itemAttributes.ServerHitDamage = damageUnwielded;
				itemAttributes.SetSprites(Unwielded);
				HideHand( HiddenHandValue.none, info.FromPlayer.PlayerScript);
			}
		}

		[Server]
		private void HideHand(HiddenHandValue hiddenHandSelection, PlayerScript playerScript)
		{
			//TODO Think a better implementation since this hides for All hands
		}

		private ItemSlot DetermineHiddenHand(HandActivate interaction)
		{
			ItemSlot hiddenHand = null;
			var playerStorage = interaction.PerformerPlayerScript.DynamicItemStorage;
			var currentSlot = playerStorage.GetActiveHandSlot();

			var leftHands = playerStorage.GetNamedItemSlots(NamedSlot.leftHand);
			foreach (var leftHand in leftHands)
			{
				if (leftHand != currentSlot && Validations.HasComponent<Wieldable>(leftHand.ItemObject) == false)
				{
					hiddenHand = leftHand;
				}
			}

			var rightHands = playerStorage.GetNamedItemSlots(NamedSlot.rightHand);
			foreach (var rightHand in rightHands)
			{
				if (rightHand != currentSlot && Validations.HasComponent<Wieldable>(rightHand.ItemObject) == false)
				{
					hiddenHand = rightHand;
				}
			}

			return hiddenHand;
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			ItemSlot hiddenHand = DetermineHiddenHand(interaction);

			if (hiddenHand != null)
			{
				HiddenHandValue hiddenHandSelection = HiddenHandValue.bothHands;

				if (hiddenHand.NamedSlot.GetValueOrDefault(NamedSlot.none) == NamedSlot.leftHand)
				{
					hiddenHandSelection =  HiddenHandValue.leftHand;
				}
				else if (hiddenHand.NamedSlot.GetValueOrDefault(NamedSlot.none) == NamedSlot.rightHand)
				{
					hiddenHandSelection = HiddenHandValue.rightHand;
				}

				Inventory.ServerDrop(hiddenHand);

				SyncState(isWielded, !isWielded);


				if (isWielded)
				{
					itemAttributes.ServerHitDamage = damageWielded;
					itemAttributes.SetSprites(Wielded);
					Chat.AddExamineMsgFromServer(interaction.Performer, $"You wield {gameObject.ExpensiveName()} grabbing it with both of your hands.");
					HideHand(hiddenHandSelection, interaction.PerformerPlayerScript);
				}
				else
				{
					itemAttributes.ServerHitDamage = damageUnwielded;
					itemAttributes.SetSprites(Unwielded);
					Chat.AddExamineMsgFromServer(interaction.Performer, $"You unwield {gameObject.ExpensiveName()}.");
					HideHand(HiddenHandValue.none, interaction.PerformerPlayerScript);
				}

				PlayerAppearanceMessage.SendToAll(interaction.Performer, (int)interaction.HandSlot.NamedSlot.GetValueOrDefault(NamedSlot.none), gameObject);
			}
		}
	}
}
