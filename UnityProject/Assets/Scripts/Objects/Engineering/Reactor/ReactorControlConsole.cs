using System.Collections.Generic;
using UnityEngine;
using Systems.ObjectConnection;


namespace Objects.Engineering
{
	public class ReactorControlConsole : MonoBehaviour, ISetMultitoolSlave
	{
		public ReactorGraphiteChamber ReactorChambers = null;

		public void SuchControllRodDepth(float requestedDepth)
		{
			requestedDepth = requestedDepth.Clamp(0, 1);

			if (ReactorChambers != null)
			{
				ReactorChambers.SetControlRodDepth(requestedDepth);
			}
		}

		#region Multitool Interaction

		public MultitoolConnectionType ConType => MultitoolConnectionType.ReactorChamber;

		public void SetMaster(ISetMultitoolMaster Imaster)
		{
			var Chamber = (Imaster as Component)?.gameObject.GetComponent<ReactorGraphiteChamber>();
			if (Chamber != null)
			{
				ReactorChambers = Chamber;
			}
		}

		#endregion
	}
}
