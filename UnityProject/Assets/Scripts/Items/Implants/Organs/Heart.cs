using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;

namespace Items.Implants.Organs
{
	public class Heart : BodyPartFunctionality
	{
		public bool HeartAttack = false;

		public bool CanTriggerHeartAttack = true;

		public int heartAttackThreshold = -80;

		public int SecondsOfRevivePulse = 30;

		public int CurrentPulse = 0;

		[SerializeField] private Reagent salt;

		[SerializeField] private float dangerSaltLevel = 20f; //in u

		public HungerComponent HungerComponent;

		public ReagentCirculatedComponent _ReagentCirculatedComponent;

		public override void Awake()
		{
			base.Awake();
			HungerComponent = this.GetComponentCustom<HungerComponent>();
			_ReagentCirculatedComponent = this.GetComponentCustom<ReagentCirculatedComponent>();
		}

		public override void EmpResult(int strength)
		{
			if (DMMath.Prob(50)) DoHeartAttack();

			base.EmpResult(strength);
		}

		public override void ImplantPeriodicUpdate()
		{
			base.ImplantPeriodicUpdate();
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

			DoHeartBeat();
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			_ReagentCirculatedComponent.AssociatedSystem.PumpingDevices.Remove(this);
		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			_ReagentCirculatedComponent.AssociatedSystem.PumpingDevices.Add(this);
		}

		public void DoHeartBeat()
		{
			//If we actually have a circulatory system.
			if (HeartAttack)
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
			if (_ReagentCirculatedComponent.AssociatedSystem.BloodPool.MajorMixReagent == salt ||
			    _ReagentCirculatedComponent.AssociatedSystem.BloodPool[salt] > dangerSaltLevel)
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
			if (HeartAttack || RelatedPart.HealthMaster.brain == null) //Needs a brain for heart to work
			{
				return 0;
			}

			//To exclude stuff like hunger and oxygen damage
			var TotalModified = 1f;
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

			return TotalModified;
		}

		public void DoHeartAttack()
		{
			HeartAttack = true;
			RelatedPart.HealthMaster.Death();
		}
	}
}