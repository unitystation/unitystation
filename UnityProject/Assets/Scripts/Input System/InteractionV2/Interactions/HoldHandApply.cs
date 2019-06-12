
using UnityEngine;

//NOTE: Not currently used, but should be with the next IF2 PR - also it shouldn't be targeted - it should
//always aim at the mouse
/// <summary>
/// Encapsulates all of the info needed for handling hold hand apply interaction.
///
/// A hold hand apply works just like a hand apply, but fires on mouse down and mouse up. Thus, the
/// behavior can do something will the mouse is held, such as firing a burst.
/// </summary>
public class HoldHandApply : TargetedInteraction
{
	private MouseButtonState mouseButtonState;
	/// <summary>
	///
	/// </summary>
	/// <param name="performer">The gameobject of the player performing the drop interaction</param>
	/// <param name="handObject">Object in the player's hand. Null if player's hand is empty.</param>
	/// <param name="targetObject">Object that the player clicked on</param>
	/// <param name="buttonState">state of the mouse button, indicating whether it is being initiated
	/// or ending.</param>
	public HoldHandApply(GameObject performer, GameObject handObject, GameObject targetObject, MouseButtonState buttonState) :
		base(performer, handObject, targetObject)
	{
		this.mouseButtonState = buttonState;
	}
}

/// <summary>
/// represents the paricular state of the mouse button during a HoldHandApply interaction
/// </summary>
public enum MouseButtonState
{
	//button pressed down
	BUTTON_DOWN,
	//button released
	BUTTON_UP
}
