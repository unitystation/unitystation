using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Allows this object to toggle its electrical node when clicked - turning the supply on or off.
/// </summary>
public class ToggleableElectricalNode : NBHandApplyInteractable, INodeControl
{
  	[SyncVar(hook = "UpdateState")]
	public bool isOn = false;
	public ElectricalNodeControl ElectricalNodeControl;

	protected override InteractionValidationChain<HandApply> InteractionValidationChain()
	{
		return CommonValidationChains.CAN_APPLY_HAND_CONSCIOUS;
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		isOn = !isOn;
		UpdateServerState(isOn);
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		isOn = true;
		UpdateServerState(isOn);
	}
	public void UpdateServerState(bool _isOn)
	{
		if (isOn)
		{
			//Logger.Log("TurnOnSupply");
			ElectricalNodeControl.TurnOnSupply();
		}
		else
		{
			//Logger.Log("TurnOffSupply");
			ElectricalNodeControl.TurnOffSupply();
		}
	}

	public void PowerNetworkUpdate() { }


	public void UpdateState(bool _isOn)
	{
		isOn = _isOn;
		if (isOn)
		{
			//Logger.Log("on");
		}
		else
		{
			//Logger.Log("off");
		}
	}
}
