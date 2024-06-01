using System.Collections.Generic;
using Mirror;
using Shared.Systems.ObjectConnection;
using Systems.Electricity.NodeModules;
using UnityEngine;

namespace Systems.Electricity.PowerSupplies
{
	public class SolarPanelController : NetworkBehaviour, IMultitoolMasterable
	{
		public ModuleSupplyingDevice ModuleSupply;
		public float UpdateRate = 10f;
		[SerializeField] private List<SolarPanel> connectedPanels;

		public MultitoolConnectionType ConType { get; } = MultitoolConnectionType.SolarPanel;
		public bool CanRelink { get; } = true;
		public int MaxDistance { get; } = 64;
		public bool IgnoreMaxDistanceMapper { get; } = false;

		private void Awake()
		{
			ModuleSupply ??= GetComponent<ModuleSupplyingDevice>();
			UpdateManager.Add(UpdateMe, UpdateRate);
		}

		private void OnDestroy()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			var watts = 0;
			foreach (var panel in connectedPanels)
			{
				watts += panel.LastProducedWatts;
			}
			ModuleSupply.ProducingWatts = watts;
		}

		public bool AddDevice(SolarPanel panel)
		{
			if (connectedPanels.Contains(panel)) return false;
			connectedPanels.Add(panel);
			return true;
		}

		public void RemoveDevice(SolarPanel panel)
		{
			if (connectedPanels.Contains(panel) == false) return;
			panel.Controller = null;
			connectedPanels.Remove(panel);
		}
	}
}