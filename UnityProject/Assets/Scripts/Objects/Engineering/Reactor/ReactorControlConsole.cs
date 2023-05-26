using System;
using System.Collections.Generic;
using Shared.Systems.ObjectConnection;
using UnityEngine;

namespace Objects.Engineering
{
	public class ReactorControlConsole : MonoBehaviour, IMultitoolSlaveable
	{
		[SceneObjectReference] public ReactorGraphiteChamber ReactorChambers = null;

		[SceneObjectReference] public List<ReactorGraphiteChamber> ReactorChambers2 = new List<ReactorGraphiteChamber>();

		private void Awake()
		{
			AddToReactorChambers();
		}

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
		bool IMultitoolSlaveable.TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			SetMaster(master);
			return true;
		}
		void IMultitoolSlaveable.SetMasterEditor(IMultitoolMasterable master)
		{
			ReactorChambers = master is ReactorGraphiteChamber reactor ? reactor : null;
		}

		private void SetMaster(IMultitoolMasterable master)
		{

			RemoveToReactorChambers();
			ReactorChambers = master is ReactorGraphiteChamber reactor ? reactor : null;
			AddToReactorChambers();
		}

		private void AddToReactorChambers()
		{
			if (ReactorChambers != null)
			{
				if (ReactorChambers.ConnectedConsoles.Contains(this) == false)
				{
					ReactorChambers.ConnectedConsoles.Add(this);
				}
			}
		}

		private void RemoveToReactorChambers()
		{
			if (ReactorChambers != null)
			{
				if (ReactorChambers.ConnectedConsoles.Contains(this))
				{
					ReactorChambers.ConnectedConsoles.Remove(this);
				}
			}
		}

		public void OnDestroy()
		{
			RemoveToReactorChambers();
		}

		#endregion
	}
}
