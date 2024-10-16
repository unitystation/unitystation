using System;
using System.Collections.Generic;
using Mirror;
using UI.Core.Action;
using UnityEngine;

public interface IGameActionHolder : IServerDespawn
{
	/// <summary>
	/// The global key used for tracking an action, stored as a string for client communication, 2 ACTIONS SHOULD NEVER EVER SHARE THE SAME KEY
	/// </summary>
	public string ActionGuid {get;}

	ActionData ActionData { get; }

	void CallActionClient();

	void OnDespawnServer(DespawnInfo info)
	{
		UIActionManager.UnregisterAction(this);
	}
}

///Using both IGameActionHolder and IActionGUIMULTI on a script will not work!!!, USE ONLY ONE OF THE Interface Types!!!///

/// <summary>
/// Simply implement this to Implement your Networked screen action
/// </summary>
public interface IServerActionGUI : IGameActionHolder
{
	void CallActionServer(PlayerInfo playerInfo); //Requires validation in this
}

//some example classes
/*
public class __ExampleIActionGUI__ : IGameActionHolder
{
	[SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;
	public int ActionKey => UIActionManager.RegisterAction(this);

	public void CallActionClient()
	{
		Do whatever you want
	}
}

public class __ExampleIServerActionGUI__ : IServerActionGUI
{
	[SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;

	public void CallActionClient()
	{
		Do whatever you want
		Remember if its networked do validation
	}

	public void CallActionServer(PlayerInfo playerInfo)
	{
		Validation
		do Action
	}
}*/

/// <summary>
/// Simply implement this to Implement your Screen action
/// </summary>
public interface IActionGUIMulti : IGameActionHolder
{
	List<ActionData> ActionData { get; }

	void CallActionClient(ActionData data);
}


/// <summary>
/// Simply implement this to Implement your Networked screen action
/// </summary>
public interface IServerActionGUIMulti : IActionGUIMulti
{
	void CallActionServer(ActionData data, PlayerInfo playerInfo); //Requires validation in this
}