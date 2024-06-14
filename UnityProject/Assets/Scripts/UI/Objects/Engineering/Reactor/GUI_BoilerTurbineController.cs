using System;
using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Engineering;

namespace UI.Objects.Engineering
{
	public class GUI_BoilerTurbineController : NetTab
	{
		public BoilerTurbineController boilerTurbineController;
		public GUI_BoilerAnnunciators GUIBoilerAnnunciators;
		public GUI_TurbineAnnunciators GUITurbineAnnunciators;
		public decimal CapacityPercent = 0;
		[SerializeField] private NetSliderDial BoilerTemperature = null;
		[SerializeField] private NetSliderDial BoilerPressure = null;
		[SerializeField] private NetSliderDial BoilerCapacity = null;
		[SerializeField] private NetText_label TurbinePowerGenerating = null;
		[SerializeField] private NetText_label TurbineVoltage = null;
		[SerializeField] private NetText_label TurbineCurrent = null;

		private const int MAX_BOILER_TEMPERATURE = 2000;

		private void Start()
		{
			if (Provider != null)
			{
				//Makes sure it connects with the dispenser properly
				boilerTurbineController = Provider.GetComponentInChildren<BoilerTurbineController>();
				GUIBoilerAnnunciators.GUIBoilerTurbineController = this;
				GUITurbineAnnunciators.GUIBoilerTurbineController = this;
			}
		}

		public override void OnEnable()
		{
			base.OnEnable();
			if (CustomNetworkManager.Instance._isServer == false) return;
			UpdateManager.Add(Refresh, 1);
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.Instance._isServer == false) return;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Refresh);
		}

		public void Refresh()
		{
			if (boilerTurbineController == null || boilerTurbineController.ReactorBoiler == null) return;
			
			CapacityPercent = (decimal)(boilerTurbineController.ReactorBoiler.ReactorPipe.pipeData.mixAndVolume.Total.x
					/ boilerTurbineController.ReactorBoiler.ReactorPipe.pipeData.mixAndVolume.TheVolume);

			BoilerTemperature.MasterSetValue(Math
					.Round((boilerTurbineController.ReactorBoiler.ReactorPipe.pipeData.mixAndVolume.Temperature / MAX_BOILER_TEMPERATURE) * 100).ToString());

			BoilerPressure.MasterSetValue(Math
					.Round((decimal)boilerTurbineController.ReactorBoiler.BoilerPressure / boilerTurbineController.ReactorBoiler.MaxPressureInput * 100).ToString());


			BoilerCapacity.MasterSetValue(Math.Round(CapacityPercent * 100).ToString());
				GUIBoilerAnnunciators.Refresh();

			TurbineCurrent.MasterSetValue(boilerTurbineController.ReactorTurbine.moduleSupplyingDevice.GetCurrente() +
											  "A");
			TurbineVoltage.MasterSetValue(boilerTurbineController.ReactorTurbine.moduleSupplyingDevice.GetVoltage() +
											  "V");
			TurbinePowerGenerating.MasterSetValue(boilerTurbineController.ReactorTurbine.moduleSupplyingDevice
					.ProducingWatts + "W");
			GUITurbineAnnunciators.Refresh();
			
		}

		[Serializable]
		public class GUI_BoilerAnnunciators
		{
			public GUI_BoilerTurbineController GUIBoilerTurbineController;

			public NetFlasher BoilerAtHighPressure = null;
			public NetFlasher BoilerAtLowPressure = null;
			public NetFlasher HighBoilerPressureDelta = null;

			public void Refresh()
			{
				Pressure();
			}

			private float last_pressure = 0;
			private float pressureDelta = 0;

			public void Pressure()
			{
				BoilerAtHighPressure.MasterSetValue((GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.BoilerPressure >
					(float)(GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.MaxPressureInput * 0.8m))
					.ToString());

				BoilerAtLowPressure.MasterSetValue((GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.BoilerPressure >
					(float)(GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.MaxPressureInput * 0.1m))
					.ToString());
				pressureDelta = Math.Abs((float)GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.BoilerPressure - last_pressure);

				HighBoilerPressureDelta.MasterSetValue((pressureDelta > 200).ToString());
				last_pressure = (float)GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.BoilerPressure;
			}
		}

		[Serializable]
		public class GUI_TurbineAnnunciators
		{
			public GUI_BoilerTurbineController GUIBoilerTurbineController;

			public NetFlasher Highvoltage = null;
			public NetFlasher LowVoltage = null;
			public NetFlasher NoVoltage = null;

			private const int NO_VOLTAGE_CUTOFF = 10;
			private const int LOW_VOLTAGE_CUTOFF = 500000; //500 kV
			private const int HIGH_VOLTAGE_CUTOFF = 1500000; //1.5 MV

			public void Refresh()
			{
				var Voltage = GUIBoilerTurbineController.boilerTurbineController.ReactorTurbine.moduleSupplyingDevice.GetVoltage();
				Highvoltage.MasterSetValue((Voltage > HIGH_VOLTAGE_CUTOFF).ToString());
				LowVoltage.MasterSetValue((Voltage < LOW_VOLTAGE_CUTOFF).ToString());
				NoVoltage.MasterSetValue((Voltage < NO_VOLTAGE_CUTOFF).ToString());
			}

		}
	}
}
