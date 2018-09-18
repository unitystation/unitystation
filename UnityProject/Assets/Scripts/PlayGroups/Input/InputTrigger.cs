using UnityEngine;
using UnityEngine.Networking;

public abstract class InputTrigger : NetworkBehaviour
{
	public void Trigger ()
	{
		Trigger (transform.position);
	}

	public void Trigger (Vector3 position)
	{
		Interact (position);
	}

	private void Interact (Vector3 position)
	{
		if (PlayerManager.LocalPlayer != null)
		{
			Interact (PlayerManager.LocalPlayerScript.gameObject, position, UIManager.Hands.CurrentSlot.eventName);
		}
	}

	public void Interact (GameObject originator, string hand)
	{
		Interact (originator, transform.position, hand);
	}

	public abstract void Interact (GameObject originator, Vector3 position, string hand);
}