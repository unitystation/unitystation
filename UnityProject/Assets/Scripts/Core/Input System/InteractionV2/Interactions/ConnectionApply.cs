using UnityEngine;

/// <summary>
/// Useful for placing cables on grid
/// </summary>
public class ConnectionApply : TargetedInteraction
{
	public static readonly ConnectionApply Invalid = new ConnectionApply(null, null,
		null, Connection.NA, Connection.NA, Vector2.zero, null, Intent.Help);

	private readonly ItemSlot handSlot;

	public ItemSlot HandSlot => handSlot;

	/// <summary>
	/// Object being used in hand (same as UsedObject). Returns null if nothing in hand.
	/// </summary>
	public GameObject HandObject => UsedObject;

	private readonly Connection connectionPointA, connectionPointB;

	private readonly Vector2 targetVector;

	/// <summary>
	/// Targeted world position deduced from target vector and performer position.
	/// </summary>
	public Vector2 WorldPositionTarget => (Vector2)Performer.transform.position + targetVector;

	/// <summary>
	/// Vector pointing from the performer to the targeted position. Set to Vector2.zero if aiming at self.
	/// </summary>
	public Vector2 TargetVector => targetVector;


	/// <summary>
	/// Cable connection point
	/// </summary>
	public Connection WireEndA => connectionPointA;
	/// <summary>
	/// Cable connection point
	/// </summary>
	public Connection WireEndB => connectionPointB;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="performer">The gameobject of the player performing the drop interaction</param>
	/// <param name="handObject">Object in the player's active hand. Null if player's hand is empty.</param>
	/// <param name="targetObject">Object that the player clicked on</param>
	/// <param name="startPoint">cable start connection</param>
	/// <param name="endPoint">cable end connection</param>
	/// <param name="worldPositionTarget">position of target tile (world space)</param>
	/// <param name="handSlot">active hand slot that is being used</param>
	private ConnectionApply(GameObject performer, GameObject handObject, GameObject targetObject, Connection startPoint, Connection endPoint, Vector2 targetVec,
		ItemSlot handSlot, Intent intent) :
		base(performer, handObject, targetObject, intent)
	{
		this.targetVector = targetVec;
		this.connectionPointA = startPoint;
		this.connectionPointB = endPoint;
		this.handSlot = handSlot;
	}

	/// <summary>
	/// Creates a CableApply interaction performed by the local player targeting the specified object.
	/// </summary>
	/// <param name="targetObject">object targeted by the interaction, null to target empty space</param>
	/// <param name="wireEndA">cable start connection</param>
	/// <param name="wireEndB">cable end connection</param>
	/// <param name="worldPositionTarget">position of target tile (world space)</param>
	/// <returns></returns>
	public static ConnectionApply ByLocalPlayer(GameObject targetObject, Connection wireEndA, Connection wireEndB, Vector2? targetVector)
	{
		if (PlayerManager.LocalPlayerScript.IsGhost) return Invalid;

		var targetVec = targetVector ?? Camera.main.ScreenToWorldPoint(CommonInput.mousePosition) -
						PlayerManager.LocalPlayer.transform.position;

		return new ConnectionApply(
			PlayerManager.LocalPlayer,
			UIManager.Hands.CurrentSlot.ItemObject,
			targetObject,
			wireEndA,
			wireEndB,
			targetVec,
			UIManager.Instance.hands.CurrentSlot.ItemSlot,
			UIManager.CurrentIntent
		);
	}

	/// <summary>
	/// Creates a CableApply interaction performed by the client targeting the specified object.
	/// </summary>
	/// <param name="targetObject">object targeted by the interaction, null to target empty space</param>
	/// <param name="wireEndA">cable start connection</param>
	/// <param name="wireEndB">cable end connection</param>
	/// <param name="worldPositionTarget">position of target tile (world space)</param>
	/// <param name="handSlot">active hand slot that is being used</param>
	/// <returns></returns>
	public static ConnectionApply ByClient(GameObject clientPlayer, GameObject handObject, GameObject targetObject, Connection startPoint, Connection endPoint, Vector2 targetVec,
		ItemSlot handSlot, Intent intent)
	{
		return new ConnectionApply(
			clientPlayer,
			handObject,
			targetObject,
			startPoint,
			endPoint,
			targetVec,
			handSlot,
			intent
		);
	}
}
