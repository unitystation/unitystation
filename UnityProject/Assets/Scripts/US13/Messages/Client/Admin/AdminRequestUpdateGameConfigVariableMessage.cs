using Mirror;
using US13.Core.Admin.Logs;
using US13.Managers;

namespace US13.Messages.Client.Admin
{
	public class AdminRequestUpdateGameConfigVariableMessage : ClientMessage<AdminRequestUpdateGameConfigVariableMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Value;
			public string VariableName;
		}

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.RCON_ACCESS) == false) return;

			AdminLogsManager.AddNewLog(
				$"Admin {SentByPlayer.Username} updated game config variable {msg.VariableName} to {msg.Value}",
				LogCategory.Admin, BubbleUpToChatAdmin: true
				);

			GameConfigManager.Instance.SetVariable(msg.VariableName, msg.Value);
		}
	}
}