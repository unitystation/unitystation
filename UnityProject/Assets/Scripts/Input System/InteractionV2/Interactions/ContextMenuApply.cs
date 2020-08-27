
using System;
using UnityEngine;

/// <summary>
/// Encapsulates all of the info needed for handling a ContextMenuApply interaction.
///
/// A ContextMenuyApply interaction occurs when a player activates the
/// context menu of an object (probably by right-clicking it) and clicks on an option.
/// </summary>
public class ContextMenuApply : TargetedInteraction
{
	private static readonly ContextMenuApply Invalid = new ContextMenuApply(null, null, null, null, Intent.Help);

	private readonly string requestedOption;
	public string RequestedOption => requestedOption;

	/// <summary>
	///
	/// </summary>
	/// <param name="performer">the gameobject of the player performing the interaction</param>
	/// <param name="handObject">object in the player's active hand. Null if player's hand is empty.</param>
	/// <param name="targetObject">object that the player right-clicked on</param>
	/// <param name="requestedOption">activated option in target object's right-click context menu.</param>
	protected ContextMenuApply(
			GameObject performer, GameObject handObject, GameObject targetObject, string requestedOption, Intent intent) :
			base(performer, handObject, targetObject, intent)
	{
		this.requestedOption = requestedOption;
	}

	/// <summary>
	/// Creates a ContextMenuApply interaction performed by the local player
	/// activating the specified context menu option of the target object.
	/// </summary>
	/// <param name="targetObject">object targeted by the interaction</param>
	/// <param name="requestedOption">activated option in target object's right-click context menu</param>
	/// <returns>a ContextMenuApply, for activating the specified context menu option of the target object</returns>
	public static ContextMenuApply ByLocalPlayer(GameObject targetObject, string requestedOption)
	{
		if (PlayerManager.LocalPlayerScript.IsGhost) return Invalid;

		return new ContextMenuApply(
				PlayerManager.LocalPlayer, UIManager.Hands.CurrentSlot.ItemObject, targetObject, requestedOption, UIManager.CurrentIntent);
	}

	/// <summary>
	/// For server only. Create a ContextMenuApply interaction initiated by the client.
	/// </summary>
	/// <param name="clientPlayer">gameobject of the client's player</param>
	/// <param name="targetObject">object client is targeting.</param>
	/// <param name="requestedOption">activated option in target object's right-click context menu</param>
	/// <param name="handObject">object in the player's active hand. This parameter is used so
	/// it doesn't need to be looked up again, since it already should've been looked up in
	/// the message processing logic. Should match SentByPlayer.Script.playerNetworkActions.GetActiveHandItem().</param>
	/// <returns>a ContextMenuApply by the client, activating the specified context menu option of the target object</returns>
	public static ContextMenuApply ByClient(
			GameObject clientPlayer, GameObject handObject, GameObject targetObject, string requestedOption, Intent intent)
	{
		return new ContextMenuApply(clientPlayer, handObject, targetObject, requestedOption, intent);
	}
}
