using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Electricity
{
	public interface IAPCPowered
	{
		void PowerNetworkUpdate(float Voltage);
		void StateUpdate(PowerStates State);
	}
}
