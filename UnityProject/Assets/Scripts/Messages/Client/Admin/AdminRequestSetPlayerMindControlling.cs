using Messages.Client;
using Mirror;
using UnityEngine;

public class AdminRequestSetPlayerMindControlling : ClientMessage<AdminRequestSetPlayerMindControlling.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public uint MindID;
		public string playerID;
	}

	public override void Process(NetMessage msg)
	{
		if (HasPermission(TAG.MANAGE_MIND_OWNERSHIP)) //TODO tagss!!
		{
			var Mind = MindManager.Instance.minds[msg.MindID];
			var Player = PlayerList.Instance.GetPlayerByID(msg.playerID);

			if (Mind.ControlledBy != null && Mind.ControlledBy != Player)
			{
				Mind.ControlledBy.GenNewMind();
			}

			PlayerSpawn.TransferAccountToSpawnedMind(Player, Mind);
			Mind.StopGhosting();
		}
	}

	public static NetMessage Send(uint MindID, string playerID)
	{
		var msg = new NetMessage
		{
			MindID = MindID,
			playerID = playerID
		};

		Send(msg);
		return msg;
	}
}