using System.Collections;
using System.Collections.Generic;
using Mirror;
using NaughtyAttributes;
using Systems.Explosions;
using UnityEngine;


namespace Items.Storage
{
	public class PizzaBox : NetworkBehaviour, ICheckedInteractable<HandApply>, IInteractable<InventoryApply>
	{
		[Header("Settings")]
		[SerializeField] private ItemTrait pizzaTrait;
		[SerializeField] private SpriteDataSO spritePizzaBoxOpen;
		[SerializeField] private SpriteDataSO spritePizzaBoxClosed;
		[SerializeField] private SpriteDataSO spritePizzaBoxMessy;
		[SerializeField, ShowIf("isBomb")] private SpriteDataSO spritePizzaBoxBombInactive;
		[SerializeField, ShowIf("isBomb")] private SpriteDataSO spritePizzaBoxBombActive;
		private bool isOpen = false;
		private bool hadPizza = false;
		private bool bombIsCountingDown;
		[SerializeField] private bool isBomb = false;
		[SerializeField, ShowIf("isBomb")] private float bombStrength = 3700;
		[SerializeField] private string writtenNote = "";
		[Header("Components")]
		[SerializeField] private SpriteHandler boxSprites;
		[SerializeField] private SpriteHandler writingSprites;
		[SerializeField] private SpriteHandler pizzaSprites;
		[SerializeField] private ItemStorage pizzaBoxStorage;
		[SerializeField] private ObjectBehaviour objectBehaviour;


		private void Detonate()
		{
			if (isServer)
			{
				// Get data before despawning
				var worldPos = objectBehaviour.AssumedWorldPositionServer();

				// Despawn the explosive
				_ = Despawn.ServerSingle(gameObject);
				Explosion.StartExplosion(worldPos, bombStrength);
			}
		}

		private void UpdatePizzaSprites(GameObject pizza)
		{
			var sprite = pizza.GetComponentInChildren<SpriteHandler>();
			if (sprite != null)
			{
				pizzaSprites.SetSprite(sprite.CurrentSprite);
			}
		}

		private void OpenBox()
		{
			isOpen = true;
			pizzaSprites.SetActive(true);
			if(writtenNote != "") writingSprites.SetActive(true);
			if (isBomb)
			{
				if (bombIsCountingDown)
				{
					boxSprites.SetSpriteSO(spritePizzaBoxBombActive);
					return;
				}
				boxSprites.SetSpriteSO(spritePizzaBoxBombInactive);
			}
			if (hadPizza)
			{
				boxSprites.SetSpriteSO(spritePizzaBoxMessy);
			}
			else
			{
				boxSprites.SetSpriteSO(spritePizzaBoxOpen);
			}
		}

		private void CloseBox()
		{
			isOpen = false;
			boxSprites.SetSpriteSO(spritePizzaBoxClosed);
			pizzaSprites.SetActive(false);
			writingSprites.SetActive(false);
			if (pizzaBoxStorage.GetNextFreeIndexedSlot() == null)
			{
				hadPizza = true;
			}
		}


		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (isOpen == false || interaction.IsAltClick == false) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (isOpen == false)
			{
				OpenBox();
				return;
			}
			if (interaction.UsedObject != null)
			{
				if (interaction.UsedObject.Item().HasTrait(pizzaTrait))
				{
					if (pizzaBoxStorage.ServerTryTransferFrom(interaction.UsedObject))
					{
						UpdatePizzaSprites(interaction.UsedObject);
						return;
					}
					Chat.AddExamineMsg(interaction.Performer, $"<color=red>You can't add {interaction.UsedObject} " +
					                                          $"to the {gameObject.ExpensiveName()} because there's already something in it!</color>");
					return;
				}
			}

			if (interaction.HandSlot.IsEmpty && pizzaBoxStorage.GetNextFreeIndexedSlot() == null)
			{
				if (Inventory.ServerTransfer(pizzaBoxStorage.GetTopOccupiedIndexedSlot(), interaction.HandSlot))
				{
					pizzaSprites.PushClear();
				}
				return;
			}

			CloseBox();
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (isOpen == false)
			{
				OpenBox();
				return;
			}
			CloseBox();
		}
	}

}
