using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using HealthV2;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Objects
{
	public class FloorHazard : StepEvent
	{
		[SerializeField] private AttackType attackType = AttackType.Melee;
		[SerializeField] private DamageType damageType = DamageType.Brute;
		[SerializeField, ShowIf("canCauseTrauma")] private TraumaticDamageTypes traumaType = TraumaticDamageTypes.NONE;
		[SerializeField] protected float damageToGive = 5f;
		[SerializeField] protected float armorPentration = 0f;
		[SerializeField, ShowIf("canCauseTrauma")] protected float traumaChance = 0f;
		[SerializeField] protected List<AddressableAudioSource> onStepSounds;
		[SerializeField] protected bool hurtsOneFootOnly;
		[SerializeField] protected bool ignoresFootwear;
		[SerializeField] protected bool ignoresHandwear;
		[SerializeField] private bool canCauseTrauma;
		[SerializeField, HideIf("ignoresFootwear")] private List<ItemTrait> protectiveItemTraits;
		[SerializeField] private List<BodyPartType> limbsToHurt;

		public override void OnStep(BaseEventData eventData)
		{
			LivingHealthMasterBase health = eventData.selectedObject.GetComponent<LivingHealthMasterBase>();
			HurtFeet(health); //Moving this to it's own function to keep things clean.
			//Text and Audio feedback.
			Chat.AddActionMsgToChat(gameObject, $"You step on the {gameObject.ExpensiveName()}!",
				$"{health.playerScript.visibleName} steps on the {gameObject.ExpensiveName()}!");
			PlayAudio();
		}

		protected void HurtFeet(LivingHealthMasterBase health)
		{
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
				ApplyDamageToPartyType(health, limbsToHurt.PickRandom());
				return;
			}
			foreach (BodyPart limb in health.BodyPartList)
			{
				if (!limbsToHurt.Contains(limb.BodyPartType)) continue;
				ApplyDamageToPartyType(health, limb.BodyPartType);
			}
		}

		protected void PlayAudio()
		{
			if(onStepSounds.Count == 0) return;
			SoundManager.PlayNetworkedAtPos(onStepSounds.PickRandom(), gameObject.AssumedWorldPosServer());
		}

		protected void ApplyDamageToPartyType(LivingHealthMasterBase health, BodyPartType type)
		{
			health.ApplyDamageToBodyPart(gameObject, damageToGive, attackType, damageType, type, armorPentration, traumaChance, traumaType);
		}

		public override bool WillStep(BaseEventData eventData)
		{
			if (eventData.selectedObject.gameObject.GetComponent<LivingHealthMasterBase>() != null) return true;
			return false;
		}
	}
}

