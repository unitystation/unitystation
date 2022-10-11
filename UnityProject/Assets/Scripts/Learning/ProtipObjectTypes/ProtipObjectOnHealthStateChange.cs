using System;
using HealthV2;
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

		private void Awake()
		{
			healthState = GetComponent<HealthStateController>();
			if (healthState != null) return;
			Logger.LogError($"[Protips/HealthStateChangeTip/{gameObject} - Missing Health State controller.]");
			Destroy(this);
		}

		private void OnEnable()
		{
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
			switch (state)
			{
				case >= AtmosConstants.MINIMUM_OXYGEN_PRESSURE:
					TriggerTip(PressureLowEvent);
					break;
				case <= AtmosConstants.WARNING_HIGH_PRESSURE:
					TriggerTip(PressureHighEvent);
					break;
			}
		}

		private void TriggerHungerTip(HungerState state)
		{
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
			if(state) TriggerTip(SuffuicationEvent);
		}

		private void TriggerToxinsTip(bool state)
		{
			if(state) TriggerTip(ToxinEvent);
		}

		private void TriggerTemperatureTip(float state)
		{
			switch (state)
			{
				case >= AtmosConstants.COLD_HEAT:
					TriggerTip(TemperatureColdEvent);
					break;
				case <= AtmosConstants.HOT_HEAT:
					TriggerTip(TemperatureHotEvent);
					break;
			}
		}
	}
}