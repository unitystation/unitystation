using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using HealthV2;
using NaughtyAttributes;
using Objects;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Objects
{
	public class FloorHazard : IStepEvent, ICheckedInteractable<HandActivate>
	{
		[SerializeField] private AttackType attackType = AttackType.Melee;
		[SerializeField] private DamageType damageType = DamageType.Brute;
		[SerializeField, ShowIf("canCauseTrauma")] private TraumaticDamageTypes traumaType = TraumaticDamageTypes.NONE;
		[SerializeField] private float damageToGive = 5f;
		[SerializeField] private float armorPentration = 0f;
		[SerializeField, ShowIf("canCauseTrauma")] private float traumaChance = 0f;
		[SerializeField] private List<AddressableAudioSource> onStepSounds;
		[SerializeField] private bool hurtsOneFootOnly;
		[SerializeField] private bool ignoresFootwear;
		[SerializeField] private bool isTrap;
		[SerializeField] private bool canCauseTrauma;
		[SerializeField, ShowIf("isTrap")] private bool isArmed;
		[SerializeField, HideIf("ignoresFootwear")] private List<ItemTrait> protectiveItemTraits;
		[SerializeField] private List<BodyPartType> limbsToHurt;

		public override void OnStep(BaseEventData eventData)
		{
			LivingHealthMasterBase health = eventData.selectedObject.GetComponent<LivingHealthMasterBase>();
			HurtFeet(health); //Moving this to it's own function to keep things clean.
			if (isTrap && isArmed) isArmed = false;
			//Text and Audio feedback.
			Chat.AddActionMsgToChat(gameObject, $"You step on the {gameObject.ExpensiveName()}!",
				$"{health.playerScript.visibleName} steps on the {gameObject.ExpensiveName()}!");
			if(onStepSounds.Count == 0) return;
			SoundManager.PlayNetworkedAtPos(onStepSounds.PickRandom(), gameObject.AssumedWorldPosServer());
		}

		private void HurtFeet(LivingHealthMasterBase health)
		{
			void ApplyDamageToFeet(BodyPartType type)
			{
				health.ApplyDamageToBodyPart(gameObject, damageToGive, attackType, damageType, type, armorPentration, traumaChance, traumaType);
			}
			if (ignoresFootwear == false)
			{
				bool willHurt = false;
				foreach (var slot in health.playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.feet))
				{
					if (slot.IsEmpty == false)
					{
						//Check if the footwear we have on has any protective traits against the floor hazard.
						if (slot.ItemAttributes.GetTraits().Any(trait => protectiveItemTraits.Contains(trait)))
						{
							willHurt = true;//If we found out that there's a trait that protect this foot.
							break;
						}
					}
				}
				if (willHurt == true) return;
			}
			if (hurtsOneFootOnly)
			{
				ApplyDamageToFeet(limbsToHurt.PickRandom());
				return;
			}
			foreach (BodyPart limb in health.BodyPartList)
			{
				if (!limbsToHurt.Contains(limb.BodyPartType)) continue;
				ApplyDamageToFeet(limb.BodyPartType);
			}
		}

		public bool ArmTrap()
		{
			isArmed = !isArmed;
			return isArmed;
		}

		public override bool WillStep(BaseEventData eventData)
		{
			if (isTrap == true && isArmed == false) return false;
			if (eventData.selectedObject.gameObject.GetComponent<LivingHealthMasterBase>() != null) return true;
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

