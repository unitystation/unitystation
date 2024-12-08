using System;
using System.Collections.Generic;
using Mirror;
using UI.Action;
using UI.Core.Action;
using UnityEngine;

public interface IGameActionHolder
{
	GameObject gameObject { get; }
	/// <summary>
	/// The global key used for tracking an action, stored as a string for client communication, 2 ACTIONS SHOULD NEVER EVER SHARE THE SAME KEY
	/// </summary>
	public string ActionGuid { get; }
	/// <summary>
	/// The container this action is inside
	/// </summary>
//	public IGameActionContainer ActionContainer { get; set; }
	/// <summary>
	/// The mind that owns this action
	/// </summary>
//	public Mind ActionOwner { get; set; }
	bool IsActionAvailable()
	{
		return true;
	}
	ActionData ActionData { get; }

	void CallActionClient() //this gets called when the UI button gets clicked by a player
	{
		UIAction action = UIActionManager.Instance.DicIActionGUI[this][0];
		PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestAction(ActionGuid, action.LastClickPosition);
	}
}

/// <summary>
/// Simply implement this to Implement your Networked screen action
/// </summary>
public interface IServerGameActionHolder : IGameActionHolder
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

public class __ExampleIServerActionGUI__ : IServerGameActionHolder
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
