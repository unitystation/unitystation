using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using NaughtyAttributes;
using Communications;
using Managers;
using Systems.Explosions;
using UI.Items;
using Mirror;

namespace Items.Storage
{
	public class PizzaBox : SignalReceiver, ICheckedInteractable<HandApply>, IInteractable<InventoryApply>
	{
		[Header("Settings")]
		[SerializeField] private ItemTrait pizzaTrait;
		[SerializeField] private SpriteDataSO spritePizzaBoxOpen;
		[SerializeField] private SpriteDataSO spritePizzaBoxClosed;
		[SerializeField] private SpriteDataSO spritePizzaBoxMessy;
		[SerializeField, ShowIf("isBomb")] private SpriteDataSO spritePizzaBoxBombInactive;
		[SerializeField, ShowIf("isBomb")] private SpriteDataSO spritePizzaBoxBombActive;
		[SyncVar] private bool isOpen = false;
		[SyncVar] private bool hadPizza = false;
		[SyncVar] private bool bombIsCountingDown;
		private int timeToDetonate;
		private bool detenationOnTimer = false;
		[SyncVar] private bool isArmed;
		[SerializeField] private bool isBomb = false;
		[SerializeField, ShowIf("isBomb")] private float bombStrength = 3700;
		[SerializeField] private string writtenNote = "";
		[Header("Components")]
		[SerializeField] private SpriteHandler boxSprites;
		[SerializeField] private SpriteHandler writingSprites;
		[SerializeField] private SpriteHandler pizzaSprites;
		[SerializeField] private ItemStorage pizzaBoxStorage;
		[SerializeField] private ObjectBehaviour objectBehaviour;
		[SerializeField, ShowIf("isBomb")] private HasNetworkTabItem netTab;
		[HideInInspector] public GUI_PizzaBomb GUI;

		public bool BombIsCountingDown
		{
			set => bombIsCountingDown = value;
			get => bombIsCountingDown;
		}
		public bool DetenationOnTimer
		{
			set => detenationOnTimer = value;
			get => detenationOnTimer;
		}
		public float TimeToDetonate
		{
			set => timeToDetonate = (int)value;
			get => timeToDetonate;
		}

		public bool IsArmed
		{
			set => isArmed = value;
			get => isArmed;
		}

		private void Start()
		{
			if (isBomb && netTab == null)
			{
				netTab = GetComponent<HasNetworkTabItem>();
			}
		}

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

		public async void Countdown()
		{
			if (bombIsCountingDown) return;
			bombIsCountingDown = true;
			if (isOpen) pizzaSprites.SetSpriteSO(spritePizzaBoxBombActive);
			if (writtenNote != "")
			{
				Chat.AddLocalMsgToChat($"<color=red>An explsovive can be seen from the {gameObject.ExpensiveName()} " +
				                       $"and below it is a note that reads '{writtenNote}'!</color>", gameObject);
			}
			if (GUI != null) GUI.StartCoroutine(GUI.UpdateTimer());
			await Task.Delay(timeToDetonate * 1000).ConfigureAwait(false); //Delay is in milliseconds
			Detonate();
		}

		private void UpdatePizzaSprites()
		{
			var pizza = pizzaBoxStorage.GetTopOccupiedIndexedSlot();
			if(pizza == null) return;
			var sprite = pizza.ItemObject.GetComponentInChildren<SpriteHandler>();
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
			UpdatePizzaSprites();
			if (isArmed && detenationOnTimer == false)
			{
				Countdown();
				return;
			}
			if (isBomb)
			{
				if (bombIsCountingDown)
				{
					boxSprites.SetSpriteSO(spritePizzaBoxBombActive);
					return;
				}
				boxSprites.SetSpriteSO(spritePizzaBoxBombInactive);
				return;
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
			if (isOpen == false && interaction.IsAltClick == false) return false;
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
						UpdatePizzaSprites();
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
			if (isBomb && interaction.IsAltClick == false)
			{
				netTab.ServerPerformInteraction(interaction);
				return;
			}
			CloseBox();
		}

		public override void ReceiveSignal(SignalStrength strength, ISignalMessage message = null)
		{
			if(isArmed == false) return;
			if (detenationOnTimer)
			{
				Countdown();
				return;
			}
			Detonate();
		}
	}
}
