using System.Collections.Generic;
using UnityEngine;
using Systems.ObjectConnection;


namespace Objects.Engineering
{
	public class ReactorControlConsole : MonoBehaviour, IMultitoolSlaveable
	{
		[SceneObjectReference] public ReactorGraphiteChamber ReactorChambers = null;

		[SceneObjectReference] public List<ReactorGraphiteChamber> ReactorChambers2 = new List<ReactorGraphiteChamber>();

		public void SuchControllRodDepth(float requestedDepth)
		{
			requestedDepth = requestedDepth.Clamp(0, 1);

			if (ReactorChambers != null)
			{
				ReactorChambers.SetControlRodDepth(requestedDepth);
			}
		}

		#region Multitool Interaction

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.ReactorChamber;
		IMultitoolMasterable IMultitoolSlaveable.Master => ReactorChambers;
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

		private void SetMaster(IMultitoolMasterable master)
		{
			ReactorChambers = master is ReactorGraphiteChamber reactor ? reactor : null;
		}

		#endregion
	}
}
