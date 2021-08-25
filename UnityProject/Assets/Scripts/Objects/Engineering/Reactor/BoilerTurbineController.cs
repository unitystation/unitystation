using System.Collections.Generic;
using UnityEngine;
using Systems.ObjectConnection;


namespace Objects.Engineering
{
	public class BoilerTurbineController : MonoBehaviour, IMultitoolSlaveable
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

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.BoilerTurbine;
		IMultitoolMasterable IMultitoolSlaveable.Master { get => linkedMaster; set => SetMaster(value); }

		private IMultitoolMasterable linkedMaster;

		private void SetMaster(IMultitoolMasterable master)
		{
			var boiler = (master as Component)?.gameObject.GetComponent<ReactorBoiler>();
			if (boiler != null)
			{
				linkedMaster = master;
				ReactorBoiler = boiler;
			}
			var turbine = (master as Component)?.gameObject.GetComponent<ReactorTurbine>();
			if (turbine != null)
			{
				linkedMaster = master;
				ReactorTurbine = turbine;
			}
		}

		#endregion
	}
}
