using System.Collections;
using Logs;
using UnityEngine;
using Mirror;

namespace Messages.Server
{
	/// <summary>
	///     Tells client to update the progress bar for crafting
	/// </summary>
	public class ProgressBarMessage : ServerMessage<ProgressBarMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint Recipient;
			public int SpriteIndex;
			public Vector2Int OffsetFromPlayer;
			public int ProgressBarID;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.Recipient);

			var bar = UIManager.GetProgressBar(msg.ProgressBarID);

			//bar not found, so create it unless we are the server (in which case it would already be created)
			if (bar == null && !CustomNetworkManager.IsServer)
			{
				Loggy.LogTraceFormat("Client progress bar ID {0} not found, creating it.", Category.ProgressAction, msg.ProgressBarID);
				bar = UIManager.CreateProgressBar(msg.OffsetFromPlayer, msg.ProgressBarID);
			}

			if (bar != null)
			{
				bar.ClientUpdateProgress(msg.SpriteIndex);
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
		public static NetMessage SendCreate(GameObject recipient, int spriteIndex, Vector2Int offsetFromPlayer, int progressBarID)
		{
			NetMessage msg = new NetMessage
			{
				Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				SpriteIndex = spriteIndex,
				OffsetFromPlayer = offsetFromPlayer,
				ProgressBarID = progressBarID
			};

			SendTo(recipient, msg);
			return msg;
		}

		/// <summary>
		/// Sends the message to update the progress bar with the specified id
		/// </summary>
		/// <param name="recipient"></param>
		/// <param name="spriteIndex"></param>
		/// <param name="progressBarID"></param>
		/// <returns></returns>
		public static NetMessage SendUpdate(GameObject recipient, int spriteIndex, int progressBarID)
		{
			NetMessage msg = new NetMessage
			{
				Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				SpriteIndex = spriteIndex,
				ProgressBarID = progressBarID
			};

			SendTo(recipient, msg);
			return msg;
		}
	}
}