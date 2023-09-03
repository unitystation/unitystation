using System;
using System.Collections;
using System.Collections.Generic;
using Logs;
using Messages.Client;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

public class RequestForceSpriteUpdate : ClientMessage<RequestForceSpriteUpdate.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public uint SpriteHandlerManager;

		public string Data;
	}

	public override void Process(NetMessage msg)
	{
		//TODO Need some safeguards
		LoadNetworkObject(msg.SpriteHandlerManager);
		if (SentByPlayer == PlayerInfo.Invalid)
			return;
		if (NetworkObject == null) return;
		var spriteHandlerManager = NetworkObject.GetComponent<SpriteHandlerManager>();
		if (spriteHandlerManager == null) return;

		List<SpriteHandlerIdentifier> received = null;
		try
		{
			received = JsonConvert.DeserializeObject<List<SpriteHandlerIdentifier>>(msg.Data);
		}
		catch (Exception e)
		{
			Loggy.LogError("malformed Json from client " + SentByPlayer + "\n" + e);
			return;
		}

		if (received != null)
		{
			var toRequestSH = new List<SpriteHandler>();
			foreach (var toc in received)
			{
				if (NetworkServer.spawned.ContainsKey(toc.ID))
				{
					var netId = NetworkServer.spawned[toc.ID];
					if (SpriteHandlerManager.PresentSprites.ContainsKey(netId))
					{
						if (SpriteHandlerManager.PresentSprites[netId].ContainsKey(toc.Name))
						{
							toRequestSH.Add(SpriteHandlerManager.PresentSprites[netId][toc.Name]);
						}
					}
				}
			}

			if (toRequestSH.Count == 0) return;
			spriteHandlerManager.ClientRequestForceUpdate(toRequestSH, SentByPlayer.Connection);
		}
	}

	public static NetMessage Send(SpriteHandlerManager spriteHandlerManager, List<SpriteHandler> toUpdate)
	{
		if (CustomNetworkManager.Instance._isServer == true) return new NetMessage();
		var toSend = new List<SpriteHandlerIdentifier>();
		foreach (var sh in toUpdate)
		{
			toSend.Add(new SpriteHandlerIdentifier(sh.GetMasterNetID().netId, sh.name));
		}

		var msg = new NetMessage()
		{
			SpriteHandlerManager = spriteHandlerManager.GetComponent<NetworkIdentity>().netId,
			Data = JsonConvert.SerializeObject(toSend)
		};
		Send(msg);
		return msg;
	}


	public struct SpriteHandlerIdentifier
	{
		public uint ID;
		public string Name;

		public SpriteHandlerIdentifier(uint inID, string imName)
		{
			ID = inID;
			Name = imName;
		}
	}
}