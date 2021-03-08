using System.Collections.Generic;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Messages.Server.Addressable
{
	public class SendCataloguesToClient : ServerMessage<SendCataloguesToClient.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string serialiseCatalogues;
		}

		public override void Process(NetMessage msg)
		{
			AddressableCatalogueManager.LoadCataloguesFromServer(JsonConvert.DeserializeObject<List<string>>(msg.serialiseCatalogues));
		}

		public static NetMessage Send(List<string> Catalogues, GameObject ToWho)
		{
			NetMessage msg = new NetMessage
			{
				serialiseCatalogues = JsonConvert.SerializeObject(Catalogues)
			};

			SendTo(ToWho, msg);
			return msg;
		}

	}
}
