using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SolarPanel : InputTrigger, INodeControl
{
  	[SyncVar(hook = "UpdateState")]
	public bool isOn = false;
	public ElectricalNodeControl ElectricalNodeControl;

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!isServer)
		{
			InteractMessage.Send(gameObject, hand);
		}
		else
		{
			isOn = !isOn;
			UpdateServerState(isOn);
		}
		return true;
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
