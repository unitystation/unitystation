using System.Collections;
using System.Collections.Generic;
using UI.Core.Action;
using UnityEngine;

/// <summary>
/// Used for if an action needs to be a self contained object, EG spells
/// </summary>
public class GameActionObject : MonoBehaviour, IGameActionHolder
{
	public string ActionGuid => UIActionManager.RegisterAction(this);

    [SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;
}
