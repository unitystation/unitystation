using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class SendCataloguesToClient : ServerMessage
{
	public string serialiseCatalogues;

	public override void Process()
	{
		AddressableCatalogueManager.LoadCataloguesFromServer(JsonConvert.DeserializeObject<List<string>>(serialiseCatalogues));
	}

	public static SendCataloguesToClient Send(List<string> Catalogues, GameObject ToWho)
	{
		SendCataloguesToClient msg = new SendCataloguesToClient
		{
			serialiseCatalogues = JsonConvert.SerializeObject(Catalogues)
		};
		msg.SendTo(ToWho);
		return msg;
	}

}
