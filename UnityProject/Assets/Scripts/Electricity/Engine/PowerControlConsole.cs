using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerControlConsole : MonoBehaviour, IAPCPowered
{
	public ResistanceSourceModule ThisDevice;
	public ReactorControlConsole ControlConsole;
	private float TargetsSupplyOutputVoltage = 275000;

	private float PreviousVoltage = 0;

	void IAPCPowered.PowerNetworkUpdate(float Voltage)
	{
		/*ElectricityFunctions.WorkOutActualNumbers(ThisDevice.ControllingNode.Node.InData);

		var ControllerMultiplier = (TargetsSupplyOutputVoltage+1) / (ThisDevice.ControllingNode.Node.InData.Data.ActualVoltage+1);
		Logger.Log("in ControllerMultiplier " + ControllerMultiplier+"  >> " +  (TargetsSupplyOutputVoltage+1)+ " / " + (ThisDevice.ControllingNode.Node.InData.Data.ActualVoltage+1));
		if (ControllerMultiplier != 0 && !float.IsInfinity(ControllerMultiplier))
		{
			ControlConsole.RequestRelativeChange(ControllerMultiplier);
		}

		PreviousVoltage = ThisDevice.ControllingNode.Node.InData.Data.ActualVoltage;*/

	}

	void IAPCPowered.StateUpdate(PowerStates State)
	{

	}
}
