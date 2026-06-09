using Mirror;
using US13.Core.Performance;

namespace US13.Messages.Client.Admin
{
	public class AdminRequestPerformancesStatistics : ClientMessage<AdminRequestPerformancesStatistics.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{

		}

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.ADMIN_INFO))
			{
				PerformanceManager.Instance.AdminRequest(SentByPlayer);
			}
		}

		public static NetMessage Send()
		{
			var msg = new NetMessage
			{
			};

			Send(msg);
			return msg;
		}
	}
}