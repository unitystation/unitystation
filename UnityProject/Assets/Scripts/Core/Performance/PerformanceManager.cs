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

		public int NumberOfLightSynchronisationCategories;
		public int NumberOfTrackedMatrixIntersections;
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

		};

		info.ElectricalTick = ElectricalManager.Instance.electricalSync.electricalThread.ticker;
		info.ElectricalSupplies = ElectricalManager.Instance.electricalSync.TotalSupplies.Count;
		info.ElectricalPoweredDevices = ElectricalManager.Instance.electricalSync.PoweredDevices.Count;

		info.AtmosTick = AtmosManager.Instance.AtmosThread.ticker;
		info.AtmosPipes = AtmosManager.Instance.pipeList.Count;
		info.AtmosTiles = AtmosManager.Instance.atmosphericsUpdates.Count;

		info.RadiationTick = RadiationManager.Instance.RadiationThread.ticker;
		info.RadiationPulseQueue = RadiationManager.Instance.PulseQueue.Count;

		info.UpdateManagerPreCameraUpdateActionsCount = UpdateManager.Instance.preCameraUpdateActionsCount;
		info.UpdateManagerUpdateActionsCount = UpdateManager.Instance.updateActionsCount;
		info.UpdateManagerFixedUpdateActionsCount = UpdateManager.Instance.fixedUpdateActionsCount;
		info.UpdateManagerLateUpdateActionsCount = UpdateManager.Instance.lateUpdateActionsCount;
		info.UpdateManagerPeriodicUpdateActionsCount = UpdateManager.Instance.periodicUpdateActionsCount;
		info.UpdateManagerSoundUpdatesCount = UpdateManager.Instance.soundUpdatesCount;
		info.UpdateManagerThinkShotActionsCount = UpdateManager.Instance.thinkShotActionsCount;

		info.NumberOfLightSynchronisationCategories = LightBrightnessSyncManager.Updates.Count;
		info.NumberOfTrackedMatrixIntersections = MatrixManager.Instance.TrackedIntersections.Count;


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