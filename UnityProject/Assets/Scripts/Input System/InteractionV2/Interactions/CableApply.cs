using UnityEngine;

/// <summary>
/// Useful for placing cables on grid
/// </summary>
public class CableApply : BodyPartTargetedInteraction
{
	public static readonly CableApply Invalid = new CableApply(null, null,
		null, Connection.NA, Connection.NA, Vector2.zero, null, Intent.Help, BodyPartType.None);

	private readonly ItemSlot handSlot;

	public ItemSlot HandSlot => handSlot;

	/// <summary>
	/// Object being used in hand (same as UsedObject). Returns null if nothing in hand.
	/// </summary>
	public GameObject HandObject => UsedObject;

	private Vector2 tileWorldPosition;
	private readonly Connection wireEndA, wireEndB;

	/// <summary>
	/// Targeted tile position (world space).
	/// </summary>
	public Vector2 WorldPositionTarget => tileWorldPosition;

	/// <summary>
	/// Cable connection point
	/// </summary>
	public Connection WireEndA => wireEndA;
	/// <summary>
	/// Cable connection point
	/// </summary>
	public Connection WireEndB => wireEndB;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="performer">The gameobject of the player performing the drop interaction</param>
	/// <param name="handObject">Object in the player's active hand. Null if player's hand is empty.</param>
	/// <param name="targetObject">Object that the player clicked on</param>
	/// <param name="startPoint">cable start connection</param>
	/// <param name="endPoint">cable end connection</param>
	/// <param name="tileWorldPosition">position of target tile (world space)</param>
	/// <param name="handSlot">active hand slot that is being used</param>
	private CableApply(GameObject performer, GameObject handObject, GameObject targetObject, Connection startPoint, Connection endPoint, Vector2 tileWorldPosition,
		ItemSlot handSlot, Intent intent, BodyPartType targetBodyPart) :
		base(performer, handObject, targetObject, targetBodyPart, intent)
	{
		this.tileWorldPosition = tileWorldPosition;
		this.wireEndA = startPoint;
		this.wireEndB = endPoint;
		this.handSlot = handSlot;
	}

	/// <summary>
	/// Creates a CableApply interaction performed by the local player targeting the specified object.
	/// </summary>
	/// <param name="targetObject">object targeted by the interaction, null to target empty space</param>
	/// <param name="wireEndA">cable start connection</param>
	/// <param name="wireEndB">cable end connection</param>
	/// <param name="tileWorldPosition">position of target tile (world space)</param>
	/// <returns></returns>
	public static CableApply ByLocalPlayer(GameObject targetObject, Connection wireEndA, Connection wireEndB, Vector2 tileWorldPosition)
	{
		if (PlayerManager.LocalPlayerScript.IsGhost) return Invalid;

		return new CableApply(
			PlayerManager.LocalPlayer,
			UIManager.Hands.CurrentSlot.ItemObject,
			targetObject,
			wireEndA,
			wireEndB,
			tileWorldPosition,
			UIManager.Instance.hands.CurrentSlot.ItemSlot,
			UIManager.CurrentIntent,
			UIManager.DamageZone
		);
	}

	/// <summary>
	/// Creates a CableApply interaction performed by the client targeting the specified object.
	/// </summary>
	/// <param name="targetObject">object targeted by the interaction, null to target empty space</param>
	/// <param name="wireEndA">cable start connection</param>
	/// <param name="wireEndB">cable end connection</param>
	/// <param name="tileWorldPosition">position of target tile (world space)</param>
	/// <param name="handSlot">active hand slot that is being used</param>
	/// <returns></returns>
	public static CableApply ByClient(GameObject clientPlayer, GameObject handObject, GameObject targetObject, Connection startPoint, Connection endPoint, Vector2 tileWorldPosition,
		ItemSlot handSlot, Intent intent, BodyPartType targetBodyPart)
	{
		return new CableApply(
			clientPlayer,
			handObject,
			targetObject,
			startPoint,
			endPoint,
			tileWorldPosition,
			handSlot,
			intent,
			targetBodyPart
		);
	}
}
