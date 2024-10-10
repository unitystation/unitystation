using System.Collections.Generic;
using Systems.Electricity;
using UnityEngine;

namespace Doors.Modules
{
	public class PowerModule : DoorModuleBase, IServerSpawn
	{
		public bool HasPower => GetPowerState();
		[SerializeField] private APCPoweredDevice apc;
		public APCPoweredDevice Apc => apc;

		
		public void OnSpawnServer(SpawnInfo info)
		{
			master.HackingProcessBase.RegisterPort(CheckPower, master.GetType());
		}
		
		public bool GetPowerState()
		{			
			return master.HackingProcessBase.PulsePortConnectedNoLoop(CheckPower);
		}

		public void CheckPower()
		{
			if (APCPoweredDevice.IsOn(apc.State))
			{
				master.HackingProcessBase.ReceivedPulse(CheckPower);
			}
		}
		
		public override void OpenInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (HasPower == false)
			{
				States.Add(DoorProcessingStates.PowerPrevented);
			}
		}
		
		public override void ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (HasPower == false)
			{
				States.Add(DoorProcessingStates.PowerPrevented);
			}
		}
		
		public override void BumpingInteraction(GameObject byPlayer, HashSet<DoorProcessingStates> States)
		{
			if (HasPower == false)
			{
				Chat.AddExamineMsgFromServer(byPlayer, $"{master.gameObject.ExpensiveName()} is unpowered");
				States.Add(DoorProcessingStates.PowerPrevented);
			}
		}
	}
}