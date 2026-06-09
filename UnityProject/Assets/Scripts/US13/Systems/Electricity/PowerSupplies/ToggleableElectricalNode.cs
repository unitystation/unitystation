using System;
using Mirror;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Objects.Engineering;
using US13.Systems.Electricity.Interfaces;
using US13.Systems.Electricity.NodeModules;

namespace US13.Systems.Electricity.PowerSupplies
{
	/// <summary>
	/// Allows this object to toggle its electrical node when clicked - turning the supply on or off.
	/// </summary>
	public class ToggleableElectricalNode : NetworkBehaviour, IInteractable<HandApply>, INodeControl
	{
		[SyncVar(hook = nameof(UpdateState))]
		public bool isOn = false;
		public ElectricalNodeControl ElectricalNodeControl;

		public event Action<PowerState, PowerState> OnStateChangeEvent;
		private PowerState currentPowerState = PowerState.Off;

		public void ServerPerformInteraction(HandApply interaction)
		{
			isOn = !isOn;
			UpdateServerState();
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			isOn = true;
			UpdateServerState();
		}

		public void UpdateServerState()
		{
			if (isOn)
			{
				ElectricalNodeControl.TurnOnSupply();
			}
			else
			{
				ElectricalNodeControl.TurnOffSupply();
			}
		}

		public void PowerNetworkUpdate()
		{
			SetPowerStateFromVoltage();
		}
		public PowerState SetPowerStateFromVoltage()
		{
			PowerState newState = currentPowerState;

			if (isOn == false) newState = PowerState.Off;
			else newState = PowerState.On;

			if (newState == currentPowerState) return currentPowerState;
			OnStateChangeEvent?.Invoke(currentPowerState, newState);
			currentPowerState = newState;
			return currentPowerState;
		}

		public void UpdateState(bool _wasOn, bool _isOn)
		{
			isOn = _isOn;
		}
	}
}
