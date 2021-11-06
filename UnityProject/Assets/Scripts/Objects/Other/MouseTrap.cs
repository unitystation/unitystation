using UnityEngine.EventSystems;
using UnityEngine;
using HealthV2;


namespace Objects.Other
{
	public class MouseTrap : FloorHazard, ICheckedInteractable<HandActivate>
	{

		[SerializeField] private bool isArmed;
		public bool IsArmed => isArmed;

		private BodyPartType[] handTypes = {BodyPartType.LeftArm, BodyPartType.RightArm};

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
			PlayAudio();
		}

		/// <summary>
		/// Things to trigger for attached items if found + hurting peoples hands.
		/// </summary>
		/// <param name="health"></param>
		public void TriggerTrapFromContainer(LivingHealthMasterBase health)
		{
			if(health != null) HurtHand(health);
			//TODO : Add the ability to attach items to mousetraps and trigger them.
		}


		public override void OnStep(BaseEventData eventData)
		{
			if (IsArmed == false) return;
			LivingHealthMasterBase health = eventData.selectedObject.GetComponent<LivingHealthMasterBase>();
			isArmed = false;
			HurtFeet(health);
			Chat.AddActionMsgToChat(gameObject, $"You step on the {gameObject.ExpensiveName()}!",
				$"{health.playerScript.visibleName} steps on the {gameObject.ExpensiveName()}!");
			PlayAudio();
		}

		public override bool WillStep(BaseEventData eventData)
		{
			if (eventData.selectedObject.gameObject.TryGetComponent<LivingHealthMasterBase>(out var _) == true) return true;
			return false;
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (ArmTrap())
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "You arm the " + gameObject.ExpensiveName());
				return;
			}
			Chat.AddExamineMsgFromServer(interaction.Performer, "You disarm the " + gameObject.ExpensiveName());
		}
	}
}