using Mirror;

namespace Messages.Server
{
	public class UpdateServerFPS : ServerMessage<UpdateServerFPS.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int CurrentFPS;
			public int AverageFPS;
		}

		public override void Process(NetMessage msg)
		{
			GameManager.Instance.ServerCurrentFPS = msg.CurrentFPS;
			GameManager.Instance.ServerAverageFPS = msg.AverageFPS;
		}

		public static NetMessage Send(float currentFPS, float averageFPS)
		{
			NetMessage msg = new NetMessage
			{
				CurrentFPS = (int)currentFPS,
				AverageFPS = (int)averageFPS
			};

			SendToAll(msg);
			return msg;
		}
	}
}