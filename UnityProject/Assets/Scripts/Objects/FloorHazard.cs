using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Systems.MobAIs;
using AddressableReferences;
using HealthV2;
using HealthV2.Limbs;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Objects
{
	public class FloorHazard : EnterTileBase, IServerInventoryMove
	{
		[SerializeField] private AttackType attackType = AttackType.Melee;
		[SerializeField] private DamageType damageType = DamageType.Brute;
		[SerializeField, ShowIf(nameof(canCauseTrauma))] private TraumaticDamageTypes traumaType = TraumaticDamageTypes.NONE;
		[SerializeField] protected float damageToGive = 5f;
		[SerializeField] protected float armorPentration = 0f;
		[SerializeField, ShowIf(nameof(canCauseTrauma))] protected float traumaChance = 0f;
		[SerializeField] protected List<AddressableAudioSource> onStepSounds;
		[SerializeField] protected bool hurtsOneFootOnly;
		[SerializeField] protected bool ignoresFootwear;
		[SerializeField] private bool canCauseTrauma;
		[SerializeField, HideIf("ignoresFootwear")] private List<ItemTrait> protectiveItemTraits;
		[SerializeField] private List<BodyPartType> limbsToHurt;

		public override bool WillAffectPlayer(PlayerScript playerScript)
		{
			return playerScript.IsGhost == false;
		}

		public override void OnPlayerStep(PlayerScript playerScript)
		{
			if(objectPhysics.registerTile.LocalPosition == TransformState.HiddenPos) return;
			var health = playerScript.playerHealth;

			HurtFeet(health); //Moving this to it's own function to keep things clean.
			//Text and Audio feedback.
			Chat.AddActionMsgToChat(gameObject, $"You step on the {gameObject.ExpensiveName()}!",
				$"{health.playerScript.visibleName} steps on the {gameObject.ExpensiveName()}!");
			PlayStepAudio();
		}

		public override bool WillAffectObject(GameObject eventData)
		{
			//Old health
			return eventData.HasComponent<LivingHealthBehaviour>();
		}

		public override void OnObjectEnter(GameObject eventData)
		{
			//Old health
			eventData.GetComponent<LivingHealthBehaviour>().ApplyDamageToBodyPart(
				gameObject, damageToGive, attackType, damageType);
		}

		protected void HurtFeet(LivingHealthMasterBase health)
		{
			if (health.OrNull()?.playerScript.OrNull()?.DynamicItemStorage == null) return;

			if (ignoresFootwear == false)
			{
				foreach (var slot in health.playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.feet))
				{
					if (slot.IsEmpty == false)
					{
						//Check if the footwear we have on has any protective traits against the floor hazard.
						if (slot.ItemAttributes.GetTraits().Any(trait => protectiveItemTraits.Contains(trait)))
						{
							return;
						}
					}
				}

				foreach (var BodyPart in health.SurfaceBodyParts)
				{
					var leg = BodyPart.CommonComponents.SafeGetComponent<HumanoidLeg>();
					if (leg != null)
					{
						//Check if the leg we have on has any protective traits against the floor hazard.
						if (BodyPart.CommonComponents.ItemAttributes.GetTraits().Any(trait => protectiveItemTraits.Contains(trait)))
						{
							return;
						}
					}
				}
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

		protected void PlayStepAudio()
		{
			if(onStepSounds.Count == 0) return;
			SoundManager.PlayNetworkedAtPos(onStepSounds.PickRandom(), gameObject.AssumedWorldPosServer());
		}

		protected void ApplyDamageToPartyType(LivingHealthMasterBase health, BodyPartType type)
		{
			health.ApplyDamageToBodyPart(gameObject, damageToGive, attackType, damageType, type, armorPentration, traumaChance, traumaType);
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (this.gameObject != info.MovedObject.gameObject) return;
			OnLocalPositionChangedServer(info.FromPlayer != null ? info.FromPlayer.LocalPosition : TransformState.HiddenPos,
				objectPhysics.registerTile.LocalPosition);
		}
	}
}

