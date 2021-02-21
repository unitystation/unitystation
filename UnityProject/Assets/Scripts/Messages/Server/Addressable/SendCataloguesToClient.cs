using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Newtonsoft.Json;

public class SendCataloguesToClient : ServerMessage
{
	public class SendCataloguesToClientNetMessage : NetworkMessage
	{
		public string serialiseCatalogues;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as SendCataloguesToClientNetMessage;
		if(newMsg == null) return;

		AddressableCatalogueManager.LoadCataloguesFromServer(JsonConvert.DeserializeObject<List<string>>(newMsg.serialiseCatalogues));
	}

	public static SendCataloguesToClientNetMessage Send(List<string> Catalogues, GameObject ToWho)
	{
		SendCataloguesToClientNetMessage msg = new SendCataloguesToClientNetMessage
		{
			serialiseCatalogues = JsonConvert.SerializeObject(Catalogues)
		};

		new SendCataloguesToClient().SendTo(ToWho, msg);
		return msg;
	}

}
