using Mirror;
using Systems.Electricity;
using Systems.ObjectConnection;
using UnityEngine;

namespace Systems.Research.Objects
{
	public class ResearchPointMachine : NetworkBehaviour, IAPCPowerable, IMultitoolSlaveable, IExaminable
	{
		public ResearchServer researchServer;

		/// <summary>
		/// Add points based on difference, as per AddResearchPointsDifference on the ResearchServer.
		/// </summary>
		/// <param name="source">The machine that is adding points.</param>
		/// <param name="points">The Amount to attempt to add.</param>
		/// <returns></returns>
		public virtual int AwardResearchPoints(ResearchPointMachine source,int points)
		{
			return researchServer.AddResearchPointsDifference(source, points);
		}

		/// <summary>
		/// Adds extra untracked Research Points to the techWeb total, not tied to any source.
		/// </summary>
		/// <param name="points">The amount to be added.</param>
		public virtual void AddResearchPoints(int points)
		{
			researchServer.AddResearchPoints(points);
		}

		/// <summary>
		/// Adds Research Points to the techWeb total, tracked according to source.
		/// </summary>
		/// <param name="source">Machine type that's adding the points.</param>
		/// <param name="points">The amount to be added.</param>
		/// <returns></returns>
		public virtual int AddResearchPoints(ResearchPointMachine source, int points)
		{
			return researchServer.AddResearchPoints(source, points);
		}

		#region Multitool Interaction
		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.ResearchServer;
		IMultitoolMasterable IMultitoolSlaveable.Master => researchServer;
		bool IMultitoolSlaveable.RequireLink => false;

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
			if (master is ResearchServer server)
			{
				SubscribeToServerEvent(server);
			}
			else if (researchServer != null)
			{
				UnSubscribeFromServerEvent();
			}
		}

		public virtual void SubscribeToServerEvent(ResearchServer server)
		{
			if (PoweredState == PowerState.Off) return;
			UnSubscribeFromServerEvent();
			researchServer = server;
		}

		public virtual void UnSubscribeFromServerEvent()
		{
			researchServer = null;
		}

		#endregion

		#region IAPCPowerable
		public PowerState PoweredState;

		public void PowerNetworkUpdate(float voltage)
		{
		}

		public virtual void StateUpdate(PowerState state)
		{
			if (state == PowerState.Off)
			{
				//Machine loses connection to server on power loss
				UnSubscribeFromServerEvent();
			}

			PoweredState = state;
		}
		#endregion

		#region IExaminable

		public virtual string Examine(Vector3 worldPos = default)
		{
			if (PoweredState != PowerState.Off)
			{
				return (researchServer == null) ?
					"Server connection light is blinking red. " :
					"Server connection light is green. ";
			}

			return default;
		}

		#endregion
	}
}
