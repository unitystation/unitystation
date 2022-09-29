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
			if (boilerTurbineController.ReactorBoiler != null)
			{
				CapacityPercent = boilerTurbineController.ReactorBoiler.CurrentPressureInput /
								  boilerTurbineController.ReactorBoiler.MaxPressureInput;
				BoilerTemperature.MasterSetValue(Math
					.Round((boilerTurbineController.ReactorBoiler.ReactorPipe.pipeData.mixAndVolume.Temperature /
							12000) * 100).ToString());

				BoilerPressure.MasterSetValue(Math
					.Round((boilerTurbineController.ReactorBoiler.CurrentPressureInput /
							boilerTurbineController.ReactorBoiler.MaxPressureInput) * 100).ToString());


				BoilerCapacity.MasterSetValue(Math.Round((CapacityPercent) * 100).ToString());
				GUIBoilerAnnunciators.Refresh();
			}

			if (boilerTurbineController.ReactorTurbine != null)
			{
				TurbineCurrent.MasterSetValue(boilerTurbineController.ReactorTurbine.moduleSupplyingDevice.GetCurrente() +
											  "A");
				TurbineVoltage.MasterSetValue(boilerTurbineController.ReactorTurbine.moduleSupplyingDevice.GetVoltage() +
											  "V");
				TurbinePowerGenerating.MasterSetValue(boilerTurbineController.ReactorTurbine.moduleSupplyingDevice
					.ProducingWatts + "W");
				GUITurbineAnnunciators.Refresh();
			}
		}

		[Serializable]
		public class GUI_BoilerAnnunciators
		{
			public GUI_BoilerTurbineController GUIBoilerTurbineController;

			public NetFlasher BoilerAtHighCapacity = null;
			public NetFlasher BoilerAtLowCapacity = null;
			public NetFlasher HighBoilerCapacityDelta = null;
			public NetFlasher HighPressure = null;
			public NetFlasher LowPressure = null;
			public NetFlasher HighPressureDelta = null;

			public void Refresh()
			{
				Capacity();
			}

			public float Last_capacity = 0;
			public float capacityDelta = 0;

			public void Capacity()
			{
				BoilerAtHighCapacity.MasterSetValue(
					(GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.CurrentPressureInput >
					 (GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.MaxPressureInput * 0.8m))
					.ToString());

				BoilerAtLowCapacity.MasterSetValue(
					(GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.CurrentPressureInput >
					 (GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.MaxPressureInput * 0.1m))
					.ToString());
				capacityDelta = Math.Abs((float)GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.CurrentPressureInput - Last_capacity);

				HighBoilerCapacityDelta.MasterSetValue((capacityDelta > 200).ToString());
				Last_capacity = (float)GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.CurrentPressureInput;
			}
		}

		[Serializable]
		public class GUI_TurbineAnnunciators
		{
			public GUI_BoilerTurbineController GUIBoilerTurbineController;

			public NetFlasher Highvoltage = null;
			public NetFlasher LowVoltage = null;
			public NetFlasher NoVoltage = null;

			public void Refresh()
			{
				var Voltage = GUIBoilerTurbineController.boilerTurbineController.ReactorTurbine.moduleSupplyingDevice.GetVoltage();
				Highvoltage.MasterSetValue((Voltage > 780000).ToString());
				LowVoltage.MasterSetValue((Voltage < 700000).ToString());
				NoVoltage.MasterSetValue((Voltage < 10).ToString());
			}

		}
	}
}
