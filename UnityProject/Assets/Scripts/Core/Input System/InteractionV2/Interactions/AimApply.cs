
using System.Linq;
using UnityEngine;

/// <summary>
/// Encapsulates all of the info needed for handling aim apply interactions.
///
/// An AimApply is an interaction where the player is aiming at the current mouse position. The interaction
/// fires continuously while the mouse button is being held down, and fires once on the initial click and
/// also on the release. This is used for things like guns.
/// </summary>
public class AimApply : Interaction
{
	//cache player layermask
	private static LayerMask PLAYER_LAYER_MASK = -1;

	private readonly MouseButtonState mouseButtonState;
	private readonly Vector2 targetVector;
	private readonly ItemSlot handSlot;

	/// <summary>
	/// Hand slot being used.
	/// </summary>
	public ItemSlot HandSlot => handSlot;

	/// <summary>
	/// State of the mouse button when this interaction was triggered
	/// </summary>
	public MouseButtonState MouseButtonState => mouseButtonState;

	/// <summary>
	/// Targeted world position deduced from target vector and performer position.
	/// </summary>
	public Vector2 WorldPositionTarget => (Vector2)Performer.transform.position + targetVector;

	/// <summary>
	/// Vector pointing from the performer to the targeted position. Set to Vector2.zero if aiming at self.
	/// </summary>
	public Vector2 TargetVector => targetVector;

	/// <summary>
	/// Whether player is aiming at themselves.
	/// </summary>
	public bool IsAimingAtSelf => targetVector == Vector2.zero;

	/// <summary>
	///
	/// </summary>
	/// <param name="performer">The gameobject of the player performing the aimed interaction</param>
	/// <param name="handObject">Object in the player's active hand. Null if player's hand is empty.</param>
	/// <param name="handSlot">slot of the active hand</param>
	/// <param name="buttonState">state of the mouse button, indicating whether it is being initiated
	/// or ending.</param>
	/// <param name="targetVector">vector pointing from shooter to the spot they are targeting - should NOT be normalized. Set to
	/// Vector2.zero if aiming at self.</param>
	private AimApply(GameObject performer, GameObject handObject, ItemSlot handSlot, MouseButtonState buttonState, Vector2 targetVector, Intent intent) :
		base(performer, handObject, intent)
	{
		this.targetVector = targetVector;
		this.handSlot = handSlot;
		this.mouseButtonState = buttonState;
	}

	/// <summary>
	/// Create an AimAPply for the local player aiming at the current mouse position with the item in their
	/// active hand slot.
	/// </summary>
	/// <param name="buttonState">state of mouse (initial click vs. being held after a click)</param>
	/// <returns></returns>
	public static AimApply ByLocalPlayer(MouseButtonState buttonState)
	{
		//Check for self aim
		if (PLAYER_LAYER_MASK == -1)
		{
			PLAYER_LAYER_MASK = LayerMask.GetMask("Players");
		}

		var targetVector = (Vector2) MouseUtils.MouseToWorldPos() -
		                   (Vector2) PlayerManager.LocalPlayer.transform.position;
		//check for self aim if target vector is sufficiently small so we can avoid raycast
		var selfAim = false;
		if (targetVector.magnitude < 0.6)
		{
			selfAim = MouseUtils.GetOrderedObjectsUnderMouse(PLAYER_LAYER_MASK,
				go => go == PlayerManager.LocalPlayer).Any();
		}

		return new AimApply(PlayerManager.LocalPlayer, PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot().ItemObject,
			PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot(),
			buttonState,
			selfAim ? Vector2.zero : targetVector, UIManager.CurrentIntent);
	}

	/// <summary>
	/// For server only. Create an aim apply interaction initiated by the client.
	/// </summary>
	/// <param name="clientPlayer">gameobject of the client's player</param>
	/// <param name="targetVector">target vector pointing from clientPlayer to the position they are targeting.</param>
	/// <param name="handObject">object in the player's active hand. This parameter is used so
	/// it doesn't need to be looked up again, since it already should've been looked up in
	/// the message processing logic. Should match SentByPlayer.Script.playerNetworkActions.GetActiveHandItem().</param>
	/// <param name="handSlot">Player's active hand. This parameter is used so
	/// it doesn't need to be looked up again, since it already should've been looked up in
	/// the message processing logic. Should match HandSlot.ForName(SentByPlayer.Script.playerNetworkActions.activeHand).</param>
	/// <returns>a hand apply by the client, targeting the specified object with the item in the active hand</returns>
	/// <param name="mouseButtonState">state of the mouse button</param>
	public static AimApply ByClient(GameObject clientPlayer, Vector2 targetVector, GameObject handObject, ItemSlot handSlot, MouseButtonState mouseButtonState,
		Intent intent)
	{
		return new AimApply(clientPlayer, handObject, handSlot,  mouseButtonState, targetVector, intent);
	}
}

/// <summary>
/// represents the paricular state of the mouse button during a AimApply interaction
/// </summary>
public enum MouseButtonState
{
	//button pressed down
	PRESS = 0,
	//button being held down
	HOLD = 1
}
