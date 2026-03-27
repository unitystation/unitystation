using System.Collections.Generic;
using Chemistry;
using Logs;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.Items;
using US13.Items.Traits;
using US13.Managers;
using US13.Objects.Engineering;
using US13.Systems.Electricity.Interfaces;
using US13.Systems.Inventory;
using Util;

namespace US13.Objects.Chemistry
{
	/// <summary>
	/// Main component for ChemMaster, or Chemical Master™.
	/// </summary>
	public class ChemMaster : MonoBehaviour, ICheckedInteractable<HandApply>, IAPCPowerable, ICheckedInteractable<MouseDrop>
	{
		[SerializeField] public List<GameObject> ChemMasterProducts;

		public ItemStorage itemStorage;

		private void Start()
		{

			containerSlot = itemStorage.GetIndexedItemSlot(0);
			bufferItemOne = itemStorage.GetIndexedItemSlot(1);
			bufferItemTwo = itemStorage.GetIndexedItemSlot(2);
		}

		public delegate void ChangeEvent();

		public static event ChangeEvent changeEvent;

		private void UpdateGui()
		{
			// Change event runs updateAll in GUI_ChemMaster
			if (changeEvent != null)
			{
				changeEvent();
			}
		}

		#region Reagent Management

		public void TransferContainerToBuffer(Reagent reagent, float amount)
		{
			float capacity = 0;
			ReagentMix tempTransfer = GetBufferMix();
			float currentTotal = tempTransfer.Total;
			if (BufferslotOne != null)
			{
				capacity += BufferslotOne.MaxCapacity;
			}

			if (BufferslotTwo != null)
			{
				capacity += BufferslotTwo.MaxCapacity;
			}

			//math
			float space = capacity - currentTotal;
			Loggy.Trace($"Buffer| capacity:{capacity} total:{currentTotal} space:{space}", Category.Chemistry);

			//part one of transfer: isolate reagents, add to tempTransfer Mix
			if (space > 0)
			{
				Loggy.Trace($"BEFORE| Mix:{Container.CurrentReagentMix}", Category.Chemistry);
				if (amount < space)
				{
					Container.CurrentReagentMix.Remove(reagent, amount);
					tempTransfer.Add(reagent, amount);
				}
				else
				{
					Container.CurrentReagentMix.Remove(reagent, space);
					tempTransfer.Add(reagent, space);
				}

				Loggy.Trace($"AFTER|| Mix:{Container.CurrentReagentMix}", Category.Chemistry);
			}

			//part two of transfer: fill Buffer from tempTransfer Mix
			TransferMixToBuffer(tempTransfer);

			UpdateGui();
		}

		public void TransferBufferToContainer(Reagent reagent, float amount)
		{
			ReagentMix tempTransfer = GetBufferMix();

			//Container never gets swapped without clearing buffer, so we can assume there's space in container
			tempTransfer.Remove(reagent, amount);
			Container.CurrentReagentMix.Add(reagent, amount);

			TransferMixToBuffer(tempTransfer);

			UpdateGui();
		}

		public void RemoveFromBuffer(Reagent reagent, float amount)
		{
			ReagentMix tempTransfer = GetBufferMix();

			//one removal, no math
			tempTransfer.Remove(reagent, amount);

			//part two of transfer: fill Buffer from tempTransfer Mix
			TransferMixToBuffer(tempTransfer);

			UpdateGui();
		}

		/// <summary>
		/// Overrides Buffer with incoming ReagentMix.
		/// Used with GetBufferMix() to return reagents from temporary ReagentMix
		/// </summary>
		/// <param name="overridingMix">ReagentMix to override Buffer with</param>
		private void TransferMixToBuffer(ReagentMix overridingMix)
		{
			//seperate back to slots
			if (BufferslotOne)
			{
				BufferslotOne.CurrentReagentMix.Clear();
				if (overridingMix.Total <= BufferslotOne.MaxCapacity)
				{
					overridingMix.TransferTo(BufferslotOne.CurrentReagentMix, overridingMix.Total);
				}
				else
				{
					overridingMix.TransferTo(BufferslotOne.CurrentReagentMix, BufferslotOne.MaxCapacity);
				}

				Loggy.Trace($"ChemMaster: {gameObject} " +
				                $"Reagentmix buffer one after: {BufferslotOne.CurrentReagentMix}", Category.Chemistry);
			}

			if (BufferslotTwo)
			{
				BufferslotTwo.CurrentReagentMix.Clear();
				//Only two containers, and previous math confirms
				// that tempTransfer amount won't be larger than last buffer
				overridingMix.TransferTo(BufferslotTwo.CurrentReagentMix, overridingMix.Total);
				Loggy.Trace($"ChemMaster: {gameObject} " +
				                $"reagentmix buffer two after: {BufferslotTwo.CurrentReagentMix}", Category.Chemistry);
			}
		}

		public void ClearBuffer()
		{
			if (BufferslotOne)
			{
				BufferslotOne.CurrentReagentMix.Clear();
			}

			if (BufferslotTwo)
			{
				BufferslotTwo.CurrentReagentMix.Clear();
			}

			Loggy.Trace($"The buffer for ChemMaster {gameObject} is cleared.", Category.Chemistry);
		}

		public void DispenseProduct(GameObject productId, int numberOfProduct, string newName, int PillproductChoice)
		{
			ReagentMix temp = GetBufferMix();
			//Do Math
			float maxProductAmount = productId.GetCachedComponent<ReagentContainer>(includeDisabled: false).MaxCapacity;
			float maxTotalAllProducts = maxProductAmount * numberOfProduct;
			float amountPerProduct = ((maxTotalAllProducts > temp.Total) ? temp.Total : maxTotalAllProducts)
			                         / numberOfProduct;

			for (int i = 0; i < numberOfProduct; i++)
			{
				//Spawn Object
				var product = Spawn.ServerPrefab(productId, gameObject.AssumedWorldPosServer(),
					transform.parent).GameObject;

				if (product.GetComponent<ItemAttributesV2>().HasTrait(CommonTraits.Instance.Pill))
				{
					product.GetComponentInChildren<SpriteHandler>().SetCatalogueIndexSprite(PillproductChoice);
				}

				//Fill Product
				ReagentContainer productContainer = product.GetCachedComponent<ReagentContainer>(includeDisabled: false);
				if (amountPerProduct <= productContainer.MaxCapacity)
				{
					temp.TransferTo(productContainer.CurrentReagentMix, amountPerProduct);
				}
				else
				{
					temp.TransferTo(productContainer.CurrentReagentMix, productContainer.MaxCapacity);
				}

				//Give it some color
				productContainer.OnReagentMixChanged?.Invoke();

				//Give it some love
				product.GetComponent<ItemAttributesV2>().ServerSetArticleName($"{newName}" +
				                                                              $" {product.GetComponent<ItemAttributesV2>().InitialName}");
			}

			ClearBuffer();
			TransferMixToBuffer(temp);
			UpdateGui();
		}

		#endregion

		#region Internal Contents

		public ReagentContainer Container => containerSlot != null && containerSlot.ItemObject != null
			? containerSlot.ItemObject.GetCachedComponent<ReagentContainer>(includeDisabled: false)
			: null;

		public ReagentContainer BufferslotOne => bufferItemOne != null && bufferItemOne.ItemObject != null
			? bufferItemOne.ItemObject.GetCachedComponent<ReagentContainer>(includeDisabled: false)
			: null;

		public ReagentContainer BufferslotTwo => bufferItemTwo != null && bufferItemTwo.ItemObject != null
			? bufferItemTwo.ItemObject.GetCachedComponent<ReagentContainer>(includeDisabled: false)
			: null;

		public ItemSlot ItemSlot { get; }
		private ItemSlot containerSlot;
		private ItemSlot bufferItemOne;
		private ItemSlot bufferItemTwo;

		/// <summary>
		/// Retreive Buffer contents as one ReagentMix
		/// </summary>
		/// <returns> Buffer's mix of reagents </returns>
		public ReagentMix GetBufferMix()
		{
			ReagentMix emptyMix = new ReagentMix();
			if (BufferslotOne)
			{
				ReagentMix temp = BufferslotOne.CurrentReagentMix.Clone();
				temp.TransferTo(emptyMix, temp.Total);
			}

			if (BufferslotTwo)
			{
				ReagentMix temp = BufferslotTwo.CurrentReagentMix.Clone();
				temp.TransferTo(emptyMix, temp.Total);
			}

			return emptyMix;
		}

		public float GetBufferCapacity()
		{
			float returnCapacity = 0;
			if (BufferslotOne)
			{
				returnCapacity += BufferslotOne.MaxCapacity;
			}

			if (BufferslotTwo)
			{
				returnCapacity += BufferslotTwo.MaxCapacity;
			}

			return returnCapacity;
		}

		public float GetBufferSpace()
		{
			return GetBufferCapacity() - GetBufferMix().Total;
		}

		#endregion

		#region Interactions

		private ItemSlot GetBestSlot(GameObject item, PlayerInfo subject)
		{
			if (subject == null)
			{
				return default;
			}

			var playerStorage = subject.Script.DynamicItemStorage;
			return playerStorage.GetBestHandOrSlotFor(item);
		}

		/// <summary>
		/// Ejects input container from ChemMaster into best slot available and clears the buffer
		/// </summary>
		/// <param name="subject"></param>
		public void EjectContainer(PlayerInfo subject)
		{
			containerSlot.Item.GetCachedComponent<ReagentContainer>(includeDisabled: false).OnReagentMixChanged.Invoke();
			var bestSlot = GetBestSlot(containerSlot.ItemObject, subject);
			if (!Inventory.ServerTransfer(containerSlot, bestSlot))
			{
				Inventory.ServerDrop(containerSlot);
			}

			ClearBuffer();
			UpdateGui();
		}

		public bool WillInteract(MouseDrop interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//only interaction that works is using a reagent container on this
			if (Validations.HasComponent<ReagentContainer>(interaction.DroppedObject) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(MouseDrop interaction)
		{

			if (containerSlot.IsOccupied)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The machine already has a beaker in it");
				return;
			}

			//Inserts reagent container
			Inventory.ServerAdd(interaction.DroppedObject, containerSlot);
			UpdateGui();
		}


		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//only interaction that works is using a reagent container on this
			if (!Validations.HasComponent<ReagentContainer>(interaction.HandObject)) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{

			if (containerSlot.IsOccupied)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The machine already has a beaker in it");
				return;
			}

			//Inserts reagent container
			Inventory.ServerTransfer(interaction.HandSlot, containerSlot);
			UpdateGui();
		}

		#endregion

		#region IAPCPowerable

		public PowerState ThisState;

		public void PowerNetworkUpdate(float voltage)
		{
		}

		public void StateUpdate(PowerState state)
		{
			ThisState = state;
		}

		#endregion
	}
}