using System;
using Systems.Electricity.NodeModules;
using Mirror;
using UnityEngine;

namespace Objects.Engineering
{
	[RequireComponent(typeof(ElectricalNodeControl))]
	[RequireComponent(typeof(BatterySupplyingModule))]
	public class SMES : NetworkBehaviour, ICheckedInteractable<HandApply>, INodeControl, IExaminable
	{
		[Tooltip("How often (in seconds) the SMES's charging status should be updated.")]
		[SerializeField]
		[Range(1, 20)]
		private int indicatorUpdatePeriod = 5;

		private ElectricalNodeControl electricalNodeControl;
		private BatterySupplyingModule batterySupplyingModule;

		private SpriteHandler baseSpriteHandler;
		// Overlays
		private SpriteHandler chargingIndicator;
		private SpriteHandler outputEnabledIndicator;
		private SpriteHandler chargeLevelIndicator;

		private bool IsCharging => batterySupplyingModule.ChargingDivider > 0.1f;
		private float MaxCharge => batterySupplyingModule.CapacityMax;
		private float CurrentCharge => batterySupplyingModule.CurrentCapacity;
		private int ChargePercent => Convert.ToInt32(Math.Round(CurrentCharge * 100 / MaxCharge));

		private bool outputEnabled = false;

		private enum SpriteState
		{
			Normal = 0,
			CellsExposed = 1
		}

		private enum OutputEnabledOverlayState
		{
			OutputEnabled = 0,
			SMESNoCells = 1
		}

		private enum ChargingOverlayState
		{
			Discharging = 0,
			Charging = 1
		}

		#region Lifecycle

		private void Awake()
		{
			baseSpriteHandler = transform.GetChild(0).GetComponent<SpriteHandler>();
			chargingIndicator = transform.GetChild(1).GetComponent<SpriteHandler>();
			outputEnabledIndicator = transform.GetChild(2).GetComponent<SpriteHandler>();
			chargeLevelIndicator = transform.GetChild(3).GetComponent<SpriteHandler>();

			electricalNodeControl = GetComponent<ElectricalNodeControl>();
			batterySupplyingModule = GetComponent<BatterySupplyingModule>();
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			UpdateMe();
			UpdateManager.Add(UpdateMe, indicatorUpdatePeriod);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		#endregion Lifecycle

		private void UpdateMe()
		{
			UpdateChargingIndicator();
			UpdateChargeLevelIndicator();
		}

		private void UpdateChargingIndicator()
		{
			if (IsCharging)
			{
				chargingIndicator.ChangeSprite((int) ChargingOverlayState.Charging);
			}
			else
			{
				chargingIndicator.ChangeSprite((int) ChargingOverlayState.Discharging);
			}
		}

		private void UpdateChargeLevelIndicator()
		{
			int chargeIndex = Convert.ToInt32(Math.Round((ChargePercent / 100f) * 4));
			chargeLevelIndicator.ChangeSprite(chargeIndex);
		}

		#region Interaction

		public string Examine(Vector3 worldPos = default)
		{
			UpdateMe();
			return $"The charge indicator shows a {ChargePercent} percent charge. " +
			       $"The power input/output is " +
			       $"{(outputEnabled ? $"enabled, and it seems to {(IsCharging ? "be" : "not be")} charging" : "disabled")}.";
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.TargetObject != gameObject) return false;
			if (interaction.HandObject != null) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			ServerToggleOutputMode();
		}

		#endregion Interaction

		private void ServerToggleOutputMode()
		{
			if (outputEnabled)
			{
				ServerToggleOutputModeOff();
			}
			else
			{
				ServerToggleOutputModeOn();
			}
		}

		private void ServerToggleOutputModeOn()
		{
			outputEnabledIndicator.ChangeSprite((int) OutputEnabledOverlayState.OutputEnabled);
			outputEnabledIndicator.PushTexture();
			electricalNodeControl.TurnOnSupply();
			outputEnabled = true;
		}

		private void ServerToggleOutputModeOff()
		{
			outputEnabledIndicator.PushClear();
			electricalNodeControl.TurnOffSupply();
			outputEnabled = false;
		}

		public void PowerNetworkUpdate() { }
	}
}
