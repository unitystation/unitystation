using Cysharp.Threading.Tasks;
using Logs;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Sprite_Handler;
using US13.Items;
using US13.Health.Objects;
using US13.Items.Kitchen;
using US13.Systems.Inventory;
using Util;

namespace US13.Objects.Machines
{
	public enum BasketState
	{
		Up,
		Down
	}

	/// <summary>
	/// Indices into the SpriteHandler SubCatalogue for basket animations.
	/// Must match the order set in the inspector.
	/// </summary>
	public enum BasketSpriteState
	{
		Off = 0,
		Start = 1,
		On = 2,
		Stop = 3
	}

	/// <summary>
	/// Pure C# class representing a single deep fryer basket.
	/// Owned and ticked by <see cref="DeepFryer"/>.
	/// </summary>
	public class FryerBasket
	{
		public BasketState State { get; private set; } = BasketState.Up;
		public ItemSlot StorageSlot { get; }

		private readonly DeepFryer owner;
		private readonly SpriteHandler spriteHandler;

		private Vector2 dropOffset;

		private Cookable cachedCookable;
		private DeepFryable cachedDeepFryable;
		private ReagentContainer cachedReagentContainer;
		public bool IsUp => State == BasketState.Up;
		public bool IsDown => State == BasketState.Down;
		public bool HasItem => StorageSlot.IsOccupied;
		public bool IsCookableByFryer => cachedCookable != null;

		public FryerBasket(ItemSlot storageSlot, DeepFryer owner, SpriteHandler spriteHandler)
		{
			StorageSlot = storageSlot;
			this.owner = owner;
			this.spriteHandler = spriteHandler;
		}

		/// <summary>
		/// Called by DeepFryer each tick while the machine is powered and this basket is down.
		/// </summary>
		public void Tick(float secondsPerTick, float voltageModifier)
		{
			if (State != BasketState.Down) return;
			if (StorageSlot.IsEmpty) return;
			if (owner.HasEnoughOil() == false)
			{
				owner.SendLocalMessage("The deep fryer sputters and the basket rises on its own.");
				Raise();
				return;
			}

			float scaledTime = secondsPerTick * voltageModifier;
			owner.TransferOilTo(cachedReagentContainer);

			if (IsCookableByFryer)
			{
				TickCookable(scaledTime);
				return;
			}

			if (cachedDeepFryable != null)
			{
				TickRegularFriedItem(scaledTime);
				return;
			}

			Loggy.Error()
				.Format("Someone is trying to fry an unfryable thing!!! {0}",
					Category.Chemistry,
					StorageSlot.Item.gameObject.name);
		}

		private void TickRegularFriedItem(float scaledTime)
		{
			bool reachedPerfection = cachedDeepFryable.AddFryingTime(scaledTime);
			if (reachedPerfection)
			{
				owner.PlayDing();
			}
		}

		private void TickCookable(float scaledTime)
		{
			cachedCookable.AddCookingTime(scaledTime);
		}

		/// <summary>
		/// Called when a Cookable item finishes cooking. CookProduct has already run
		/// at this point the original item is destroyed and the cooked result is in the slot.
		/// </summary>
		private void OnCookableCooked()
		{
			EjectCookedItemNextFrame().Forget();
		}

		private async UniTaskVoid EjectCookedItemNextFrame()
		{
			// wait until next frame just to be sure
			await UniTask.NextFrame();

			ClearComponents();
			State = BasketState.Up;
			if (StorageSlot.IsOccupied)
			{
				Inventory.ServerDrop(StorageSlot, dropOffset);
			}
			spriteHandler.AnimateOnce((int)BasketSpriteState.Stop);
			owner.PlayEmerge();
			owner.PlayDing();
			owner.RefreshWattage();
		}

		/// <summary>
		/// Lowers the basket into the oil and transfers the item from the player's hand into the storage slot.
		/// </summary>
		public void Lower(ItemSlot handSlot, Vector2 basketDropOffset)
		{
			if (State != BasketState.Up) return;
			if (owner.IsPowered == false) return;
			if (owner.HasEnoughOil() == false) return;
			if (handSlot == null || handSlot.IsEmpty) return;
			if (StorageSlot.IsOccupied) return;

			// If the item is stackable, split one off to fry individually.
			if (handSlot.ItemObject.TryGetComponentCustom(out Stackable stackable) && stackable.Amount > 1 )
			{
				GameObject singleItem = stackable.ServerTake(1);
				if (Inventory.ServerAdd(singleItem.GetComponentCustom<Pickupable>(), StorageSlot) == false) return;
			}
			else
			{
				if (Inventory.ServerTransfer(handSlot, StorageSlot) == false) return;
			}

			CacheComponents();
			if (cachedDeepFryable != null)
			{
				cachedDeepFryable.StartFrySession();
			}
			State = BasketState.Down;
			dropOffset = basketDropOffset;
			spriteHandler.AnimateOnce((int)BasketSpriteState.Start, inAnimateNext: true);
			owner.RefreshWattage();
		}

		/// <summary>
		/// Raises the basket out of the oil. No-op if already up.
		/// </summary>
		public void Raise()
		{
			if (State != BasketState.Down) return;

			State = BasketState.Up;
			if (cachedDeepFryable != null)
			{
				cachedDeepFryable.EndFrySession();
			}
			ClearComponents();

			if (StorageSlot.IsOccupied)
			{
				Inventory.ServerDrop(StorageSlot, dropOffset);
			}

			spriteHandler.AnimateOnce((int)BasketSpriteState.Stop);
			owner.PlayEmerge();
			owner.RefreshWattage();
		}

		private void CacheComponents()
		{
			GameObject item = StorageSlot.ItemObject;
			item.TryGetComponentCustom(out cachedCookable);
			if (cachedCookable != null && cachedCookable.CookableBy.HasFlag(CookSource.DeepFrier) == false)
			{
				cachedCookable = null;
			}

			item.TryGetComponentCustom(out cachedDeepFryable);
			item.TryGetComponentCustom(out cachedReagentContainer);

			if (cachedCookable != null)
			{
				cachedCookable.OnCooked.AddListener(OnCookableCooked);
			}
		}

		private void ClearComponents()
		{
			if (cachedCookable != null)
			{
				cachedCookable.OnCooked.RemoveListener(OnCookableCooked);
			}
			cachedCookable = null;
			cachedDeepFryable = null;
			cachedReagentContainer = null;
		}
	}
}
