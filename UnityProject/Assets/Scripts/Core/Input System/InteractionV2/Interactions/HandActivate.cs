
using UnityEngine;

/// <summary>
/// Hand Activate interaction. Triggers by pressing the Activate key or clicking the item while it's in the active hand.
/// </summary>
public class HandActivate : Interaction
{
	private static readonly HandActivate Invalid = new HandActivate(null, null, null, Intent.Help, null, false);

	private readonly ItemSlot handSlot;

	public bool IsAltClick { get; private set; }

	/// <summary>
	/// Hand slot being activated
	/// </summary>
	public ItemSlot HandSlot => handSlot;

	/// <summary>
	///
	/// </summary>
	/// <param name="performer">The gameobject of the player activating the item</param>
	/// <param name="activatedObject">Object that is being activated</param>
	/// <param name="handSlot">hand slot that is being activated</param>
	private HandActivate(GameObject performer, GameObject activatedObject, ItemSlot handSlot, Intent intent, Mind InPerformerMind, bool isAltClick) :
		base(performer, activatedObject, intent, InPerformerMind)
	{
		this.handSlot = handSlot;
		this.IsAltClick = isAltClick;
	}

	/// <summary>
	/// Create an Activate interaction where the local player is activating their active hand item
	/// </summary>
	/// <returns></returns>
	public static HandActivate ByLocalPlayer()
	{
		if (PlayerManager.LocalPlayerScript.IsNormal == false)
		{
			//hand apply never works when local player
			return HandActivate.Invalid;
		}

		return new HandActivate(PlayerManager.LocalPlayerObject,
			PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot()?.ItemObject,
			PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot(),
			UIManager.CurrentIntent,
			PlayerManager.LocalPlayerScript.Mind,
			KeyboardInputManager.IsAltActionKeyPressed());
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
	public static HandActivate ByClient(GameObject clientPlayer, GameObject activatedObject, ItemSlot handSlot, Intent intent ,Mind InPerformerMind, bool isAltClick)
	{
		return new HandActivate(clientPlayer, activatedObject, handSlot, intent, InPerformerMind, isAltClick);
	}
}
