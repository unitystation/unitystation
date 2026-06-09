using Mirror;
using US13.Core.Chat;

namespace US13.Messages.Client
{
	public class RequestEmote : ClientMessage<RequestEmote.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Name;
		}

		public override void Process(NetMessage msg)
		{
			//if (Validations.CanInteract(SentByPlayer.Script, NetworkSide.Server)  == false) return;?
			EmoteActionManager.DoEmote(msg.Name, SentByPlayer.Mind.GetDeepestBody().gameObject);
		}

		public static void Send( string Name)
		{
			var Net = new NetMessage()
			{
				Name = Name,
			};


			Send(Net);
		}

	}
}
