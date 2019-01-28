using System;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Represents a behavior for a gameobject which can trigger reactions in response to input.
///
/// There are 2 kinds of interactions - an interaction (defined when the mouse is initially clicked) and a drag interaction (defined as when the mouse is
/// being dragged while held down but not during the initial click)
///
/// Keep in mind that (currently) every interaction which is checked on a given update has the ability to disable futher interactions by returning true. This is
/// what most interactions do. If an interaction returns false (meaning interaction checking should continue), the game will proceed checking
/// the other possible interactions that can happen, going down from the top to the bottom layers.
/// </summary>
public abstract class InputTrigger : NetworkBehaviour
{
	/// <summary>
	/// Trigger an interaction with the position set to this transform, the originator set to the LocalPlayer
	/// (if localplayer is not null), and the hand set to the localplayer's current hand.
	/// </summary>
	/// <returns>true if further interactions should be prevented for the current update</returns>
	public bool Trigger()
	{
		return Trigger(transform.position);
	}

	/// <summary>
	/// Trigger an interaction with the position set to the specified position, the originator set to the LocalPlayer
	/// (if localplayer is not null), and the hand set to the localplayer's current hand.
	/// </summary>
	/// <param name="position">position of the interaction</param>
	/// <returns>true if further interactions should be prevented for the current update</returns>
	public bool Trigger(Vector3 position)
	{
		return Interact(position);
	}

	/// <summary>
	/// Trigger an interaction with the position set to this transform's position, with the specified originator and hand.
	/// </summary>
	/// <param name="originator">GameObject of the player initiating the interaction</param>
	/// <param name="hand">hand being used by the originator</param>
	/// <returns>true if further interactions should be prevented for the current update</returns>
	public bool Interact(GameObject originator, string hand) {
		return Interact(originator, transform.position, hand);
	}

	private bool Interact(Vector3 position) {
		if (PlayerManager.LocalPlayer != null) {
			return Interact(PlayerManager.LocalPlayerScript.gameObject, position, UIManager.Hands.CurrentSlot.eventName);
		}

		return false;
	}

	/// <summary>
	/// Trigger a drag interaction with the position set to this transform, the originator set to the LocalPlayer
	/// (if localplayer is not null), and the hand set to the localplayer's current hand.
	/// </summary>
	/// <returns>true if further interactions should be prevented for the current update</returns>
	public bool TriggerDrag() {
		return TriggerDrag(transform.position);
	}

	/// <summary>
	/// Trigger an interaction with the position set to the specified position, the originator set to the LocalPlayer
	/// (if localplayer is not null), and the hand set to the localplayer's current hand.
	/// </summary>
	/// <param name="position">position of the interaction</param>
	/// <returns>true if further interactions should be prevented for the current update</returns>
	public bool TriggerDrag(Vector3 position) {
		return DragInteract(position);
	}

	private bool DragInteract(Vector3 position) {
		if (PlayerManager.LocalPlayer != null) {
			return DragInteract(PlayerManager.LocalPlayerScript.gameObject, position, UIManager.Hands.CurrentSlot.eventName);
		}
		return false;
	}

	/// <summary>
	/// Trigger an interaction, defined as when the mouse is initially clicked (but not while it is being held down and dragged)
	/// </summary>
	/// <param name="originator">game object that is performing the interaction upon this gameobject</param>
	/// <param name="hand">hand of the originator which is being used to perform the interaction</param>
	/// <param name="position">position of the interaction</param>
	/// <returns>true if further interactions should be prevented for the current update</returns>
	public abstract bool Interact(GameObject originator, Vector3 position, string hand);

	//TODO: Document what "position" really is - is it always mouse position? Is it sometimes the position of the object being interacted with?
	/// <summary>
	/// Trigger a drag interaction, defined as when the mouse is currently being held (but not initially clicked) and is dragged over. The default implementation is
	/// that nothing happens.
	/// </summary>
	/// <param name="originator">game object that is performing the interaction upon this gameobject</param>
	/// <param name="hand">hand of the originator which is being used to perform the interaction</param>
	/// <param name="position">position of the interaction</param>
	/// <returns>true if further interactions should be prevented for the current update</returns>
	public virtual bool DragInteract(GameObject originator, Vector3 position, string hand) { return false; }

	/// <Summary>
	/// This is called by the hand button on the hand slots. It can also be expanded for interaction on other slots
	/// Just add a button to the slot and GetComponent<InputTrigger>().UI_Interact(PlayerManager.LocalPlayer, UIManager.CurrentSlot.eventName)
	/// </Summary>
	public virtual void UI_Interact(GameObject originator, string hand) {}
	public virtual void UI_InteractOtherSlot(GameObject originator, GameObject otherHandItem){}


}