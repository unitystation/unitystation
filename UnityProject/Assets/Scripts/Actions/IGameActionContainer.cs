using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is used for the highest level object that an action would be held by(ex: item actions go to mob and spells go to mind), actual access to actions is only controlled by mind
/// </summary>
public interface IGameActionContainer
{
    public Dictionary<string, IGameActionHolder> OwnedActions {get; set;}


	void SetUp(List<IGameActionHolder> gainedActions)
	{
		foreach (var action in gainedActions)
		{
			GainAction(action);
		}
	}

    void GainAction(IGameActionHolder addedHolder)
    {
		OwnedActions[addedHolder.ActionGuid] = addedHolder;
    }
}
