using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Electricity
{
	public interface IAPCPowerable
	{
		void PowerNetworkUpdate(float voltage);

		void StateUpdate(PowerState state);
	}
}
