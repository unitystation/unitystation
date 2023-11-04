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
			public int Minutes;
		}

		public override void Process(NetMessage msg)
		{
			GameManager.Instance.SyncTime(msg.Time, msg.Minutes);
		}

		public static NetMessage Send(string time, int minutes)
		{
			NetMessage msg = new NetMessage
			{
				Time = time,
				Minutes = minutes
			};

			SendToAll(msg);
			return msg;
		}
	}
}