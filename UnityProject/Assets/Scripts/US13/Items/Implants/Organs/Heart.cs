using System;
using Chemistry;
using Logs;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using US13.Core.Chat;
using US13.HealthV2.Living;
using US13.HealthV2.Living.PolymorphicSystems;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;
using US13.Managers.UpdateManager;
using Util;

namespace US13.Items.Implants.Organs
{

	[System.Serializable]
	public class Heart : BodyPartBehavior
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

		[UnityEngine.SerializeReference] public HungerComponent HungerComponent;

		[UnityEngine.SerializeReference] public ReagentCirculatedComponent _ReagentCirculatedComponent;

		public ReagentPoolSystem CashedReagentPoolSystem = null;

		public int ForcedBeats = 0;

		public bool isEMPVunerable = false;

		[ShowIf("isEMPVunerable")]
		public int EMPResistance = 2;

		public int BeatFrameCash = 0;
		public float BeatStrength = 0;
		public override void OnInitialize()
		{
			base.OnInitialize();
			HungerComponent = GetBodyPartBehavior<HungerComponent>();
			_ReagentCirculatedComponent = GetBodyPartBehavior<ReagentCirculatedComponent>();
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
			if (CashedReagentPoolSystem == null || RelatedPart.HealthMaster == null) return;
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



		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			CashedReagentPoolSystem?.PumpingDevices.Remove(this);
			CashedReagentPoolSystem = null;
			BeatFrameCash = -1;
			BeatStrength = 0;
		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			var system = _ReagentCirculatedComponent?.AssociatedSystem;
			if (CashedReagentPoolSystem != system) CashedReagentPoolSystem?.PumpingDevices.Remove(this);
			CashedReagentPoolSystem = system;
			BeatFrameCash = -1;
			if (system == null)
			{
				Loggy.Error($"Heart on {name} requires an enabled ReagentCirculatedComponent bound to a blood system.");
				return;
			}
			if (!system.PumpingDevices.Contains(this)) system.PumpingDevices.Add(this);
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
			if (!Enabled || CashedReagentPoolSystem == null || RelatedPart.HealthMaster == null)
			{
				BeatStrength = 0;
				return 0;
			}
			if (BeatFrameCash == UpdateManager.CurrentFrameCashed)
			{
				return BeatStrength;
			}

			BeatFrameCash = UpdateManager.CurrentFrameCashed;
			if (ForcedBeats > 0)
			{
				ForcedBeats--;
				BeatStrength = 1;
				return BeatStrength;
			}

			if ((CanHaveHeartAttack && HeartAttack) || RelatedPart.HealthMaster.brain == null) //Needs a brain for heart to work
			{
				BeatStrength = 0;
				return BeatStrength;
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
							Mathf.Max((float)(RelatedPart.MaxHealth - RelatedPart.TotalDamageWithoutOxyCloneRadStam), 0) / RelatedPart.MaxHealth);
					}
					else if (modifier == HungerComponent?.HungerModifier)
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

			BeatStrength = TotalModified;
			return BeatStrength;
		}

		public void DoHeartAttack()
		{
			if (CanHaveHeartAttack == false) return;
			HeartAttack = true;
			RelatedPart.HealthMaster.Death();
		}
	}
}
