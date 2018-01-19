using PlayGroups.Input;
using UI;
using UnityEngine;


public class AtmosMonitorTrigger : InputTrigger
{

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	public override void Interact(GameObject originator, Vector3 position, string hand)
	{
		UIManager.Chat.AddChatEvent(new ChatEvent("Atmospheric levels Nominal", ChatChannel.Examine));
	}

}
