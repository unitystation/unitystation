using System;
using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Items.Implants.Organs
{
	[RequireComponent(typeof(ReagentCirculatedComponent))]
	public class Heart : BodyPartFunctionality
	{
		public bool CanHaveHeartAttack = true;

		[FormerlySerializedAs("EfficiencyLinksToDamage")] public bool EfficiencyLinksToModifiers = true;

		public bool HeartAttack = false;

		[NonSerialized]
		public bool CanTriggerHeartAttack = true;

		public int heartAttackThreshold = -80;

		public int SecondsOfRevivePulse = 30;

		public int CurrentPulse = 0;

		[SerializeField] private Reagent salt;

		[SerializeField] private float dangerSaltLevel = 20f; //in u

		public HungerComponent HungerComponent;

		public ReagentCirculatedComponent _ReagentCirculatedComponent;

		public ReagentPoolSystem CashedReagentPoolSystem = null;

		public int ForcedBeats = 0;

		public bool isEMPVunerable = false;

		[ShowIf("isEMPVunerable")]
		public int EMPResistance = 2;

		public override void Awake()
		{
			base.Awake();
			HungerComponent = this.GetComponentCustom<HungerComponent>();
			_ReagentCirculatedComponent = this.GetComponentCustom<ReagentCirculatedComponent>();
		}

		public override void OnEmp(int strength)
		{
			if (isEMPVunerable == false) return;

			if (EMPResistance == 0 || DMMath.Prob(100 / EMPResistance))
			{
				if (DMMath.Prob(50)) DoHeartAttack();
			}
		}

		public override void ImplantPeriodicUpdate()
		{
			base.ImplantPeriodicUpdate();
			if (CanHaveHeartAttack)
			{
				if (RelatedPart.HealthMaster.OverallHealth <= heartAttackThreshold)
				{
					if (CanTriggerHeartAttack)
					{
						DoHeartAttack();
						CanTriggerHeartAttack = false;
						CurrentPulse = 0;
						return;
					}

					if (HeartAttack == false)
					{
						CurrentPulse++;
						if (SecondsOfRevivePulse < CurrentPulse)
						{
							DoHeartAttack();
						}
					}
				}
				else if (RelatedPart.HealthMaster.OverallHealth < heartAttackThreshold/2)
				{
					CanTriggerHeartAttack = true;
					CurrentPulse = 0;
				}
			}


			DoHeartBeat();
		}



		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			CashedReagentPoolSystem.PumpingDevices.Remove(this);
			CashedReagentPoolSystem = null;
		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			CashedReagentPoolSystem = _ReagentCirculatedComponent.AssociatedSystem;
			CashedReagentPoolSystem.PumpingDevices.Add(this);
		}

		public void DoHeartBeat()
		{
			//If we actually have a circulatory system.
			if (CanHaveHeartAttack && HeartAttack)
			{
				if (SecondsOfRevivePulse < CurrentPulse) return;
				if (DMMath.Prob(0.1))
				{
					HeartAttack = false;
				}

				return;
			}

			if (RelatedPart.HealthMaster.IsDead)
				return; //For some reason the heart will randomly still continue to try and beat after death.
			if (salt != null && (_ReagentCirculatedComponent.AssociatedSystem.BloodPool.MajorMixReagent == salt ||
			    _ReagentCirculatedComponent.AssociatedSystem.BloodPool[salt] > dangerSaltLevel))
			{
				Chat.AddActionMsgToChat(RelatedPart.HealthMaster.gameObject,
					"<color=red>Your body spasms as a jolt of pain surges all over your body then into your heart!</color>",
					$"<color=red>{RelatedPart.HealthMaster.playerScript.visibleName} spasms before holding " +
					$"{RelatedPart.HealthMaster.playerScript.characterSettings.TheirPronoun(RelatedPart.HealthMaster.playerScript)} chest in shock before falling to the ground!</color>");
				DoHeartAttack();
			}
		}


		public float CalculateHeartbeat()
		{
			if (ForcedBeats > 0)
			{
				ForcedBeats--;
				return 1;
			}

			if ((CanHaveHeartAttack && HeartAttack) || RelatedPart.HealthMaster.brain == null) //Needs a brain for heart to work
			{
				return 0;
			}

			//To exclude stuff like hunger and oxygen damage
			var TotalModified = 1f;
			if (EfficiencyLinksToModifiers)
			{
				foreach (var modifier in RelatedPart.AppliedModifiers)
				{
					var toMultiply = 1f;
					if (modifier == RelatedPart.DamageModifier)
					{
						toMultiply = Mathf.Max(0f,
							Mathf.Max(RelatedPart.MaxHealth - RelatedPart.TotalDamageWithoutOxyCloneRadStam, 0) / RelatedPart.MaxHealth);
					}
					else if (modifier == HungerComponent.OrNull()?.HungerModifier)
					{
						continue;
					}
					else
					{
						toMultiply = Mathf.Max(0f, modifier.Multiplier);
					}

					TotalModified *= toMultiply;
				}

			}

			return TotalModified;
		}

		public void DoHeartAttack()
		{
			if (CanHaveHeartAttack)
			{
				HeartAttack = true;
				RelatedPart.HealthMaster.Death();
			}
		}
	}
}