using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_BoilerTurbineController : NetTab
{
	public BoilerTurbineController boilerTurbineController;

	[SerializeField] private NetLabel BoilerTemperature;
	[SerializeField] private NetLabel BoilerPressure;
	[SerializeField] private NetLabel BoilerCapacity;
	[SerializeField] private NetLabel TurbinePowerGenerating;
	[SerializeField] private NetLabel TurbineVoltage;
	[SerializeField] private NetLabel TurbineCurrent;

	void Start()
	{
		if (Provider != null)
		{
			//Makes sure it connects with the dispenser properly
			boilerTurbineController = Provider.GetComponentInChildren<BoilerTurbineController>();
		}

	}

	private void OnEnable()
	{
		base.OnEnable();
		UpdateManager.Add(Refresh, 1);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Refresh);
	}

	public void Refresh()
	{
		if (boilerTurbineController.ReactorBoiler != null)
		{

			BoilerTemperature.SetValueServer("Boiler Temperature " + boilerTurbineController.ReactorBoiler.ReactorPipe.pipeData.mixAndVolume.Mix.Temperature + " K " );
			BoilerPressure.SetValueServer("Boiler BoilerPressure " + boilerTurbineController.ReactorBoiler.CurrentPressureInput + " Kpa " );
			BoilerCapacity.SetValueServer("Boiler Capacity " + boilerTurbineController.ReactorBoiler.CurrentPressureInput/boilerTurbineController.ReactorBoiler.MaxPressureInput + " % " );
		}
		if (boilerTurbineController.ReactorTurbine != null )
		{
			TurbinePowerGenerating.SetValueServer("Turbine Power Generating " + boilerTurbineController.ReactorTurbine.moduleSupplyingDevice.ProducingWatts+ " watts " );
		}

	}

}
