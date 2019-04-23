using UnityEngine;


public class MessageOnInteract : InputTrigger
{
	public string Message;

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!CanUse(originator, hand, position, false))
		{
			return false;
		}
		if (!isServer)
		{
			//ask server to perform the interaction
			InteractMessage.Send(gameObject, position, hand);
			return true;
		}

		UpdateChatMessage.Send(originator, ChatChannel.Examine, Message);
		return true;
	}

}
