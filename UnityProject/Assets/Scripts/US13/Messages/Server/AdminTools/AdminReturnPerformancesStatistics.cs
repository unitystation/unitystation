using Mirror;
using US13.Core.Performance;
using US13.Managers;
using US13.UI.Systems.AdminTools;

namespace US13.Messages.Server.AdminTools
{
	public class AdminReturnPerformancesStatistics : ServerMessage<AdminReturnPerformancesStatistics.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public PerformanceManager.PerformanceInfo Info;



		}

		public override void Process(NetMessage msg)
		{
			GUI_AdminTools.Instance.SetServerPerformancePage(msg.Info);
		}



		public static NetMessage Send(PlayerInfo player, PerformanceManager.PerformanceInfo InInfo)
		{
			NetMessage msg = new NetMessage
			{
				Info = InInfo
			};

			SendTo(player.Connection, msg);
			return msg;
		}
	}
}