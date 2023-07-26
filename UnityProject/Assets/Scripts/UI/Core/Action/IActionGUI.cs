using System.Collections.Generic;
using Mirror;
using UnityEngine;

public interface IAction { }

///Using both IActionGUI and IActionGUIMULTI on a script will not work!!!, USE ONLY ONE OF THE Interface Types!!!///

/// <summary>
/// Simply implement this to Implement your Screen action
/// </summary>
public interface IActionGUI : IAction
{
	ActionData ActionData { get; }

	void CallActionClient();
	void CallToggleActionClient(bool toggled);
}


/// <summary>
/// Simply implement this to Implement your Networked screen action
/// </summary>
public interface IServerActionGUI : IActionGUI
{
	void CallActionServer(PlayerInfo playerInfo); //Requires validation in this
	void CallToggleActionServer(PlayerInfo playerInfo, bool toggled); //Requires validation in this
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

	public void CallToggleActionClient(bool toggled)
	{
		//Do action when isToggleble and toggled on or off
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

	public void CallActionServer(PlayerInfo playerInfo)
	{
		//Validation
		//do Action
	}

	public void CallToggleActionClient(bool toggled)
	{
		//Do action when isToggleble and toggled on or off
		//Remember if its networked do validation
	}

	public void CallToggleActionServer(PlayerInfo playerInfo, bool toggled)
	{
		//Validation
		//Do action when isToggleble and toggled on or off
	}
}

/// <summary>
/// Simply implement this to Implement your Screen action
/// </summary>
public interface IActionGUIMulti : IAction
{
	List<ActionData> ActionData { get; }

	void CallActionClient(ActionData data);
	void CallToggleActionClient(ActionData data, bool toggle);
}


/// <summary>
/// Simply implement this to Implement your Networked screen action
/// </summary>
public interface IServerActionGUIMulti : IActionGUIMulti
{
	void CallActionServer(ActionData data, PlayerInfo playerInfo); //Requires validation in this
	void CallToggleActionServer(ActionData data, PlayerInfo playerInfo, bool toggle);
}