using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Mirror;
using UnityEngine;

public class ClientRequestCatalogues : ClientMessage
{
	public struct ClientRequestCataloguesNetMessage : NetworkMessage { }

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public ClientRequestCataloguesNetMessage message;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as ClientRequestCataloguesNetMessage?;
		if(newMsgNull == null) return;
		var newMsg = newMsgNull.Value;

		AddressableCatalogueManager.ClientRequestCatalogue(SentByPlayer.GameObject);
	}

	public static ClientRequestCataloguesNetMessage RequestCatalogue()
	{
		ClientRequestCataloguesNetMessage msg = new ClientRequestCataloguesNetMessage();

		new ClientRequestCatalogues().Send(msg);
		return msg;
	}
}
