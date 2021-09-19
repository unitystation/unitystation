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
		IMultitoolMasterable IMultitoolSlaveable.Master => linkedMaster;
		bool IMultitoolSlaveable.RequireLink => true;
		bool IMultitoolSlaveable.TrySetMaster(PositionalHandApply interaction, IMultitoolMasterable master)
		{
			SetMaster(master);
			return true;
		}
		void IMultitoolSlaveable.SetMasterEditor(IMultitoolMasterable master)
		{
			SetMaster(master);
		}

		private IMultitoolMasterable linkedMaster;

		private void SetMaster(IMultitoolMasterable master)
		{
			if (master is ReactorBoiler boiler)
			{
				linkedMaster = master;
				ReactorBoiler = boiler;
			}
			else if (master is ReactorTurbine turbine)
			{
				linkedMaster = master;
				ReactorTurbine = turbine;
			}
			else
			{
				linkedMaster = null;
				ReactorBoiler = null;
				ReactorTurbine = null;
			}
		}

		#endregion
	}
}
