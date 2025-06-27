using System.Collections.Generic;
using Mirror;
using Shared.Managers;
using Systems.Atmospherics;
using Systems.Electricity;
using Systems.Radiation;
using UnityEngine;

public class PerformanceManager : SingletonManager<PerformanceManager>
{
	public struct PerformanceInfo

	{
		public uint ElectricalTick;
		public int ElectricalSupplies;
		public int ElectricalPoweredDevices;

		public uint AtmosTick;
		public int AtmosPipes;
		public int AtmosTiles;

		public uint RadiationTick;
		public int RadiationPulseQueue;

		public int UpdateManagerPreCameraUpdateActionsCount;
		public int UpdateManagerUpdateActionsCount;
		public int UpdateManagerFixedUpdateActionsCount;
		public int UpdateManagerLateUpdateActionsCount;
		public int UpdateManagerPeriodicUpdateActionsCount;
		public int UpdateManagerSoundUpdatesCount;
		public int UpdateManagerThinkShotActionsCount;

		public int[] MatrixQueuedChangeCounts;
	}

	public void AdminRequest(PlayerInfo recipient)
	{
		AdminReturnPerformancesStatistics.Send(recipient, GetInfo());
	}


	public PerformanceInfo GetInfo()
	{
		//TODO Get more info, Without being laggy
		//so We can't say what and the update manager is being slow well you can say what  categories are taking x time
		//However we can't work out what action-method-class is being called too many times or is being slow in general
		//Since that would require getting the name of every action in the update manager every frame SLOW,
		//Could may be an option that That we turn on for a frame for the admin?
		//(Need to check what's best way to check if statement with bool, since it needs to be basically zero impact when not in use)
		//
		//Currently there is no good way of catching the Ienumerating/Uni task This stuff and Net message processing/commons
		//
		//A satellite reagent processing is a good example since it so split up, since each container does its own processing
		//
		//TODO have a think abouut this
		var info = new PerformanceInfo
		{
			ElectricalTick = ElectricalManager.Instance.electricalSync.electricalThread.ticker,
			ElectricalSupplies = ElectricalManager.Instance.electricalSync.TotalSupplies.Count,
			ElectricalPoweredDevices = ElectricalManager.Instance.electricalSync.PoweredDevices.Count,

			AtmosTick = AtmosManager.Instance.AtmosThread.ticker,
			AtmosPipes = AtmosManager.Instance.pipeList.Count,
			AtmosTiles = AtmosManager.Instance.atmosphericsUpdates.Count,

			RadiationTick = RadiationManager.Instance.RadiationThread.ticker,
			RadiationPulseQueue = RadiationManager.Instance.PulseQueue.Count,

			UpdateManagerPreCameraUpdateActionsCount = UpdateManager.Instance.preCameraUpdateActionsCount,
			UpdateManagerUpdateActionsCount = UpdateManager.Instance.updateActionsCount,
			UpdateManagerFixedUpdateActionsCount = UpdateManager.Instance.fixedUpdateActionsCount,
			UpdateManagerLateUpdateActionsCount = UpdateManager.Instance.lateUpdateActionsCount,
			UpdateManagerPeriodicUpdateActionsCount = UpdateManager.Instance.periodicUpdateActionsCount,
			UpdateManagerSoundUpdatesCount = UpdateManager.Instance.soundUpdatesCount,
			UpdateManagerThinkShotActionsCount = UpdateManager.Instance.thinkShotActionsCount,


		};

		var Matrix = new List<int>();

		foreach (var matrix in MatrixManager.Instance.ActiveMatrices)
		{
			if (matrix.Value.MetaTileMap.QueuedChanges.Count > 0)
			{
				Matrix.Add(matrix.Value.MetaTileMap.QueuedChanges.Count);
			}
		}

		info.MatrixQueuedChangeCounts = Matrix.ToArray();
		return info;
	}
}