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
		container.Opened = !container.Opened;

		string msg = container.Opened ? "The valve is open." : "The valve is closed.";

		ChatRelay.Instance.AddToChatLogClient(msg, ChatChannel.Local);

		return true;
	}
}