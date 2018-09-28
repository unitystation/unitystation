using UnityEngine;
using UnityEngine.Networking;

public abstract class InputTrigger : NetworkBehaviour
{
	public void Trigger()
	{
		Trigger(transform.position);
	}

	public void Trigger(Vector3 position)
	{
		Interact(position);
	}

	private void Interact(Vector3 position)
	{
		if (PlayerManager.LocalPlayer != null)
		{
			Interact(PlayerManager.LocalPlayerScript.gameObject, position, UIManager.Hands.CurrentSlot.eventName);
		}
	}

	public void Interact(GameObject originator, string hand)
	{
		Interact(originator, transform.position, hand);
	}

	public abstract void Interact(GameObject originator, Vector3 position, string hand);

	/// <Summary>
	/// This is called by the hand button on the hand slots. It can also be expanded for interaction on other slots
	/// Just add a button to the slot and GetComponent<InputTrigger>().UI_Interact(PlayerManager.LocalPlayer, UIManager.CurrentSlot.eventName)
	/// </Summary>
	public virtual void UI_Interact(GameObject originator, string hand) {}
}