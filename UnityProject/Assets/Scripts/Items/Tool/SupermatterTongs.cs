using UnityEngine;
using UnityEngine.Events;

namespace Items
{
	public class SupermatterTongs : MonoBehaviour, ICheckedInteractable<InventoryApply>, ICheckedInteractable<HandApply>, ICheckedInteractable<HandActivate>
	{
		private const int NOT_LOADED_SPRITE = 0;
		private const int LOADED_SPRITE = 1;

		[SerializeField]
		private ItemTrait supermatterSliverContainer;

		public SpriteHandler[] spriteHandlers = new SpriteHandler[] { };
		private ItemStorage itemStorage;
		private ItemSlot sliverSlot;

		private void Awake()
		{
			itemStorage = GetComponent<ItemStorage>();
			sliverSlot = itemStorage.GetIndexedItemSlot(0);
			sliverSlot.OnSlotContentsChangeServer.AddListener(OnServerSlotContentsChange);
		}

		private void OnEnable()
        {
			sliverSlot.OnSlotContentsChangeServer.AddListener(OnServerSlotContentsChange);
		}

		private void OnDisable()
		{
			sliverSlot.OnSlotContentsChangeServer.RemoveListener(OnServerSlotContentsChange);
		}

		private void OnServerSlotContentsChange()
		{
			UpdateSprites();
		}

		public void LoadSliver(Pickupable toLoad)
		{
			Inventory.ServerAdd(toLoad, sliverSlot);
		}

		private void UpdateSprites()
		{
			foreach (var handler in spriteHandlers)
			{
				if (handler)
				{
					int newSpriteID = sliverSlot.ItemObject == null ? NOT_LOADED_SPRITE : LOADED_SPRITE;
					handler.ChangeSprite(newSpriteID);
				}
			}
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (!Validations.HasItemTrait(interaction.TargetObject, supermatterSliverContainer)) return false;

			return true;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (interaction.TargetObject.TryGetComponent<SupermatterSliverContainer>(out var smContainer))
			{
				if (!smContainer.isSealed && !smContainer.isLoaded)
				{
					if (smContainer.LoadSliver(sliverSlot))
					{
						Chat.AddActionMsgToChat(interaction.Performer,
							$"You load supermatter sliver into the container.",
							$"{interaction.Performer.ExpensiveName()} loads supermatter sliver into the container.");
					}
				}
				else if (!smContainer.isSealed && smContainer.isLoaded)
				{
					if (smContainer.UnloadSliver())
					{
						Chat.AddActionMsgToChat(interaction.Performer,
							$"You unload supermatter sliver from the container.",
							$"{interaction.Performer.ExpensiveName()} unloads supermatter sliver from the container.");
					}
				}
				else
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, $"Container is sealed, you can't load/unload sliver.");
				}
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (!Validations.HasItemTrait(interaction.TargetObject, supermatterSliverContainer)) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.TargetObject.TryGetComponent<SupermatterSliverContainer>(out var smContainer))
			{
				if (!smContainer.isSealed && !smContainer.isLoaded)
				{
					if (smContainer.LoadSliver(sliverSlot))
					{
						Chat.AddActionMsgToChat(interaction.Performer,
							$"You load supermatter sliver into the container.",
							$"{interaction.Performer.ExpensiveName()} loads supermatter sliver into the container.");
					}
				}
				else if (!smContainer.isSealed && smContainer.isLoaded)
				{
					if (smContainer.UnloadSliver())
					{
						Chat.AddActionMsgToChat(interaction.Performer,
							$"You unload supermatter sliver from the container.",
							$"{interaction.Performer.ExpensiveName()} unloads supermatter sliver from the container.");
					}
				}
				else
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, $"Container is sealed, you can't load/unload sliver.");
				}
			}
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			Inventory.ServerDrop(sliverSlot);
			UpdateSprites();
		}
	}
}