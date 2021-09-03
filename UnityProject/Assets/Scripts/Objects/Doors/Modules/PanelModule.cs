using System;
using AddressableReferences;
using UnityEngine;
using System.Collections.Generic;
using Messages.Server;
using Messages.Client.NewPlayer;
using UI.Core.Net;
using Systems.Interaction;
using Mirror;

namespace Doors.Modules
{
	public class PanelModule : DoorModuleBase
	{
		private bool isPanelOpen = false;

		public override ModuleSignal OpenInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			return ModuleSignal.Continue;
		}

		public override ModuleSignal ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (interaction == null) return ModuleSignal.Continue;
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver))
			{
				TogglePanel();
				return ModuleSignal.Break;
			}
			if (interaction.HandObject == null && interaction.Performer != null && isPanelOpen)
			{
				return ModuleSignal.Break;
			}
		
			return ModuleSignal.Continue;
		}

		public override ModuleSignal BumpingInteraction(GameObject byPlayer, HashSet<DoorProcessingStates> States)
		{
			return ModuleSignal.Continue;
		}

		public override bool CanDoorStateChange()
		{
			return !isPanelOpen;
		}

		private void TogglePanel()
		{
			if (master.IsPerformingAction || !master.IsClosed)
			{
				return;
			}

			isPanelOpen = !isPanelOpen;

			if (isPanelOpen)
			{
				
				master.DoorAnimator.AddPanelOverlay();
				master.GetComponent<Objects.HasNetworkTab>().NetTabType = NetTabType.HackingPanel;
			}
			else
			{
				master.DoorAnimator.RemovePanelOverlay();
				master.GetComponent<Objects.HasNetworkTab>().NetTabType = NetTabType.Airlock;
			}
		}
	}
}
