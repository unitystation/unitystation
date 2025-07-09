using System;
using System.Collections;
using System.Collections.Generic;
using GameActions;
using Mirror;
using UI.Core.Action;
using UnityEngine;

/// <summary>
/// Used for if an action needs to be a self-contained object, EG spells
/// </summary>
public class GameActionObject : NetworkBehaviour, IGameActionHolder
{
	public string ActionGuid => UIActionManager.RegisterAction(this);

    [SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;

	public event EventHandler<GameObject> OnDestroyed;

	public void OnDestroy()
	{
		OnActionDestroy();
		OnDestroyed?.Invoke(this, gameObject);
		OnDestroyed = null;
	}

	protected virtual void OnActionDestroy() {}
}
