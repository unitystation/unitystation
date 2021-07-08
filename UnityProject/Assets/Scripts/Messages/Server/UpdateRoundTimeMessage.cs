using Mirror;

namespace Messages.Server
{
	/// <summary>
	///	Message that tells client what is the current round time
	/// </summary>
	public class UpdateRoundTimeMessage : ServerMessage<UpdateRoundTimeMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Time;
		}

		public override void Process(NetMessage msg)
		{
			GameManager.Instance.SyncTime(msg.Time);
		}

		public static NetMessage Send(string time)
		{
			NetMessage msg = new NetMessage
			{
				Time = time
			};

			SendToAll(msg);
			return msg;
		}
	}
}