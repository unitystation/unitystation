using Logs;
using Mirror;
using UnityEngine;
using US13.Managers.NetworkManagement;
using US13.Player.HUDData;
using US13.Systems.Construction;
using US13.Systems.Electricity.NodeModules;
using Util;

namespace US13.Clothing.Eyewear
{
	[RequireComponent(typeof(Machine))]
	public class DiagnosticsHUDPowerBar : NetworkBehaviour, IHUD
	{
		[field:SerializeField]
		public GameObject Prefab { get; set; }
		public GameObject InstantiatedGameObject { get; set; }

		private DiagnosticsHUDHandler diagnosticsHUDHandler;

		[SerializeField] private HUDHandler hudHandler = null;
		[SerializeField] private BatterySupplyingModule batterySupplyingModule = null;
		[SyncVar(hook = nameof(SyncCurrentChargeLevel))]private float currentCharge = 0;

		public void SyncCurrentChargeLevel(float oldCharge, float newCharge)
		{
			if (newCharge.Approx(oldCharge)) return;
			if (batterySupplyingModule && batterySupplyingModule.CapacityMax != 0)
			{
				diagnosticsHUDHandler.UpdateBar(batterySupplyingModule.GetSetCurrentCapacity / batterySupplyingModule.CapacityMax);
			}
		}

		public void Awake()
		{
			if (hudHandler == false)
			{
				Loggy.Error("Hud handler could not be found!");
			}

			if (CustomNetworkManager.IsServer)
			{
				batterySupplyingModule.OnCapacityChangedEvent += UpdateCharge;
			}
			hudHandler.AddNewHud(this);
		}

		private void UpdateCharge(float oldCharge, float newCharge)
		{
			if (CustomNetworkManager.IsServer)
			{
				SyncCurrentChargeLevel(currentCharge, newCharge);
			}
			currentCharge = newCharge;
		}


		public void SetUp()
		{
			diagnosticsHUDHandler = InstantiatedGameObject.GetComponent<DiagnosticsHUDHandler>();
			if (batterySupplyingModule != null && batterySupplyingModule.CapacityMax != 0)
			{
				diagnosticsHUDHandler.UpdateBar(batterySupplyingModule.GetSetCurrentCapacity /
				                                batterySupplyingModule.CapacityMax);
			}

			var visibility = false;
			var ThisType = typeof(DiagnosticsHUDPowerBar);
			if (HUDHandler.CategoryEnabled.ContainsKey(ThisType)) //So if you join mid round you still have the HUD showing
			{
				visibility = HUDHandler.CategoryEnabled[ThisType];
			}

			diagnosticsHUDHandler.SetVisible(visibility, DiagnosticsHUDHandler.HUDOptions.showPower);
		}


		public void SetVisible(bool newVisible)
		{
			if (gameObject.GetUniversalObjectPhysics().Intangible)
			{
				newVisible = false;
			};
			diagnosticsHUDHandler.SetVisible(newVisible, DiagnosticsHUDHandler.HUDOptions.showPower);
			if (newVisible == false) return;
			if (batterySupplyingModule != null && batterySupplyingModule.CapacityMax != 0)
			{
				diagnosticsHUDHandler.UpdateBar(currentCharge / batterySupplyingModule.CapacityMax);
			}
		}

		public void OnDestroy()
		{
			hudHandler.RemoveHud(this);
			batterySupplyingModule.OnCapacityChangedEvent -= UpdateCharge;
		}
	}
}
