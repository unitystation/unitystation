using Mirror;
using US13.Managers;
using US13.Messages.Client;

namespace US13.Actions
{
	public class RequestGameActionSO : ClientMessage<RequestGameActionSO.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public ushort soID;
		}

		public override void Process(NetMessage msg)
		{
			if (SentByPlayer != PlayerInfo.Invalid)
			{
				UIActionSOSingleton.Instance.ActionCallServer(msg.soID, SentByPlayer);
			}
		}


		public static void Send(UIActionScriptableObject uIActionScriptableObject)
		{

			NetMessage msg = new NetMessage
			{
				soID = UIActionSOSingleton.ActionsTOID[uIActionScriptableObject]
			};
			Send(msg);
		}
	}
}
