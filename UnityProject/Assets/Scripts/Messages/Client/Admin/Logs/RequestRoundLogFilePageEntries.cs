using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Admin.Logs;
using Core.Admin.Logs.Stores;
using Initialisation;
using Messages.Client;
using Messages.Client.Admin.Logs;
using Mirror;
using UnityEngine;

public class RequestRoundLogFilePageEntries : ClientMessage<RequestRoundLogFilePageEntries.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public int PageToRequest;
	}

	public override void Process(NetMessage msg)
	{
		_ = Do(msg, SentByPlayer.Connection);
	}

	private async Task Do(NetMessage msg, NetworkConnectionToClient admin)
	{

	}

	public static NetMessage Send(int page)
	{
		NetMessage msg = new NetMessage
		{
			PageToRequest = page
		};

		Send(msg);
		return msg;
	}
}
