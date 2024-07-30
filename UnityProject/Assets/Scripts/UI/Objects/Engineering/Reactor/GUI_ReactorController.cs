using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Engineering;
using Objects.Engineering.Reactor;

namespace UI.Objects.Engineering
{
	public class GUI_ReactorController : NetTab
	{
		public ReactorControlConsole ReactorControlConsole;

		public GUI_ReactorLayout GUIReactorLayout = new GUI_ReactorLayout();
		public GUI_ReactorAnnunciator GUIReactorAnnunciator = new GUI_ReactorAnnunciator();

		[SerializeField] private NetSliderDial ReactorCoreTemperature = null;
		[SerializeField] private NetSliderDial CorePressure = null;
		[SerializeField] private NetSliderDial CoreWaterLevel = null;

		[SerializeField] private NetSliderDial CoreFluxLevel = null;
		[SerializeField] private NetSliderDial CoreKValue1_00 = null;
		[SerializeField] private NetSliderDial CoreKValue0_10 = null;
		[SerializeField] private NetSliderDial CoreKValue0_01 = null;
		[SerializeField] private NetText_label RodDepth = null;

		private decimal previousRadlevel = 0;
		private decimal percentageChange = 0;
		private float MainSetControl = 1;
		private float SecondarySetControl = 1;

		private const decimal HIGH_PRESSURE_THRESHOLD = 60000;
		private const decimal HIGH_PRESSURE_THRESHOLD_SCRAM = 10000;
		private const float HIGH_PRESSURE_DELTA_THRESHOLD = 2500;

		private const int MAX_NEUTRON_FLUX_POWER = 12;

		#region Lifecycle

		private void Start()
		{
			if (Provider != null)
			{
				ReactorControlConsole = Provider.GetComponentInChildren<ReactorControlConsole>();
			}

			GUIReactorLayout.GUI_ReactorController = this;
			GUIReactorAnnunciator.GUI_ReactorController = this;
			GUIReactorLayout.Start();

			RefreshGui();
		}

		public override void OnEnable()
		{
			base.OnEnable();
			if (CustomNetworkManager.Instance._isServer == false) return;
			UpdateManager.Add(RefreshGui, 1);
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.Instance._isServer == false) return;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RefreshGui);
		}

		#endregion

		public void RefreshGui()
		{
			if (IsMasterTab == false) return;
			if (ReactorControlConsole == null || ReactorControlConsole.ReactorChambers == null) return;

			RefreshCoreSliders();
			RefreshKSliders();

			previousRadlevel = ReactorControlConsole.ReactorChambers.PresentNeutrons; //To determine K value sliders

			RodDepth.MasterSetValue($"{ReactorControlConsole.ReactorChambers.ControlRodDepthPercentage * 100}%");

			CoreFluxLevel.MasterSetValue(GetNeutronFluxSliderValue((float)previousRadlevel).ToString());

			GUIReactorLayout.Refresh();
			GUIReactorAnnunciator.Refresh();
		}

		private void RefreshCoreSliders()
		{
			float temp = ReactorControlConsole.ReactorChambers.Temperature;
			float tempSliderPercent = Mathf.Clamp(Mathf.Round(temp / ReactorGraphiteChamber.MAX_CORE_TEMPERATURE * 100), 0, 100);

			ReactorCoreTemperature.MasterSetValue(tempSliderPercent.ToString());

			decimal pressure = ReactorControlConsole.ReactorChambers.CurrentPressure;
			float pressureSliderPercent = (float)Math.Clamp(Math.Round(pressure / ReactorGraphiteChamber.MAX_CORE_PRESSURE * 100), 0, 100);

			CorePressure.MasterSetValue(pressureSliderPercent.ToString());

			float waterLevel = ReactorControlConsole.ReactorChambers.ReactorPipe.pipeData.mixAndVolume.Total.x;
			float waterLevelSliderPercent = Mathf.Clamp(Mathf.Round(waterLevel / ReactorGraphiteChamber.MAX_WATER_LEVEL * 100), 0, 100);

			CoreWaterLevel.MasterSetValue(waterLevelSliderPercent.ToString());
		}

		private void RefreshKSliders()
		{
			if (previousRadlevel == 0 || ReactorControlConsole.ReactorChambers.PresentNeutrons == 0) return;

			percentageChange = ReactorControlConsole.ReactorChambers.PresentNeutrons / previousRadlevel * 100;
			percentageChange = percentageChange - 100;
			var ValuePercentageChange1_00 = Mathf.Clamp((float) (50 + (percentageChange * 5)), 0, 100);
			var ValuePercentageChange0_10 = Mathf.Clamp((float) (50 + (percentageChange * 50)), 0, 100);
			var ValuePercentageChange0_01 = Mathf.Clamp((float) (50 + (percentageChange * 500)), 0, 100);

			CoreKValue1_00.MasterSetValue(Math.Round(ValuePercentageChange1_00).ToString());
			CoreKValue0_10.MasterSetValue(Math.Round(ValuePercentageChange0_10).ToString());
			CoreKValue0_01.MasterSetValue(Math.Round(ValuePercentageChange0_01).ToString());
		}

		public float GetNeutronFluxSliderValue(float neutronFlux)
		{
			if (neutronFlux < float.Epsilon && neutronFlux > -float.Epsilon) return 0;

			float power = Mathf.Log10(neutronFlux);
			float percent = Mathf.Clamp(Mathf.Clamp(power, 0, 100) / MAX_NEUTRON_FLUX_POWER * 100, 0, 100);

			return Mathf.Round(percent);
		}

		public void MainSetControlDepth(float Depth)
		{
			MainSetControl = Depth;
			SetControlDepth();
		}

		public void SecondarySetControlDepth(float Depth)
		{
			SecondarySetControl = Depth;
			SetControlDepth();
		}

		/// <summary>
		///  Set the control rod Depth percentage
		/// </summary>
		/// <param name="speedMultiplier"></param>
		public void SetControlDepth()
		{
			//Loggy.Log("YO " + Depth);
			ReactorControlConsole.SuchControlRodDepth(MainSetControl + (SecondarySetControl / 100));
		}

		[Serializable]
		public class GUI_ReactorLayout
		{
			public GUI_ReactorController GUI_ReactorController;

			[SerializeField]
			private List<NetColorChanger> rodChamber = new List<NetColorChanger>();

			public void Start()
			{
				for (int i = 0; i < 16; i++)
				{
					rodChamber.Add(GUI_ReactorController["ReactorSlot (" + i + ")"] as NetColorChanger);
				}
			}

			public void Refresh()
			{
				for (int i = 0; i < GUI_ReactorController.ReactorControlConsole.ReactorChambers.ReactorRods.Length; i++)
				{
					if (GUI_ReactorController.ReactorControlConsole.ReactorChambers.ReactorRods[i] != null)
					{
						var TheType = GUI_ReactorController.ReactorControlConsole.ReactorChambers.ReactorRods[i]
							.GetUIColour();


						rodChamber[i].MasterSetValue(TheType);
					}
					else
					{
						rodChamber[i].MasterSetValue(Color.gray);
					}
				}
			}
		}

		[Serializable]
		public class GUI_ReactorAnnunciator
		{
			public GUI_ReactorController GUI_ReactorController = null;

			[SerializeField] private NetFlasher highTemperature;
			[SerializeField] private NetFlasher highTemperatureDelta;
			[SerializeField] private NetFlasher lowTemperature;
			[SerializeField] private NetFlasher highNeutronFlux;
			[SerializeField] private NetFlasher highNeutronFluxDelta;
			[SerializeField] private NetFlasher lowNeutronFlux;
			[SerializeField] private NetFlasher positiveKValue;
			[SerializeField] private NetFlasher lowKValue;
			[SerializeField] private NetFlasher lowWaterLevel;

			[SerializeField] private NetFlasher highWaterLevel;
			[SerializeField] private NetFlasher highCorePressure;
			[SerializeField] private NetFlasher highPressureDelta;
			[SerializeField] private NetFlasher lowControlRodDepth;
			[SerializeField] private NetFlasher highControlRodDepth;

			[SerializeField] private NetFlasher coreMeltdown;

			[SerializeField] private NetFlasher clown;
			[SerializeField] private NetFlasher highDegradationOfFuel;
			[SerializeField] private NetFlasher corePipeBurst;


			private float last_Pressure = 0;
			private float last_Temperature = 200;

			private decimal last_HighNeutronFluxDelta = 0;
			private readonly decimal highNeutronFluxDelta_Delta = 0;

			private LayerMask? hitMask;

			public void Refresh()
			{
				hitMask ??= LayerMask.GetMask("Players");

				var chamber = GUI_ReactorController.ReactorControlConsole.ReactorChambers;

				SetTemperatureWarnings(chamber);
				SetNeutronFluxWarnings(chamber);
				SetKValueWarnings();
				SetWaterLevelWarnings(chamber);
				SetCorePressureWarnings(chamber);
				SetRobDepthWarnings(chamber);

				clown.SetState(FindClown(chamber));

				coreMeltdown.SetState(chamber.MeltedDown);
				highDegradationOfFuel.SetState(chamber.ReactorFuelRods.Any(x => (x.PresentAtomsfuel / x.PresentAtoms) < 0.65m));
				corePipeBurst.SetState(chamber.PoppedPipes);
			}

			private bool FindClown(ReactorGraphiteChamber chamber)
			{
				var players = Physics2D.OverlapCircleAll(chamber.transform.position, 10, hitMask.Value);
				foreach (var player in players)
				{
					if (player.gameObject.OrNull()?.GetComponent<PlayerScript>().OrNull()?.Mind.OrNull()?.occupation
							.OrNull()?.JobType == JobType.CLOWN)
					{
						return true;
					}
				}
				return false;
			}

			private void SetTemperatureWarnings(ReactorGraphiteChamber chamber)
			{
				bool TEMPToohigh = (chamber.RodMeltingTemperatureK - 200) <
				                   chamber.ReactorPipe.pipeData.mixAndVolume.Temperature;
				highTemperature.SetState(TEMPToohigh);

				if (TEMPToohigh)
				{
					GUI_ReactorController.ReactorControlConsole.SuchControlRodDepth(1);
				}


				float temperature_Delta = Math.Abs(chamber.ReactorPipe.pipeData.mixAndVolume.Temperature - last_Temperature);

				highTemperatureDelta.SetState(temperature_Delta > 25);

				lowTemperature.SetState(chamber.ReactorPipe.pipeData.mixAndVolume.Temperature < 373.15f);

				last_Temperature = chamber.ReactorPipe.pipeData.mixAndVolume.Temperature;
			}

			private void SetNeutronFluxWarnings(ReactorGraphiteChamber chamber)
			{
				highNeutronFlux.SetState(chamber.PresentNeutrons > (ReactorGraphiteChamber.NEUTRON_SINGULARITY - 1000000));
				highNeutronFluxDelta.SetState(highNeutronFluxDelta_Delta > 100000);
				lowNeutronFlux.SetState(chamber.PresentNeutrons < 200);

				last_HighNeutronFluxDelta = Math.Abs(chamber.PresentNeutrons - last_HighNeutronFluxDelta);
				last_HighNeutronFluxDelta = chamber.PresentNeutrons;
			}

			private void SetKValueWarnings()
			{
				positiveKValue.SetState(GUI_ReactorController.percentageChange > 2);
				lowKValue.SetState(GUI_ReactorController.percentageChange < -2);
			}

			private void SetWaterLevelWarnings(ReactorGraphiteChamber chamber)
			{
				lowWaterLevel.SetState(chamber.ReactorPipe.pipeData.mixAndVolume.Total.x < 25);
				highWaterLevel.SetState(chamber.ReactorPipe.pipeData.mixAndVolume.Total.x > 190);
			}

			private void SetCorePressureWarnings(ReactorGraphiteChamber chamber)
			{
				float pressure = (float)chamber.CurrentPressure;

				bool PressureTooHigh = pressure > (float) (ReactorGraphiteChamber.MAX_CORE_PRESSURE - HIGH_PRESSURE_THRESHOLD);

				bool PressureTooooHigh = pressure > (float) (ReactorGraphiteChamber.MAX_CORE_PRESSURE - HIGH_PRESSURE_THRESHOLD_SCRAM);

				highCorePressure.SetState(PressureTooHigh);

				if (PressureTooooHigh)
				{
					GUI_ReactorController.ReactorControlConsole.SuchControlRodDepth(1);
				}



				float pressure_Delta = Math.Abs(pressure - last_Pressure);
				highPressureDelta.SetState(pressure_Delta > HIGH_PRESSURE_DELTA_THRESHOLD);
				last_Pressure = pressure;
			}

			public void SetRobDepthWarnings(ReactorGraphiteChamber chamber)
			{
				lowControlRodDepth.SetState(chamber.ControlRodDepthPercentage < 0.20f);
				highControlRodDepth.SetState(chamber.ControlRodDepthPercentage > 0.90f);
			}
		}
	}
}