using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAPCPowered
{
	void PowerNetworkUpdate(float Voltage);
	void StateUpdate(PowerStates State);
}
