using System.Collections.Generic;
using UnityEngine;
using Systems.ObjectConnection;


namespace Objects.Engineering
{
	public class BoilerTurbineController : MonoBehaviour, ISetMultitoolSlave
	{
		public bool State = false;
		public ReactorBoiler ReactorBoiler = null;
		public ReactorTurbine ReactorTurbine = null;

		[RightClickMethod]
		public void ChangeState()
		{
			State = !State;
		}

		#region Multitool Interaction

		public MultitoolConnectionType ConType => MultitoolConnectionType.BoilerTurbine;

		public void SetMaster(ISetMultitoolMaster Imaster)
		{
			var boiler = (Imaster as Component)?.gameObject.GetComponent<ReactorBoiler>();
			if (boiler != null)
			{
				ReactorBoiler = boiler;
			}
			var Turbine = (Imaster as Component)?.gameObject.GetComponent<ReactorTurbine>();
			if (Turbine != null)
			{
				ReactorTurbine = Turbine;
			}
		}

		#endregion
	}
}
