using System;
using System.Collections.Generic;
using UnityEngine;
using Chemistry;

namespace HealthV2
{
	public class Heart : BodyPartFunctionality
	{
		public bool HeartAttack = false;

		public bool CanTriggerHeartAttack = true;

		public int SecondsOfRevivePulse = 30;

		public int CurrentPulse = 0;

		private bool alarmedForInternalBleeding = false;

		[SerializeField] private Reagent salt;

		[SerializeField] private float dangerSaltLevel = 5f; //in %

		public override void ImplantPeriodicUpdate()
		{
			base.ImplantPeriodicUpdate();
			if (RelatedPart.HealthMaster.OverallHealth <= -100)
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
			else if (RelatedPart.HealthMaster.OverallHealth < -50)
			{
				CanTriggerHeartAttack = true;
				CurrentPulse = 0;
			}

			DoHeartBeat();
		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			livingHealth.CirculatorySystem.Hearts.Remove(this);
		}

		public override void AddedToBody(LivingHealthMasterBase livingHealth)
		{
			livingHealth.CirculatorySystem.Hearts.Add(this);
		}

		public override void InternalDamageLogic()
		{
			base.InternalDamageLogic();
			if (RelatedPart.CurrentInternalBleedingDamage > 50 && alarmedForInternalBleeding == false)
			{
				Chat.AddActionMsgToChat(RelatedPart.HealthMaster.gameObject,
					$"You feel a sharp pain in your {RelatedPart.gameObject.ExpensiveName()}!",
					$"{RelatedPart.HealthMaster.playerScript.visibleName} holds their {RelatedPart.gameObject.ExpensiveName()} in pain!");
				alarmedForInternalBleeding = true;
			}

			if (RelatedPart.CurrentInternalBleedingDamage > RelatedPart.MaximumInternalBleedDamage)
			{
				DoHeartAttack();
			}
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
					alarmedForInternalBleeding = false;
				}

				return;
			}

			if (RelatedPart.HealthMaster.IsDead)
				return; //For some reason the heart will randomly still continue to try and beat after death.
			if (RelatedPart.HealthMaster.CirculatorySystem.BloodPool.MajorMixReagent == salt ||
			    RelatedPart.HealthMaster.CirculatorySystem.BloodPool[salt] * 100 > dangerSaltLevel)
			{
				Chat.AddActionMsgToChat(RelatedPart.HealthMaster.gameObject,
					"<color=red>Your body spasms as a jolt of pain surges all over your body then into your heart!</color>",
					$"<color=red>{RelatedPart.HealthMaster.playerScript.visibleName} spasms before holding " +
					$"{RelatedPart.HealthMaster.playerScript.characterSettings.TheirPronoun(RelatedPart.HealthMaster.playerScript)} chest in shock before falling to the ground!</color>");
				RelatedPart.HealthMaster.Death();
			}
		}


		public float CalculateHeartbeat()
		{
			if (HeartAttack)
			{
				return 0;
			}

			//To exclude stuff like hunger and oxygen damage
			var TotalModified = 1f;
			foreach (var modifier in bodyPart.AppliedModifiers)
			{
				var toMultiply = 1f;
				if (modifier == bodyPart.DamageModifier)
				{
					toMultiply = Mathf.Max(0f,
						Mathf.Max(bodyPart.MaxHealth - bodyPart.TotalDamageWithoutOxyCloneRadStam, 0) / bodyPart.MaxHealth);
				}
				else if (modifier == bodyPart.HungerModifier)
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

		public void Heartbeat(float efficiency)
		{
			if (efficiency > 1)
			{
				efficiency = 1;
			}

			CirculatorySystemBase circulatorySystem = RelatedPart.HealthMaster.CirculatorySystem;
			if (circulatorySystem)
			{
				float totalWantedBlood = 0;
				foreach (BodyPart implant in RelatedPart.HealthMaster.BodyPartList)
				{
					if (implant.IsBloodCirculated == false) continue;
					totalWantedBlood += implant.BloodThroughput;
				}

				float pumpedReagent = Math.Min(totalWantedBlood * efficiency, circulatorySystem.BloodPool.Total);

				foreach (BodyPart implant in RelatedPart.HealthMaster.BodyPartList)
				{
					if (implant.IsBloodCirculated == false) continue;
					implant.BloodPumpedEvent((implant.BloodThroughput / totalWantedBlood) * pumpedReagent);
				}


			}
		}

		public void DoHeartAttack()
		{
			HeartAttack = true;
		}
	}
}