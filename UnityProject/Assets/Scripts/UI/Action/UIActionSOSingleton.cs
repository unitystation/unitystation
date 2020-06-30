using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "UIActionSOSingleton", menuName = "Singleton/UIActionSOSingleton")]
public class UIActionSOSingleton : SingletonScriptableObject<UIActionSOSingleton>
{
	public List<UIActionScriptableObject> uIActions = new List<UIActionScriptableObject>();

	public static Dictionary<ushort, UIActionScriptableObject> IDtoActions = new Dictionary<ushort, UIActionScriptableObject>();
	public static Dictionary<UIActionScriptableObject, ushort> ActionsTOID = new Dictionary<UIActionScriptableObject, ushort>();

	private static bool Initialised = false;

	void OnEnable()
	{
		Setup();
	}

	void Setup()
	{
		ushort ID = 1;
		var alphabeticaluIActions= uIActions.OrderBy(X => X.name);

		foreach (var action in alphabeticaluIActions)
		{
			IDtoActions[ID] = action;
			ActionsTOID[action] = ID;
			ID++;
		}
		Initialised = true;
	}

	public IActionGUI FromID(ushort ID)
	{
		if (!Initialised)
		{
			Setup();
		}

		if (IDtoActions.ContainsKey(ID))
		{
			return (IDtoActions[ID] as IServerActionGUI);
		}
		return (null);
	}

	public void ActionCallServer(ushort ID, ConnectedPlayer SentByPlayer)
	{
		if (!Initialised)
		{
			Setup();
		}
		if (IDtoActions.ContainsKey(ID))
		{
			IDtoActions[ID].CallActionServer(SentByPlayer);
		}
	}

}
