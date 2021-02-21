using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update the progress bar for crafting
/// </summary>
public class ProgressBarMessage : ServerMessage
{
	public class ProgressBarMessageNetMessage : NetworkMessage
	{
		public uint Recipient;
		public int SpriteIndex;
		public Vector2Int OffsetFromPlayer;
		public int ProgressBarID;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as ProgressBarMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadNetworkObject(newMsg.Recipient);

		var bar = UIManager.GetProgressBar(newMsg.ProgressBarID);

		//bar not found, so create it unless we are the server (in which case it would already be created)
		if (bar == null && !CustomNetworkManager.IsServer)
		{
			Logger.LogTraceFormat("Client progress bar ID {0} not found, creating it.", Category.ProgressAction, newMsg.ProgressBarID);
			bar = UIManager.CreateProgressBar(newMsg.OffsetFromPlayer, newMsg.ProgressBarID);
		}

		if (bar != null)
		{
			bar.ClientUpdateProgress(newMsg.SpriteIndex);
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
	public static ProgressBarMessageNetMessage SendCreate(GameObject recipient, int spriteIndex, Vector2Int offsetFromPlayer, int progressBarID)
	{
		ProgressBarMessageNetMessage msg = new ProgressBarMessageNetMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId,
			SpriteIndex = spriteIndex,
			OffsetFromPlayer = offsetFromPlayer,
			ProgressBarID = progressBarID
		};
		new ProgressBarMessage().SendTo(recipient, msg);
		return msg;
	}

	/// <summary>
	/// Sends the message to update the progress bar with the specified id
	/// </summary>
	/// <param name="recipient"></param>
	/// <param name="spriteIndex"></param>
	/// <param name="progressBarID"></param>
	/// <returns></returns>
	public static ProgressBarMessageNetMessage SendUpdate(GameObject recipient, int spriteIndex, int progressBarID)
	{
		ProgressBarMessageNetMessage msg = new ProgressBarMessageNetMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId,
			SpriteIndex = spriteIndex,
			ProgressBarID = progressBarID
		};
		new ProgressBarMessage().SendTo(recipient, msg);
		return msg;
	}
}