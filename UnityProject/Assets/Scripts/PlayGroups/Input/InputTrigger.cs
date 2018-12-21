using System;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Represents a behavior for a gameobject which can trigger reactions in response to input.
///
/// There are 2 kinds of interactions - an interaction (defined when the mouse is initially clicked) and a drag interaction (defined as when the mouse is
/// being dragged while held down but not during the initial click)
///
/// Keep in mind that (currently) only at most one interaction will be triggered for a given update, and the interaction will first be attempted on the highest layer
/// before checking for interactions on lower layers. The first interaction which returns true will cause no further interactions to be checked.
/// </summary>
public abstract class InputTrigger : NetworkBehaviour
{
	/// <summary>
	/// Trigger an interaction with the position set to this transform, the originator set to the LocalPlayer
	/// (if localplayer is not null), and the hand set to the localplayer's current hand.
	/// </summary>
	public void Trigger()
	{
		Trigger(transform.position);
	}

	/// <summary>
	/// Trigger an interaction with the position set to the specified position, the originator set to the LocalPlayer
	/// (if localplayer is not null), and the hand set to the localplayer's current hand.
	/// </summary>
	/// <param name="position">position of the interaction</param>
	public void Trigger(Vector3 position)
	{
		Interact(position);
	}

	/// <summary>
	/// Trigger an interaction with the position set to this transform's position, with the specified originator and hand.
	/// </summary>
	/// <param name="hand">hand being used by the originator</param>
	public void Interact(GameObject originator, string hand) {
		Interact(originator, transform.position, hand);
	}

	private void Interact(Vector3 position) {
		if (PlayerManager.LocalPlayer != null) {
			Interact(PlayerManager.LocalPlayerScript.gameObject, position, UIManager.Hands.CurrentSlot.eventName);
		}
	}

	/// <summary>
	/// Trigger a drag interaction with the position set to this transform, the originator set to the LocalPlayer
	/// (if localplayer is not null), and the hand set to the localplayer's current hand.
	/// </summary>
	public void TriggerDrag() {
		TriggerDrag(transform.position);
	}

	/// <summary>
	/// Trigger an interaction with the position set to the specified position, the originator set to the LocalPlayer
	/// (if localplayer is not null), and the hand set to the localplayer's current hand.
	/// </summary>
	/// <param name="position">position of the interaction</param>
	public void TriggerDrag(Vector3 position) {
		DragInteract(position);
	}

	private void DragInteract(Vector3 position) {
		if (PlayerManager.LocalPlayer != null) {
			DragInteract(PlayerManager.LocalPlayerScript.gameObject, position, UIManager.Hands.CurrentSlot.eventName);
		}
	}

	/// <summary>
	/// Trigger an interaction, defined as when the mouse is initially clicked (but not while it is being held down and dragged)
	/// </summary>
	/// <param name="originator>game object that is performing the interaction upon this gameobject</param>
	/// <param name="hand">hand of the originator which is being used to perform the interaction</param>
	/// <param name="position">position of the interaction</param>
	public abstract void Interact(GameObject originator, Vector3 position, string hand);

	//TODO: Document what "position" really is - is it always mouse position? Is it sometimes the position of the object being interacted with?
	/// <summary>
	/// Trigger a drag interaction, defined as when the mouse is currently being held (but not initially clicked) and is dragged over. The default implementation is
	/// that nothing happens.
	/// </summary>
	/// <param name="originator>game object that is performing the interaction upon this gameobject</param>
	/// <param name="hand">hand of the originator which is being used to perform the interaction</param>
	/// <param name="position">position of the interaction</param>
	public virtual void DragInteract(GameObject originator, Vector3 position, string hand) { }

	/// <Summary>
	/// This is called by the hand button on the hand slots. It can also be expanded for interaction on other slots
	/// Just add a button to the slot and GetComponent<InputTrigger>().UI_Interact(PlayerManager.LocalPlayer, UIManager.CurrentSlot.eventName)
	/// </Summary>
	public virtual void UI_Interact(GameObject originator, string hand) {}
	public virtual void UI_InteractOtherSlot(GameObject originator, GameObject otherHandItem){}

	
}