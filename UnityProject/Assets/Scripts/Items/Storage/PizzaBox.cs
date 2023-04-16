using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using NaughtyAttributes;
using Communications;
using Items.Weapons;
using Managers;
using Systems.Explosions;
using UI.Items;
using Mirror;

namespace Items.Storage
{
	public class PizzaBox : ExplosiveBase, ICheckedInteractable<HandApply>, IInteractable<InventoryApply>, IServerSpawn, ICheckedInteractable<HandActivate>
	{
		[Header("Settings")]
		[SerializeField] private ItemTrait pizzaTrait;
		[SerializeField] private SpriteDataSO spritePizzaBoxOpen;
		[SerializeField] private SpriteDataSO spritePizzaBoxClosed;
		[SerializeField] private SpriteDataSO spritePizzaBoxMessy;
		[SerializeField, ShowIf("isBomb")] private SpriteDataSO spritePizzaBoxBombInactive;
		[SerializeField, ShowIf("isBomb")] private SpriteDataSO spritePizzaBoxBombActive;
		[SerializeField, ShowIf("isBomb")] private bool isArmedOnSpawn;
		[SyncVar] private bool isOpen = false;
		[SyncVar] private bool hadPizza = false;
		[SyncVar] private bool bombIsCountingDown;
		[SyncVar(hook=nameof(OnArmStateChange))] private bool detonateByTimer = false;
		[SerializeField] private bool isBomb = false;
		[SerializeField] private string writtenNote = "";
		[Header("Components")]
		[SerializeField] private SpriteHandler boxSprites;
		[SerializeField] private SpriteHandler writingSprites;
		[SerializeField] private SpriteHandler pizzaSprites;
		[SerializeField] private ItemStorage pizzaBoxStorage;
		[SerializeField, ShowIf("isBomb")] private HasNetworkTabItem netTab;
		[HideInInspector] public GUI_PizzaBomb PizzaGui;

		public bool BombIsCountingDown
		{
			set => bombIsCountingDown = value;
			get => bombIsCountingDown;
		}
		public bool DetonateByTimer
		{
			set => detonateByTimer = value;
			get => detonateByTimer;
		}

		private void Start()
		{
			if (isBomb && netTab == null)
			{
				netTab = GetComponent<HasNetworkTabItem>();
			}
		}

		public override IEnumerator Countdown()
		{
			if (bombIsCountingDown) yield break;
			bombIsCountingDown = true;
			if (isOpen) pizzaSprites.SetSpriteSO(spritePizzaBoxBombActive);
			if (writtenNote != "")
			{
				Chat.AddActionMsgToChat(gameObject, $"<color=red>An explosive can be seen ticking from the {gameObject.ExpensiveName()} " +
													$"and below it is a note that reads '{writtenNote}'!</color>");
			}
			if (PizzaGui != null) PizzaGui.StartCoroutine(PizzaGui.UpdateTimer());
			yield return WaitFor.Seconds(timeToDetonate);
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

			if (isArmed && detonateByTimer == false)
			{
				Detonate();
				return;
			}

			if (isArmed && detonateByTimer)
			{
				StartCoroutine(Countdown());
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

		protected override void Detonate()
		{
			Chat.AddCombatMsgToChat(gameObject, default, "<size=+6>The pizza bomb violently explodes!</size>");
			base.Detonate();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.IsAltClick == false) return false; // alt click to open on the spot, else pick it up
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
				if(TryAddPizza(interaction.UsedObject) == false)
				{
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
			if (isOpen && interaction.TargetObject != null && interaction.TargetObject != gameObject)
			{
				if(TryAddPizza(interaction.TargetObject)) return;
				Chat.AddExamineMsg(interaction.Performer, $"<color=red>You can't add {interaction.TargetObject} " +
				                                          $"to the {gameObject.ExpensiveName()} because there's already something in it!</color>");
				return;
			}
			if (isBomb && interaction.IsAltClick == false)
			{
				netTab.ServerPerformInteraction(interaction);
				return;
			}
			CloseBox();
		}

		private bool TryAddPizza(GameObject pizzaItem)
		{
			if (pizzaItem.Item().HasTrait(pizzaTrait) == false) return false;
			if (pizzaBoxStorage.ServerTryTransferFrom(pizzaItem))
			{
				UpdatePizzaSprites();
				return true;
			}
			return false;
		}

		public override void ReceiveSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message = null)
		{
			if(isArmed == false) return;
			if (detonateByTimer)
			{
				StartCoroutine(Countdown());
				return;
			}
			Detonate();
		}

		public override void OnSpawnServer(SpawnInfo info)
		{
			base.OnSpawnServer(info);

			IsArmed = isBomb && isArmedOnSpawn;
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			// if it is a bomb, it is armed and is not by timer, allow the interaction else pass it to NetTab
			return isBomb && isArmed && detonateByTimer == false;
		}
		public void ServerPerformInteraction(HandActivate interaction)
		{
			Detonate();
		}

		protected override void OnArmStateChange(bool oldState, bool newState)
		{
			netTab.enabled = !isArmed && !detonateByTimer;
		}
	}
}
