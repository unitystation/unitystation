using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;

public class ClientRequestCatalogues : ClientMessage
{
	public override void Process()
	{
		AddressableCatalogueManager.ClientRequestCatalogue(SentByPlayer.GameObject);
	}

	public static ClientRequestCatalogues RequestCatalogue()
	{
		ClientRequestCatalogues msg = new ClientRequestCatalogues();

		msg.Send();
		return msg;
	}
}
