using Logs;
using Mirror;
using UnityEngine;
using US13.Managers.NetworkManagement;
using US13.Objects.Engineering;
using US13.Player.HUDData;
using Util;

namespace US13.Clothing.Eyewear
{
	/// <summary>
	// Present on some APCpowered devices to report power state to the HUD
	// Charge level handled seperately
	/// </summary>
	public class DiagnosticsHUDPowerState : NetworkBehaviour, IHUD
	{
		[field:SerializeField]
		public GameObject Prefab { get; set; }

		public GameObject InstantiatedGameObject { get; set; }


		private DiagnosticsHUDHandler diagnosticsHUDHandler;
		[SerializeField] private HUDHandler hudHandler = null;
		[SerializeField] private APCPoweredDevice apcPoweredDevice = null;

		private PowerState currentPowerState = PowerState.Off;
		public void Awake()
		{
			if (hudHandler == false || apcPoweredDevice == false)
			{
				Loggy.Error("DiagnosticsHUD on machine without hudHandler or apcPoweredDevice!");
			}
			if (CustomNetworkManager.IsServer)
			{
				apcPoweredDevice.OnStateChangeEvent += SetNewPowerStateServer;

			}
			hudHandler.AddNewHud(this);
		}


		public void SetUp()
		{
			diagnosticsHUDHandler = InstantiatedGameObject.GetComponent<DiagnosticsHUDHandler>();
			diagnosticsHUDHandler.UpdateState(currentPowerState);

			var visibility = false;
			var ThisType = typeof(DiagnosticsHUDPowerState);
			if (HUDHandler.CategoryEnabled.ContainsKey(ThisType)) //So if you join mid round you still have the HUD showing
			{
				visibility = HUDHandler.CategoryEnabled[ThisType];
			}


			diagnosticsHUDHandler.SetVisible(visibility, DiagnosticsHUDHandler.HUDOptions.showState);
		}


		public void SetVisible(bool Visible)
		{
			if (gameObject.GetUniversalObjectPhysics().Intangible)
			{
				Visible = false;
			};
			diagnosticsHUDHandler.SetVisible(Visible, DiagnosticsHUDHandler.HUDOptions.showState);
			if (Visible == false) return;
			diagnosticsHUDHandler.UpdateState(currentPowerState);
		}

		public void SetNewPowerStateServer(PowerState oldPowerState, PowerState newPowerState)
		{
			if (apcPoweredDevice.IsSelfPowered) newPowerState = PowerState.On;
			currentPowerState = newPowerState;

			if (oldPowerState != newPowerState)
			{
				diagnosticsHUDHandler.UpdateState(currentPowerState);
			}
		}


		public void OnDestroy()
		{
			hudHandler.RemoveHud(this);
			if (CustomNetworkManager.IsServer && apcPoweredDevice == true)
			{
				apcPoweredDevice.OnStateChangeEvent -= SetNewPowerStateServer;
			}
		}
	}
}
