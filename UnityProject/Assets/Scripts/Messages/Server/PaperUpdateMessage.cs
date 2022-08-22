using Mirror;
using UnityEngine;
using UI;

namespace Messages.Server
{
	public class PaperUpdateMessage : ServerMessage<PaperUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint PaperToUpdate;
			public uint Recipient;
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			LoadMultipleObjects(new uint[] {msg.Recipient, msg.PaperToUpdate});
			var paper = NetworkObjects[1].GetComponent<Paper>();
			paper.PaperString = msg.Message;
			ControlTabs.RefreshTabs();
		}

		public static NetMessage Send(GameObject recipient, GameObject paperToUpdate, string message)
		{
			NetMessage msg = new NetMessage
			{
				Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				PaperToUpdate = paperToUpdate.GetComponent<NetworkIdentity>().netId,
				Message = message
			};

			SendTo(recipient, msg);
			return msg;
		}
	}
}
