using System;
using System.Collections;
using System.Collections.Generic;
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

		List<SpriteHandlerIdentifier> Received = null;
		try
		{
			Received = JsonConvert.DeserializeObject<List<SpriteHandlerIdentifier>>(msg.Data);
		}
		catch (Exception e)
		{
			Logger.LogError("malformed Json from client " + SentByPlayer + "\n" + e);
			return;
		}

		if (Received != null)
		{
			var ToRequestSH = new List<SpriteHandler>();
			foreach (var TOC in Received)
			{
				if (NetworkIdentity.spawned.ContainsKey(TOC.ID))
				{
					var NETID = NetworkIdentity.spawned[TOC.ID];
					if (SpriteHandlerManager.PresentSprites.ContainsKey(NETID))
					{
						if (SpriteHandlerManager.PresentSprites[NETID].ContainsKey(TOC.Name))
						{
							ToRequestSH.Add(SpriteHandlerManager.PresentSprites[NETID][TOC.Name]);
						}
					}
				}
			}

			if (ToRequestSH.Count == 0) return;
			spriteHandlerManager.ClientRequestForceUpdate(ToRequestSH, SentByPlayer.Connection);
		}
	}

	public static NetMessage Send(SpriteHandlerManager spriteHandlerManager, List<SpriteHandler> ToUpdate)
	{
		if (CustomNetworkManager.Instance._isServer == true) return new NetMessage();
		var TOSend = new List<SpriteHandlerIdentifier>();
		foreach (var SH in ToUpdate)
		{
			TOSend.Add(new SpriteHandlerIdentifier(SH.GetMasterNetID().netId, SH.name));
		}

		var msg = new NetMessage()
		{
			SpriteHandlerManager = spriteHandlerManager.GetComponent<NetworkIdentity>().netId,
			Data = JsonConvert.SerializeObject(TOSend)
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