using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "UIActionScriptableObject", menuName = "ScriptableObjects/UIActionSO")]
public class UIActionScriptableObject : ScriptableObject, IServerActionGUI, IServerActionGUIMulti
{
	[SerializeField]
	private ActionData actionData = null;

	[SerializeField]
	private List<ActionData> multiActionData = new List<ActionData>();
	public ActionData ActionData => actionData;

	public void CallActionClient(ActionData data)
	{
		Loggy.Log("CallActionClient SO", Category.UserInput);
		//Do whatever you want
		//Remember if its networked do validationNot just
	}

	public void CallActionServer(ActionData data, PlayerInfo playerInfo)
	{
		Loggy.Log("CallActionServer SO", Category.UserInput);
		//Validation
		//do Action
	}

	public virtual void CallActionClient()
	{
		Loggy.Log("CallActionClient SO", Category.UserInput);
		//Do whatever you want
		//Remember if its networked do validationNot just
	}

	public virtual void CallActionServer(PlayerInfo playerInfo)
	{
		Loggy.Log("CallActionServer SO", Category.UserInput);
		//Validation
		//do Action
	}
	public NetworkIdentity GetNetworkIdentity()
	{
		Loggy.Log("GetNetworkIdentity SO", Category.UserInput);
		//Return the network identity that its going to find it from,
		//**Dont** ues stuff that could change across server and client E.G station matrix
		return (null);
	}

	public void Awake()
	{
#if UNITY_EDITOR
		{
			if (UIActionSOSingleton.Instance == null)
			{
				Resources.LoadAll<UIActionSOSingleton>("ScriptableObjectsSingletons");
			}
			if (!UIActionSOSingleton.Instance.uIActions.Contains(this))
			{
				UIActionSOSingleton.Instance.uIActions.Add(this);
			}

		}
#endif
	}

	List<ActionData> IActionGUIMulti.ActionData => multiActionData;
}
