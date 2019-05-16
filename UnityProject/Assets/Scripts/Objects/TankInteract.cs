using Objects;
using UnityEngine;

public class TankInteract : InputTrigger
{
	private GasContainer container;

	private void Awake()
	{
		container = GetComponent<GasContainer>();
	}

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if(!CanUse(originator, hand, position, false)){
			return false;
		}
		if(!isServer){
			//ask server to perform the interaction
			InteractMessage.Send(gameObject, position, hand);
			return true;
		}

		container.Opened = !container.Opened;

		string msg = container.Opened ? $"The valve is open, outputting at {container.ReleasePressure} kPa." : "The valve is closed.";
		UpdateChatMessage.Send(originator, ChatChannel.Examine, msg);

		return true;
	}
}