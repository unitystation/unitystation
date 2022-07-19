using UnityEngine;

/// <summary>
/// HandApply but with a vector2 which indicates the exact position the player
/// is trying to click (transmitted as a target vector pointing from the player to the
/// spot they are clicking). Useful for objects which have different logic based on where you click,
/// such as InteractableTiles). Also fires when clicking on empty / open space.
/// </summary>
public class PositionalHandApply : HandApply
{
	private static readonly PositionalHandApply Invalid
			= new PositionalHandApply(null, null, null, Vector2.zero, null, Intent.Help, BodyPartType.None, false);

	/// <summary>Target position, the Local position that the performer is pointing at</summary>
	public Vector2 TargetPosition { get; protected set; }

	/// <summary>Vector pointing from the performer's position to the target position.</summary>
	public Vector2 TargetVector => WorldPositionTarget.To3() - Performer.RegisterTile().WorldPosition;

	/// <summary>Target world position calculated from matrix local position.</summary>
	public Vector2 WorldPositionTarget => (Vector2) TargetPosition.To3().ToWorld(Performer.RegisterTile().Matrix);

	/// <param name="performer">The gameobject of the player performing the drop interaction</param>
	/// <param name="handObject">Object in the player's active hand. Null if player's hand is empty.</param>
	/// <param name="targetVector">vector pointing from performer position to the spot they are targeting</param>
	/// <param name="targetObject">Object that the player clicked on</param>
	/// <param name="handSlot">active hand slot that is being used.</param>
	private PositionalHandApply(GameObject performer, GameObject handObject, GameObject targetObject, Vector2 targetPosition,
		ItemSlot handSlot, Intent intent, BodyPartType targetBodyPart, bool isAltClick) :
		base(performer, handObject, targetObject, targetBodyPart, handSlot, intent, isAltClick)
	{
		TargetPosition = targetPosition;
	}

	/// <summary>
	/// Creates a PositionalHandApply interaction performed by the local player targeting the specified object.
	/// </summary>
	/// <param name="targetObject">object targeted by the interaction, null to target empty space</param>
	/// <param name="targetVector">Target position, the Local position that the performer is pointing at , Defaults
	/// to where the mouse currently is.</param>
	/// <returns></returns>
	public static PositionalHandApply ByLocalPlayer(GameObject targetObject, Vector2? IntargePosition = null)
	{
		if (PlayerManager.LocalPlayerScript.IsNormal == false) return Invalid;

		var targePosition = IntargePosition ?? MouseUtils.MouseToWorldPos().ToLocal(PlayerManager.LocalPlayerObject.RegisterTile().Matrix);

		return new PositionalHandApply(
				PlayerManager.LocalPlayerObject,
				PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot()?.ItemObject,
				targetObject,
				targePosition,
				PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot(),
				UIManager.CurrentIntent,
				UIManager.DamageZone,
				KeyboardInputManager.IsAltActionKeyPressed());
	}

	/// <summary>
	/// For server only. Create a positional hand apply interaction initiated by the client.
	/// </summary>
	/// <param name="clientPlayer">gameobject of the client's player</param>
	/// <param name="targetObject">object client is targeting.</param>
	/// <param name="Targetposition">Target position, the Local position that the performer is pointing at</param>
	/// <param name="handObject">object in the player's active hand. This parameter is used so
	/// it doesn't need to be looked up again, since it already should've been looked up in
	/// the message processing logic. Should match SentByPlayer.Script.playerNetworkActions.GetActiveHandItem().</param>
	/// <param name="handSlot">Player's active hand. This parameter is used so
	/// it doesn't need to be looked up again, since it already should've been looked up in
	/// the message processing logic. Should match SentByPlayer.Script.playerNetworkActions.activeHand.</param>
	/// <returns>a hand apply by the client, targeting the specified object with the item in the active hand</returns>
	public static PositionalHandApply ByClient(GameObject clientPlayer, GameObject handObject, GameObject targetObject,
			Vector2 TargetPosition, ItemSlot handSlot, Intent intent, BodyPartType targetBodyPart, bool isAltClick)
	{
		return new PositionalHandApply(clientPlayer, handObject, targetObject, TargetPosition, handSlot, intent, targetBodyPart, isAltClick);
	}
}
