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
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
