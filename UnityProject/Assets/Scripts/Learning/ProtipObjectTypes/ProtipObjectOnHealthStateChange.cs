using System;
using System.Collections;
using HealthV2;
using Systems.Atmospherics;
using UnityEngine;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnHealthStateChange : ProtipObject
	{
		private HealthStateController healthState;

		[SerializeField] private ProtipSO BleedingVeryLowEvent;
		[SerializeField] private ProtipSO BleedingLowEvent;
		[SerializeField] private ProtipSO BleedingHighEvent;
		[SerializeField] private ProtipSO BleedingDanagerEvent;
		[SerializeField] private ProtipSO HungerEvent;
		[SerializeField] private ProtipSO StarvingEvent;
		[SerializeField] private ProtipSO FatsoEvent;
		[SerializeField] private ProtipSO CritHealthEvent;
		[SerializeField] private ProtipSO SoftHealthEvent;
		[SerializeField] private ProtipSO DeathlHealthEvent;
		[SerializeField] private ProtipSO FireStacksEvent;
		[SerializeField] private ProtipSO SuffuicationEvent;
		[SerializeField] private ProtipSO ToxinEvent;
		[SerializeField] private ProtipSO TemperatureColdEvent;
		[SerializeField] private ProtipSO TemperatureHotEvent;
		[SerializeField] private ProtipSO UnconsciousEvent;
		[SerializeField] private ProtipSO BarelyConsciousEvent;
		[SerializeField] private ProtipSO PressureHighEvent;
		[SerializeField] private ProtipSO PressureLowEvent;

		private bool PressureTipOnCooldown = false;
		private bool TempTipOnCooldown = false;
		private bool SufficationTipCooldown = false;
		private bool ToxinTipCooldown = false;
		private HungerState LastHungerState = HungerState.Normal;

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
			switch (state)
			{
				case ConsciousState.CONSCIOUS:
					break;
				case ConsciousState.BARELY_CONSCIOUS:
					TriggerTip(BarelyConsciousEvent);
					break;
				case ConsciousState.UNCONSCIOUS:
					TriggerTip(UnconsciousEvent);
					break;
				case ConsciousState.DEAD:
					TriggerTip(DeathlHealthEvent);
					break;
				default:
					break;
			}
		}

		private void TriggerPressureEvent(float state)
		{
			if(PressureTipOnCooldown) return;
			switch (state)
			{
				case <= AtmosConstants.HAZARD_LOW_PRESSURE:
					TriggerTip(PressureLowEvent);
					StartCoroutine(PressureCooldown());
					break;
				case >= AtmosConstants.WARNING_HIGH_PRESSURE:
					TriggerTip(PressureHighEvent);
					StartCoroutine(PressureCooldown());
					break;
			}
		}

		private void TriggerHungerTip(HungerState state)
		{
			if(LastHungerState == state) return;
			LastHungerState = state;
			switch (state)
			{
				case HungerState.Full:
					TriggerTip(FatsoEvent);
					break;
				case HungerState.Normal:
					break;
				case HungerState.Hungry:
					TriggerTip(HungerEvent);
					break;
				case HungerState.Malnourished:
					TriggerTip(HungerEvent);
					break;
				case HungerState.Starving:
					TriggerTip(StarvingEvent);
					break;
				default:
					break;
			}
		}

		private void TriggerBleedingTip(BleedingState state)
		{
			switch (state)
			{
				case BleedingState.None:
					break;
				case BleedingState.VeryLow:
					TriggerTip(BleedingVeryLowEvent);
					break;
				case BleedingState.Low:
					TriggerTip(BleedingLowEvent);
					break;
				case BleedingState.Medium:
					TriggerTip(BleedingHighEvent);
					break;
				case BleedingState.High:
					TriggerTip(BleedingHighEvent);
					break;
				case BleedingState.UhOh:
					TriggerTip(BleedingDanagerEvent);
					break;
				default:
					break;
			}
		}

		private void TriggerFireStacksTip(float state)
		{
			if (state > 1f)
			{
				TriggerTip(FireStacksEvent);
			}
		}

		private void TriggerSuffocatingTip(bool state)
		{
			if (state == false || SufficationTipCooldown) return;
			TriggerTip(SuffuicationEvent);
			StartCoroutine(SufficationCooldown());
		}

		private void TriggerToxinsTip(bool state)
		{
			if(state == false || ToxinTipCooldown) return;
			TriggerTip(ToxinEvent);
			StartCoroutine(ToxinCooldown());
		}

		private void TriggerTemperatureTip(float state)
		{
			if(TempTipOnCooldown) return;
			switch (state - Reactions.KOffsetC)
			{
				case <= AtmosConstants.CELSIUS_BARELY_COLD_HEAT:
					TriggerTip(TemperatureColdEvent);
					StartCoroutine(TempCooldown());
					break;
				case >= AtmosConstants.CELSIUS_HOT_HEAT:
					TriggerTip(TemperatureHotEvent);
					StartCoroutine(TempCooldown());
					break;
				default:
					break;
			}
		}

		private IEnumerator TempCooldown()
		{
			TempTipOnCooldown = true;
			yield return WaitFor.Seconds(PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM);
			TempTipOnCooldown = false;
		}

		private IEnumerator PressureCooldown()
		{
			PressureTipOnCooldown = true;
			yield return WaitFor.Seconds(PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM);
			PressureTipOnCooldown = false;
		}

		private IEnumerator SufficationCooldown()
		{
			SufficationTipCooldown = true;
			yield return WaitFor.Seconds(PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM);
			SufficationTipCooldown = false;
		}

		private IEnumerator ToxinCooldown()
		{
			ToxinTipCooldown = true;
			yield return WaitFor.Seconds(PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM);
			ToxinTipCooldown = false;
		}
	}
}