using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_BoilerTurbineController : NetTab
{
	public BoilerTurbineController boilerTurbineController;
	public GUI_BoilerAnnunciators GUIBoilerAnnunciators;
	public GUI_TurbineAnnunciators GUITurbineAnnunciators;
	public decimal CapacityPercent = 0;
	[SerializeField] private NetSliderDial BoilerTemperature = null;
	[SerializeField] private NetSliderDial BoilerPressure= null;
	[SerializeField] private NetSliderDial BoilerCapacity= null;
	[SerializeField] private NetLabel TurbinePowerGenerating= null;
	[SerializeField] private NetLabel TurbineVoltage= null;
	[SerializeField] private NetLabel TurbineCurrent= null;

	void Start()
	{

		if (Provider != null)
		{
			//Makes sure it connects with the dispenser properly
			boilerTurbineController = Provider.GetComponentInChildren<BoilerTurbineController>();
			GUIBoilerAnnunciators.GUIBoilerTurbineController = this;
			GUITurbineAnnunciators.GUIBoilerTurbineController = this;
		}
	}

	private void OnEnable()
	{
		base.OnEnable();
		if (CustomNetworkManager.Instance._isServer == false ) return;
		UpdateManager.Add(Refresh, 1);
	}

	private void OnDisable()
	{
		if (CustomNetworkManager.Instance._isServer == false ) return;
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Refresh);
	}

	public void Refresh()
	{
		if (boilerTurbineController.ReactorBoiler != null)
		{
			CapacityPercent = boilerTurbineController.ReactorBoiler.CurrentPressureInput /
			                  boilerTurbineController.ReactorBoiler.MaxPressureInput;
			BoilerTemperature.SetValueServer(Math
				.Round((boilerTurbineController.ReactorBoiler.ReactorPipe.pipeData.mixAndVolume.Mix.Temperature /
				        12000) * 100).ToString());

			BoilerPressure.SetValueServer(Math
				.Round((boilerTurbineController.ReactorBoiler.CurrentPressureInput /
				        boilerTurbineController.ReactorBoiler.MaxPressureInput) * 100).ToString());


			BoilerCapacity.SetValueServer(Math.Round((CapacityPercent) * 100).ToString());
			GUIBoilerAnnunciators.Refresh();
		}

		if (boilerTurbineController.ReactorTurbine != null)
		{
			TurbineCurrent.SetValueServer(boilerTurbineController.ReactorTurbine.moduleSupplyingDevice.GetCurrente() +
			                              "A");
			TurbineVoltage.SetValueServer(boilerTurbineController.ReactorTurbine.moduleSupplyingDevice.GetVoltage() +
			                              "V");
			TurbinePowerGenerating.SetValueServer(boilerTurbineController.ReactorTurbine.moduleSupplyingDevice
				.ProducingWatts + "W");
			GUITurbineAnnunciators.Refresh();
		}


	}

	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}

	[System.Serializable]
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
		public float capacityDelta= 0;

		public void Capacity()
		{
			BoilerAtHighCapacity.SetValueServer(
				(GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.CurrentPressureInput >
				 (GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.MaxPressureInput * 0.8m))
				.ToString());

			BoilerAtLowCapacity.SetValueServer(
				(GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.CurrentPressureInput >
				 (GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.MaxPressureInput * 0.1m))
				.ToString());
			capacityDelta = Math.Abs((float) GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.CurrentPressureInput - Last_capacity);

			HighBoilerCapacityDelta.SetValueServer((capacityDelta > 200).ToString());
			Last_capacity = (float)GUIBoilerTurbineController.boilerTurbineController.ReactorBoiler.CurrentPressureInput;
		}
	}

	[System.Serializable]
	public class GUI_TurbineAnnunciators
	{
		public GUI_BoilerTurbineController GUIBoilerTurbineController;

		public NetFlasher Highvoltage = null;
		public NetFlasher LowVoltage = null;
		public NetFlasher NoVoltage = null;

		public void Refresh()
		{
			var Voltage = GUIBoilerTurbineController.boilerTurbineController.ReactorTurbine.moduleSupplyingDevice.GetVoltage();
			Highvoltage.SetValueServer((Voltage>780000).ToString());
			LowVoltage.SetValueServer((Voltage<700000).ToString());
			NoVoltage.SetValueServer((Voltage<10).ToString());
		}

	}
}