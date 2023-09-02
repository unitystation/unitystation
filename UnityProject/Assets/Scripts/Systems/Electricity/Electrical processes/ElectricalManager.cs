using System;
using Logs;
using Shared.Managers;

namespace Systems.Electricity
{
	public class ElectricalManager : SingletonManager<ElectricalManager>
	{
		public CableTileList HighVoltageCables;
		public CableTileList MediumVoltageCables;
		public CableTileList LowVoltageCables;
		public ElectricalSynchronisation electricalSync;

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			EventManager.AddHandler(Event.PostRoundStarted, OnPostRoundStart);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			EventManager.RemoveHandler(Event.PostRoundStarted, OnPostRoundStart);
			Stop();
		}

		private void UpdateMe()
		{
			if (CustomNetworkManager.IsServer == false) return;

			if (electricalSync.MainThreadStep)
			{
				try
				{
					electricalSync.DoTick();
				}
				catch (Exception e)
				{
					Loggy.LogError($"Electrical MainThreadProcess Error! {e.GetStack()}", Category.Electrical);
				}
			}
		}

		private void OnPostRoundStart()
		{
			if (CustomNetworkManager.IsServer == false) return;

			electricalSync.StartSim();
			Loggy.Log("Round Started", Category.Electrical);
		}

		private void Stop()
		{
			electricalSync.StopSim();
			Loggy.Log("Round Ended", Category.Electrical);
		}
	}
}