using System.Collections.Generic;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Lifecycle;
using US13.Objects.Engineering;

namespace US13.Objects.Doors.Modules
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

		public override void OpenInteraction(HandApply interaction, ref HashSet<DoorProcessingStates> States)
		{
			if (HasPower == false)
			{
				States.Add(DoorProcessingStates.PowerPrevented);
			}
		}

		public override void ClosedInteraction(HandApply interaction, ref HashSet<DoorProcessingStates> States)
		{
			if (HasPower == false)
			{
				States.Add(DoorProcessingStates.PowerPrevented);
			}
		}

		public override void BumpingInteraction(GameObject byPlayer, ref HashSet<DoorProcessingStates> States)
		{
			if (HasPower == false)
			{
				Chat.AddExamineMsgFromServer(byPlayer, $"The {master.DoorName} is unpowered");
				States.Add(DoorProcessingStates.PowerPrevented);
			}
		}
	}
}