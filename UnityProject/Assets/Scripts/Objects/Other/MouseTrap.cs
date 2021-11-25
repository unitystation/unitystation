using System;
using UnityEngine.EventSystems;
using UnityEngine;
using HealthV2;


namespace Objects.Other
{
	public class MouseTrap : FloorHazard, ICheckedInteractable<HandActivate>
	{

		[SerializeField] private bool isArmed;
		[SerializeField] protected bool ignoresHandwear;
		[SerializeField] private ItemTrait trapTrait;
		[SerializeField] private SpriteHandler trapPreview;
		[SerializeField] private ItemStorage trapContent;

		private BodyPartType[] handTypes = {BodyPartType.LeftArm, BodyPartType.RightArm};
		private bool trapInSnare;
		public bool IsArmed => isArmed;

		public void Awake()
		{
			if (trapPreview == null)
			{
				Logger.LogError($"{gameObject} spawned with a null trapPreview. We can't get it on awake due to the existence of two SpriteHandlers!");
			}
		}

		private bool ArmTrap()
		{
			isArmed = !isArmed;
			return isArmed;
		}

		/// <summary>
		/// for triggering traps when inside storage containers
		/// </summary>
		/// <param name="health"></param>
		private void HurtHand(LivingHealthMasterBase health)
		{
			foreach (var hand in health.playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.hands))
			{
				if(ignoresHandwear == false && hand.IsEmpty == false) continue;
				ApplyDamageToPartyType(health, handTypes.PickRandom());
			}
			Chat.AddActionMsgToChat(gameObject, $"You are surprised with a {gameObject.ExpensiveName()} biting your hand!",
				$"{health.playerScript.visibleName} screams in pain and surprise as {gameObject.ExpensiveName()} " +
				$"bites {health.playerScript.characterSettings.TheirPronoun(health.playerScript)} hand!");
			PlayStepAudio();
		}

		/// <summary>
		/// Things to trigger for attached items if found + hurting peoples hands.
		/// </summary>
		/// <param name="health"></param>
		public void TriggerTrapFromContainer(LivingHealthMasterBase health = null)
		{
			if(health != null) HurtHand(health);
			//TODO : Add the ability to attach items to mousetraps and trigger them.
		}

		private bool HasTrapTrait(ItemSlot handSlot)
		{
			if (handSlot.Item != null)
			{
				if (handSlot.Item.gameObject.Item().HasTrait(trapTrait)) return true;
			}

			return false;
		}

		private void UpdateTrapVisual()
		{
			if (trapContent.GetNextFreeIndexedSlot() == null)
			{
				trapPreview.Empty();
				return;
			}
			//We assume that there will be only one item on each mouse trap as intended
			var slot = trapContent.GetTopOccupiedIndexedSlot();
			if (slot.Item.gameObject.TryGetComponent<SpriteHandler>(out var sprite))
			{
				trapPreview.SetSpriteSO(sprite.GetCurrentSpriteSO());
			}
		}

		public override void OnStep(GameObject eventData)
		{
			if (IsArmed == false) return;
			base.OnStep(eventData);
			isArmed = false;
		}

		public override bool WillStep(GameObject eventData)
		{
			if (eventData.gameObject.TryGetComponent<LivingHealthMasterBase>(out var _) == true) return true;
			return false;
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (HasTrapTrait(interaction.HandSlot)) return true;
			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (HasTrapTrait(interaction.HandSlot))
			{
				trapContent.ServerTryAdd(interaction.HandSlot.ItemObject);
				trapInSnare = true;
				UpdateTrapVisual();
				return;
			}

			ArmTrap();
			Chat.AddExamineMsgFromServer(interaction.Performer,
				isArmed ? "You arm the " + gameObject.ExpensiveName() : "You disarm the " + gameObject.ExpensiveName());
		}
	}
}