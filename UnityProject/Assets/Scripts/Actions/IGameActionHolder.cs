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
//	public IGameActionContainer ActionContainer { get; protected set; }
	/// <summary>
	/// The mind that owns this action
	/// </summary>
//	public Mind ActionOwner { get; protected set; }
	/// <summary>
	/// The name of this action
	/// </summary>
//	public string ActionName { get; protected set; }
	/// <summary>
	/// The description of this action
	/// </summary>
//	public string ActionDesc { get; protected set; }
	/// <summary>
	/// The list of sprites that go inside our background
	/// </summary>
//	public List<SpriteDataSO> ActionSprites { get; protected set; }
	/// <summary>
	/// Our list of backgrounds
	/// </summary>
//	public List<SpriteDataSO> ActionBackgrounds { get; protected set; }
	ActionData ActionData { get; }

	virtual void CallActionClient() //clientside, this gets called when the UI button gets clicked by a player
	{
		UIAction action = UIActionManager.Instance.DicIActionGUI[this][0];
		PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestAction(ActionGuid, action.LastClickPosition);
	}

	/// <summary>
	/// First in the action chain, return false to stop the action from activating
	/// </summary>
	virtual bool IsAvailable()
	{
		return true;
	}

	/// <summary>
	/// Second in the action chain, still able to return false to stop activation
	/// </summary>
	virtual bool PreActivate()
	{
		return true;
	}

	/// <summary>
	/// This is where you should put most of the logic to execute when your action is called
	/// </summary>
	virtual bool Activate()
	{
		return true;
	}
}

public interface ICooldownGameActionHolder : IGameActionHolder
{
	virtual void CallActionClient(PlayerInfo playerInfo)
	{

	}
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
