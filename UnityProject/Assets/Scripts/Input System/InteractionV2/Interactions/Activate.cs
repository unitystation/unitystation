
using UnityEngine;

/// <summary>
/// Activate interaction. Triggers by pressing the Activate key or clicking the item while it's in the active hand.
/// </summary>
public class Activate : Interaction
{
	private readonly HandSlot handSlot;

	/// <summary>
	/// Hand slot being activated
	/// </summary>
	public HandSlot HandSlot => handSlot;

	/// <summary>
	///
	/// </summary>
	/// <param name="performer">The gameobject of the player activating the item</param>
	/// <param name="activatedObject">Object that is being activated</param>
	/// <param name="handSlot">hand slot that is being activated</param>
	private Activate(GameObject performer, GameObject activatedObject, HandSlot handSlot) :
		base(performer, activatedObject)
	{
		this.handSlot = handSlot;
	}

	/// <summary>
	/// Create an Activate interaction where the local player is activating their active hand item
	/// </summary>
	/// <returns></returns>
	public static Activate ByLocalPlayer()
	{
		return new Activate(PlayerManager.LocalPlayer, UIManager.Hands.CurrentSlot.Item,
			HandSlot.ForName(UIManager.Hands.CurrentSlot.eventName));
	}

	/// <summary>
	/// For server only. Create an activate interaction initiated by the client.
	/// </summary>
	/// <param name="clientPlayer">gameobject of the client's player</param>
	/// <param name="activatedObject">object client is activating.</param>
	/// <param name="handSlot">Player's active hand. This parameter is used so
	/// it doesn't need to be looked up again, since it already should've been looked up in
	/// the message processing logic. Should match HandSlot.ForName(SentByPlayer.Script.playerNetworkActions.activeHand).</param>
	/// <returns></returns>
	public static Activate ByClient(GameObject clientPlayer, GameObject activatedObject, HandSlot handSlot)
	{
		return new Activate(clientPlayer, activatedObject, handSlot);
	}
}
