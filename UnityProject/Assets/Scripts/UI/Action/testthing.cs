using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class testthing : MonoBehaviour, IServerActionGUI, IInteractable<HandActivate>
{
	[SerializeField]
	private ActionData actionData;
	public ActionData ActionData => actionData;

	public UIActionScriptableObject thing;

	public bool yay;

	public void Start() { 
		UIActionManager.Instance.SetAction(this, yay);
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		Logger.Log("oh?");
		yay = !yay;

		//SetActionUI.SetAction(PlayerManager.LocalPlayerScript.gameObject, this, yay);
		//UIActionManager.Instance.SetAction(this, yay);

		SetActionUI.SetAction(PlayerManager.LocalPlayerScript.gameObject, thing, yay);
		//UIActionManager.Instance.SetAction(this, yay);
	}


	public void CallActionClient()
	{
		//Do whatever you want
		//Remember if its networked do validation
		Logger.Log("CallActionClientTT");
	}

	public void CallActionServer(ConnectedPlayer SentByPlayer)
	{
		Logger.Log("CallActionServerTT");
		//Validation
		//do Action
	}
	public NetworkIdentity GetNetworkIdentity()
	{
		//Return the network identity that its going to find it from,
		//**Dont** ues stuff that could change across server and client E.G station matrix
		return (this.GetComponent<NetworkIdentity>());
	}
}
