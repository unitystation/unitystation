using System;
using System.Collections;
using HealthV2;
using Systems.Atmospherics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnHealthStateChange : ProtipObject
	{
		private HealthStateController healthState;

		[FormerlySerializedAs("BleedingVeryLowEvent")] [SerializeField]
		private ProtipSO bleedingVeryLowEvent;
		[FormerlySerializedAs("BleedingLowEvent")] [SerializeField]
		private ProtipSO bleedingLowEvent;
		[FormerlySerializedAs("BleedingHighEvent")] [SerializeField]
		private ProtipSO bleedingHighEvent;
		[FormerlySerializedAs("BleedingDanagerEvent")] [SerializeField]
		private ProtipSO bleedingDanagerEvent;
		[FormerlySerializedAs("HungerEvent")] [SerializeField]
		private ProtipSO hungerEvent;
		[FormerlySerializedAs("StarvingEvent")] [SerializeField]
		private ProtipSO starvingEvent;
		[FormerlySerializedAs("FatsoEvent")] [SerializeField]
		private ProtipSO fatsoEvent;
		[FormerlySerializedAs("CritHealthEvent")] [SerializeField]
		private ProtipSO critHealthEvent;
		[FormerlySerializedAs("SoftHealthEvent")] [SerializeField]
		private ProtipSO softHealthEvent;
		[FormerlySerializedAs("DeathlHealthEvent")] [SerializeField]
		private ProtipSO deathlHealthEvent;
		[FormerlySerializedAs("FireStacksEvent")] [SerializeField]
		private ProtipSO fireStacksEvent;
		[FormerlySerializedAs("SuffuicationEvent")] [SerializeField]
		private ProtipSO suffuicationEvent;
		[FormerlySerializedAs("ToxinEvent")] [SerializeField]
		private ProtipSO toxinEvent;
		[FormerlySerializedAs("TemperatureColdEvent")] [SerializeField]
		private ProtipSO temperatureColdEvent;
		[FormerlySerializedAs("TemperatureHotEvent")] [SerializeField]
		private ProtipSO temperatureHotEvent;
		[FormerlySerializedAs("UnconsciousEvent")] [SerializeField]
		private ProtipSO unconsciousEvent;
		[FormerlySerializedAs("BarelyConsciousEvent")] [SerializeField]
		private ProtipSO barelyConsciousEvent;
		[FormerlySerializedAs("PressureHighEvent")] [SerializeField]
		private ProtipSO pressureHighEvent;
		[FormerlySerializedAs("PressureLowEvent")] [SerializeField]
		private ProtipSO pressureLowEvent;

		private bool pressureTipOnCooldown = false;
		private bool tempTipOnCooldown = false;
		private bool sufficationTipCooldown = false;
		private bool toxinTipCooldown = false;
		private bool hungerTipCooldown = false;
		private bool deathTipCooldown = false;
		private bool unconciousTipCooldown = false;
		private bool fireStackTipCooldown = false;
		private bool bleedingTipCooldown = false;
		private HungerState lastHungerState = HungerState.Normal;

		private const float PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM = 35f;

		private void Awake()
		{
			healthState = GetComponent<HealthStateController>();
			if (healthState != null) return;
			Logger.LogError($"[Protips/HealthStateChangeTip/{gameObject} - Missing Health State controller.]");
			Destroy(this);
		}

		private void OnEnable()
		{
			if(CustomNetworkManager.IsHeadless) return;
			healthState.BleedingEvent += TriggerBleedingTip;
			healthState.HungerEvent += TriggerHungerTip;
			healthState.FireStacksEvent += TriggerFireStacksTip;
			healthState.SuffuicationEvent += TriggerSuffocatingTip;
			healthState.ToxinEvent += TriggerToxinsTip;
			healthState.TemperatureEvent += TriggerTemperatureTip;
			healthState.ConsciousEvent += TriggerConsciousEvent;
			healthState.PressureEvent += TriggerPressureEvent;
		}

		private void OnDisable()
		{
			if(CustomNetworkManager.IsHeadless) return;
			healthState.BleedingEvent -= TriggerBleedingTip;
			healthState.HungerEvent -= TriggerHungerTip;
			healthState.FireStacksEvent -= TriggerFireStacksTip;
			healthState.SuffuicationEvent -= TriggerSuffocatingTip;
			healthState.ToxinEvent -= TriggerToxinsTip;
			healthState.TemperatureEvent -= TriggerTemperatureTip;
			healthState.ConsciousEvent -= TriggerConsciousEvent;
			healthState.PressureEvent -= TriggerPressureEvent;
		}

		private void TriggerConsciousEvent(ConsciousState state)
		{
			if (unconciousTipCooldown || deathTipCooldown || IsPlayerGhostInBody()) return;
			switch (state)
			{
				case ConsciousState.BARELY_CONSCIOUS:
					TriggerTip(barelyConsciousEvent);
					StartCoroutine(AsleepTipCooldown());
					break;
				case ConsciousState.UNCONSCIOUS:
					TriggerTip(unconsciousEvent);
					StartCoroutine(AsleepTipCooldown());
					break;
				case ConsciousState.DEAD:
					TriggerTip(deathlHealthEvent);
					StartCoroutine(DeathTipCooldown());
					break;
				default:
					break;
			}
		}

		private void TriggerPressureEvent(float state)
		{
			if(pressureTipOnCooldown || IsPlayerGhostInBody()) return;
			switch (state)
			{
				case <= AtmosConstants.HAZARD_LOW_PRESSURE:
					TriggerTip(pressureLowEvent);
					StartCoroutine(PressureCooldown());
					break;
				case >= AtmosConstants.WARNING_HIGH_PRESSURE:
					TriggerTip(pressureHighEvent);
					StartCoroutine(PressureCooldown());
					break;
			}
		}

		private void TriggerHungerTip(HungerState state)
		{
			if(lastHungerState == state || IsPlayerGhostInBody()) return;
			lastHungerState = state;
			switch (state)
			{
				case HungerState.Full:
					TriggerTip(fatsoEvent);
					break;
				case HungerState.Hungry:
					TriggerTip(hungerEvent);
					break;
				case HungerState.Malnourished:
					TriggerTip(hungerEvent);
					break;
				case HungerState.Starving:
					TriggerTip(starvingEvent);
					break;
				default:
					break;
			}

			if(lastHungerState != HungerState.Normal) StartCoroutine(HungerCooldown());
		}

		private void TriggerBleedingTip(BleedingState state)
		{
			if(bleedingTipCooldown || IsPlayerGhostInBody()) return;
			switch (state)
			{
				case BleedingState.VeryLow:
					TriggerTip(bleedingVeryLowEvent);
					break;
				case BleedingState.Low:
					TriggerTip(bleedingLowEvent);
					break;
				case BleedingState.Medium:
					TriggerTip(bleedingHighEvent);
					break;
				case BleedingState.High:
					TriggerTip(bleedingHighEvent);
					break;
				case BleedingState.UhOh:
					TriggerTip(bleedingDanagerEvent);
					break;
				default:
					break;
			}
			StartCoroutine(BleedingCooldown());
		}

		private void TriggerFireStacksTip(float state)
		{
			if(fireStackTipCooldown || IsPlayerGhostInBody()) return;
			if (state > 1f)
			{
				TriggerTip(fireStacksEvent);
				StartCoroutine(FireStackTipCooldown());
			}
		}

		private void TriggerSuffocatingTip(bool state)
		{
			if (state == false || sufficationTipCooldown || IsPlayerGhostInBody()) return;
			TriggerTip(suffuicationEvent);
			StartCoroutine(SufficationCooldown());
		}

		private void TriggerToxinsTip(bool state)
		{
			if(state == false || toxinTipCooldown || IsPlayerGhostInBody()) return;
			TriggerTip(toxinEvent);
			StartCoroutine(ToxinCooldown());
		}

		private void TriggerTemperatureTip(float state)
		{
			if(tempTipOnCooldown || IsPlayerGhostInBody()) return;
			switch (state)
			{
				case <= AtmosConstants.BARELY_COLD_HEAT:
					TriggerTip(temperatureColdEvent);
					StartCoroutine(TempCooldown());
					break;
				case >= AtmosConstants.HOT_HEAT:
					TriggerTip(temperatureHotEvent);
					StartCoroutine(TempCooldown());
					break;
				default:
					break;
			}
		}

		private bool IsPlayerGhostInBody()
		{
			return healthState.livingHealthMasterBase.playerScript.IsGhost;
		}

		private IEnumerator TempCooldown()
		{
			tempTipOnCooldown = true;
			yield return WaitFor.Seconds(PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM);
			tempTipOnCooldown = false;
		}

		private IEnumerator PressureCooldown()
		{
			pressureTipOnCooldown = true;
			yield return WaitFor.Seconds(PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM);
			pressureTipOnCooldown = false;
		}

		private IEnumerator SufficationCooldown()
		{
			sufficationTipCooldown = true;
			yield return WaitFor.Seconds(PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM);
			sufficationTipCooldown = false;
		}

		private IEnumerator ToxinCooldown()
		{
			toxinTipCooldown = true;
			yield return WaitFor.Seconds(PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM);
			toxinTipCooldown = false;
		}

		private IEnumerator HungerCooldown()
		{
			hungerTipCooldown = true;
			yield return WaitFor.Seconds(PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM);
			hungerTipCooldown = false;
		}

		private IEnumerator DeathTipCooldown()
		{
			deathTipCooldown = true;
			yield return WaitFor.Seconds(PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM);
			deathTipCooldown = false;
		}

		private IEnumerator AsleepTipCooldown()
		{
			unconciousTipCooldown = true;
			yield return WaitFor.Seconds(PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM);
			unconciousTipCooldown = false;
		}

		private IEnumerator FireStackTipCooldown()
		{
			fireStackTipCooldown = true;
			yield return WaitFor.Seconds(PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM);
			fireStackTipCooldown = false;
		}

		private IEnumerator BleedingCooldown()
		{
			bleedingTipCooldown = true;
			yield return WaitFor.Seconds(PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM);
			bleedingTipCooldown = false;
		}
	}
}