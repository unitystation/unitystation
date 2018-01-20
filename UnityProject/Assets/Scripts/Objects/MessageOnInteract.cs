using JetBrains.Annotations;
using PlayGroups.Input;
using UI;
using UnityEngine;


public class MessageOnInteract : InputTrigger
{
	public string Message;
	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	public override void Interact(GameObject originator, Vector3 position, string hand)
	{
		UIManager.Chat.AddChatEvent(new ChatEvent(Message, ChatChannel.Examine));
	}

}
