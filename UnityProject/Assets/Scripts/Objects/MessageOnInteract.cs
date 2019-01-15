using UnityEngine;


public class MessageOnInteract : InputTrigger
{
	public string Message;

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		ChatRelay.Instance.AddToChatLogClient(Message, ChatChannel.Examine);
		return true;
	}

}
