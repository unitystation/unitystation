using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update the progress bar for crafting
/// </summary>
public class ProgressBarMessage : ServerMessage
{
	public uint Recipient;
	public int SpriteIndex;
	public Vector2Int OffsetFromPlayer;
	public int ProgressBarID;

	public override void Process()
	{
		LoadNetworkObject(Recipient);

		var bar = UIManager.GetProgressBar(ProgressBarID);

		//bar not found, so create it unless we are the server (in which case it would already be created)
		if (bar == null && !CustomNetworkManager.IsServer)
		{
			Logger.LogTraceFormat("Client progress bar ID {0} not found, creating it.", Category.ProgressAction, ProgressBarID);
			bar = UIManager.CreateProgressBar(OffsetFromPlayer, ProgressBarID);
		}

		if (bar != null)
		{
			bar.ClientUpdateProgress(SpriteIndex);
		}
	}

	/// <summary>
	/// Sends the message to create the progress bar client side
	/// </summary>
	/// <param name="recipient"></param>
	/// <param name="spriteIndex"></param>
	/// <param name="offsetFromPlayer">offset from player performing the progress action</param>
	/// <param name="progressBarID"></param>
	/// <returns></returns>
	public static ProgressBarMessage SendCreate(GameObject recipient, int spriteIndex, Vector2Int offsetFromPlayer, int progressBarID)
	{
		ProgressBarMessage msg = new ProgressBarMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId,
			SpriteIndex = spriteIndex,
			OffsetFromPlayer = offsetFromPlayer,
			ProgressBarID = progressBarID
		};
		msg.SendTo(recipient);
		return msg;
	}

	/// <summary>
	/// Sends the message to update the progress bar with the specified id
	/// </summary>
	/// <param name="recipient"></param>
	/// <param name="spriteIndex"></param>
	/// <param name="progressBarID"></param>
	/// <returns></returns>
	public static ProgressBarMessage SendUpdate(GameObject recipient, int spriteIndex, int progressBarID)
	{
		ProgressBarMessage msg = new ProgressBarMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId,
			SpriteIndex = spriteIndex,
			ProgressBarID = progressBarID
		};
		msg.SendTo(recipient);
		return msg;
	}
}