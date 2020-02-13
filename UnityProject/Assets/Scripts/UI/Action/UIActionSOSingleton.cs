using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIActionSOSingleton", menuName = "Singleton/UIActionSOSingleton")]
public class UIActionSOSingleton : SingletonScriptableObject<UIActionSOSingleton>
{
	public List<UIActionScriptableObject> uIActions = new List<UIActionScriptableObject>();

	public static Dictionary<string, UIActionScriptableObject> actions = new Dictionary<string, UIActionScriptableObject>();

	private static bool Initialised = false;

	void OnEnable()
	{
		Setup();
	}

	void Setup()
	{
		foreach (var action in uIActions)
		{
			if (actions.ContainsKey(action.name))
			{
				//continue;
				Logger.LogError("There is an UIActionScriptableObject That has the same name as another UIActionScriptableObject > " + action.name);
			}
			actions[action.name] = action;
		}
		Initialised = true;
	}

	public IServerActionGUI ReturnFromName(string action)
	{
		if (!Initialised)
		{
			Setup();
		}

		if (actions.ContainsKey(action))
		{
			return (actions[action] as IServerActionGUI);
		}
		return (null);
	}

	public void ActionCallServer(string action, ConnectedPlayer SentByPlayer)
	{
		if (!Initialised)
		{
			Setup();
		}
		if (actions.ContainsKey(action))
		{
			actions[action].CallActionServer(SentByPlayer);
		}
	}

}
