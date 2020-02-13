using Mirror;
using UnityEngine;
/// <summary>
/// Simply implement this to Implement your Screen action
/// </summary>
public interface IActionGUI
{
	ActionData ActionData
	{
		get;
	}
	void CallActionClient();
}


/// <summary>
/// Simply implement this to Implement your Networked screen action
/// </summary>
public interface IServerActionGUI : IActionGUI
{
	void CallActionServer(ConnectedPlayer SentByPlayer); //Requires validation in this
	NetworkIdentity GetNetworkIdentity(); //so since it needs Something to relate scripts
}


public class __ExampleIActionGUI__ : IActionGUI
{
	[SerializeField]
	private ActionData actionData;
	public ActionData ActionData => actionData;

	public void CallActionClient()
	{
		//Do whatever you want
	}
}

public class __ExampleIServerActionGUI__ : IServerActionGUI
{
	[SerializeField]
	private ActionData actionData;
	public ActionData ActionData => actionData;

	public void CallActionClient()
	{
		//Do whatever you want
		//Remember if its networked do validation
	}

	public void CallActionServer(ConnectedPlayer SentByPlayer)
	{
		//Validation
		//do Action
	}
	public NetworkIdentity GetNetworkIdentity()
	{
		//Return the network identity that its going to find it from,
		//**Dont** ues stuff that could change across server and client E.G station matrix
		//return (this.GetComponent<NetworkIdentity>());
		return (null);
	}


}