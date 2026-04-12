using Logs;
using Mirror;
using NaughtyAttributes;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers.NetworkManagement;
using US13.Objects.Engineering;
using US13.Player.HUDData;
using US13.Systems.Electricity.Interfaces;
using Util;

namespace US13.Clothing.Eyewear
{
	/// <summary>
	// Present on some APCpowered devices to report power state to the HUD
	// Charge level handled seperately
	/// </summary>
	public class DiagnosticsHUDPowerState : NetworkBehaviour, IHUD, IServerSpawn
	{
		private enum StateController
		{
			apcPoweredDevice = 0,
			powerNode = 1,
		}
		[field:SerializeField]
		public GameObject Prefab { get; set; }

		public GameObject InstantiatedGameObject { get; set; }


		private DiagnosticsHUDHandler diagnosticsHUDHandler;
		[SerializeField] private HUDHandler hudHandler = null;

		public bool IsApcPowered => (powerSource == StateController.apcPoweredDevice);
		[SerializeField] private StateController powerSource = StateController.apcPoweredDevice;

		[SerializeField, ShowIf(nameof(IsApcPowered))] private APCPoweredDevice apcPoweredDevice = null;
		[SerializeField, HideIf(nameof(IsApcPowered))] private MonoBehaviour powerNodeMono = null;
		private INodeControl PowerNode => powerNodeMono as INodeControl;

		[SyncVar(hook = nameof(SyncCurrentPowerState))]
		private PowerState currentPowerState = PowerState.Off;
		public PowerState CurrentPowerState => currentPowerState;

		private void SyncCurrentPowerState(PowerState oldState, PowerState newState)
		{
			if (newState == oldState) return;
			diagnosticsHUDHandler?.UpdateState(newState);
			currentPowerState = newState;
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if(IsApcPowered) SyncCurrentPowerState(PowerState.Off, apcPoweredDevice.State);
			else SyncCurrentPowerState(PowerState.Off, PowerNode.SetPowerStateFromVoltage());
		}

		private void EnsureInit()
		{
			if (IsApcPowered && apcPoweredDevice == false) apcPoweredDevice = GetComponent<APCPoweredDevice>();
			else if (IsApcPowered == false) powerNodeMono = gameObject.GetComponent<INodeControl>() as MonoBehaviour;
		}

		public override void OnStartClient()
		{
			EnsureInit();
			if(IsApcPowered) SyncCurrentPowerState(PowerState.Disconnected, apcPoweredDevice.State);
			else SyncCurrentPowerState(PowerState.Disconnected, PowerNode.SetPowerStateFromVoltage());

			base.OnStartClient();
		}

		public void Awake()
		{
			if (hudHandler == false)
			{
				Loggy.Error("DiagnosticsHUDPowerState could not find HUDHandler for machine.");
				return;
			}

			if (IsApcPowered)
			{
				if (apcPoweredDevice == null)
				{
					Loggy.Error("DiagnosticsHUDPowerState on machine expected an apcPoweredDevice but none was serialized!");
					return;
				}
				if (CustomNetworkManager.IsServer) apcPoweredDevice.OnStateChangeEvent += SetNewPowerStateServer;
			}
			else
			{
				if (PowerNode == null)
				{
					Loggy.Error("DiagnosticsHUDPowerState on machine expected an INodeControl but none was serialized!");
					return;
				}

				if (CustomNetworkManager.IsServer) PowerNode.OnStateChangeEvent += SetNewPowerStateServer;
			}

			hudHandler.AddNewHud(this);
		}


		public void SetUp()
		{
			diagnosticsHUDHandler = InstantiatedGameObject.GetComponent<DiagnosticsHUDHandler>();

			var visibility = false;
			var ThisType = typeof(DiagnosticsHUDPowerState);
			if (HUDHandler.CategoryEnabled.ContainsKey(ThisType)) //So if you join mid round you still have the HUD showing
			{
				visibility = HUDHandler.CategoryEnabled[ThisType];
			}
			diagnosticsHUDHandler?.SetVisible(visibility, DiagnosticsHUDHandler.HUDOptions.showState);
		}


		public void SetVisible(bool Visible)
		{
			if (gameObject.GetUniversalObjectPhysics().Intangible) Visible = false;

			diagnosticsHUDHandler?.SetVisible(Visible, DiagnosticsHUDHandler.HUDOptions.showState);
			if (Visible) diagnosticsHUDHandler?.UpdateState(CurrentPowerState);
		}

		public void SetNewPowerStateServer(PowerState oldPowerState, PowerState newPowerState)
		{
			if (IsApcPowered && apcPoweredDevice.IsSelfPowered) newPowerState = PowerState.On;
			SyncCurrentPowerState(oldPowerState, newPowerState);
		}


		public void OnDestroy()
		{
			hudHandler.RemoveHud(this);
			if (CustomNetworkManager.IsServer == false) return;
			if (apcPoweredDevice == true)
			{
				apcPoweredDevice.OnStateChangeEvent -= SetNewPowerStateServer;
			}

			if (PowerNode != null)
			{
				PowerNode.OnStateChangeEvent -= SetNewPowerStateServer;
			}
		}
	}
}
