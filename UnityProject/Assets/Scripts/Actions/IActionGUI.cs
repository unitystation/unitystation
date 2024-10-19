using System;
using System.Collections.Generic;
using Mirror;
using UI.Core.Action;
using UnityEngine;

public interface IGameActionHolder
{
	GameObject gameObject { get; }
	/// <summary>
	/// The global key used for tracking an action, stored as a string for client communication, 2 ACTIONS SHOULD NEVER EVER SHARE THE SAME KEY
	/// </summary>
	public string ActionGuid { get; }
}

///Using both IGameActionHolderSingle and IActionGUIMULTI on a script will not work!!!, USE ONLY ONE OF THE Interface Types!!!///

public interface IGameActionHolderSingle : IGameActionHolder
{
	ActionData ActionData { get; }

	void CallActionClient();
}

/// <summary>
/// Simply implement this to Implement your Networked screen action
/// </summary>
public interface IServerActionGUI : IGameActionHolderSingle
{
	void CallActionServer(PlayerInfo playerInfo); //Requires validation in this
}

//some example classes
/*
public class __ExampleIActionGUI__ : IGameActionHolderSingle
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
