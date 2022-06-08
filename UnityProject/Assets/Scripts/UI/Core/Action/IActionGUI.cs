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
	void CallActionServer(PlayerInfo SentByPlayer); //Requires validation in this
}


public class __ExampleIActionGUI__ : IActionGUI
{
	[SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;

	public void CallActionClient()
	{
		//Do whatever you want
	}
}

public class __ExampleIServerActionGUI__ : IServerActionGUI
{
	[SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;

	public void CallActionClient()
	{
		//Do whatever you want
		//Remember if its networked do validation
	}

	public void CallActionServer(PlayerInfo SentByPlayer)
	{
		//Validation
		//do Action
	}
}