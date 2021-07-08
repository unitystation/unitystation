using System.Collections;
using System.Collections.Generic;
using Mirror;
using Systems.Electricity.NodeModules;

/// <summary>
/// Allows this object to toggle its electrical node when clicked - turning the supply on or off.
/// </summary>
public class ToggleableElectricalNode : NetworkBehaviour, IInteractable<HandApply>, INodeControl
{
  	[SyncVar(hook = nameof(UpdateState))]
	public bool isOn = false;
	public ElectricalNodeControl ElectricalNodeControl;

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

	public void PowerNetworkUpdate() { }

	public void UpdateState(bool _wasOn, bool _isOn)
	{
		isOn = _isOn;
	}
}
