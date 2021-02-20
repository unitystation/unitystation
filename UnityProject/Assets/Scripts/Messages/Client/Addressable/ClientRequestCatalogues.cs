using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;

public class ClientRequestCatalogues : ClientMessage
{
	public class ClientRequestCataloguesNetMessage : ActualMessage
	{

	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as ClientRequestCataloguesNetMessage;
		if(newMsg == null) return;

		AddressableCatalogueManager.ClientRequestCatalogue(SentByPlayer.GameObject);
	}

	public static ClientRequestCataloguesNetMessage RequestCatalogue()
	{
		ClientRequestCataloguesNetMessage msg = new ClientRequestCataloguesNetMessage();

		new ClientRequestCatalogues().Send(msg);
		return msg;
	}
}
