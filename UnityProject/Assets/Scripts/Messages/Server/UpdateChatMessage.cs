using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
/// Message that tells client to add a ChatEvent to their chat.
/// NetMsg is being used so each recipient can be targeted individually.
/// This allows for easy direct client updating (i.e. for whispering)
/// and ensures snooping can not happen on communications
/// </summary>
public class UpdateChatMessage : ServerMessage
{
	public struct UpdateChatMessageNetMessage : NetworkMessage
	{
		public ChatChannel Channels;
		public ChatModifier ChatModifiers;
		public string Message;
		//If OthersMessage is empty then Message is meant for everyone, otherwise Message is meant for originator
		// and others is meant for everyone else
		public string OthersMessage;
		public uint Recipient;
		public uint Originator;
		public string Speaker;
		public bool StripTags;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public UpdateChatMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as UpdateChatMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadNetworkObject(newMsg.Recipient);
		var recipientObject = NetworkObject;
		Chat.ProcessUpdateChatMessage(newMsg.Recipient, newMsg.Originator,
			newMsg.Message, newMsg.OthersMessage, newMsg.Channels, newMsg.ChatModifiers, newMsg.Speaker, recipientObject, newMsg.StripTags);
	}

	/// <summary>
	/// Do not use this message directly. If you need to do work with the chat system use
	/// the Chat API (the only exception to this rule is if you just need to send 1 msg to 1 client from the server
	/// i.e syndi special roles)
	/// </summary>
	public static UpdateChatMessageNetMessage Send(GameObject recipient, ChatChannel channels, ChatModifier chatMods, string chatMessage, string othersMsg = "",
		GameObject originator = null, string speaker = "", bool stripTags = true)
	{
		uint origin = NetId.Empty;
		if (originator != null)
		{
			origin = originator.GetComponent<NetworkIdentity>().netId;
		}

		UpdateChatMessageNetMessage msg =
			new UpdateChatMessageNetMessage {Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				Channels = channels,
				ChatModifiers = chatMods,
				Message = chatMessage,
				OthersMessage = othersMsg,
				Originator = origin,
				Speaker = speaker,
				StripTags = stripTags
			};

		new UpdateChatMessage().SendTo(recipient, msg);
		return msg;
	}

	public override void SendTo<T>(GameObject recipient, T msg)
	{
		if (recipient == null)
		{
			return;
		}

		NetworkConnection connection = recipient.GetComponent<NetworkIdentity>().connectionToClient;

		if (connection == null)
		{
			return;
		}

		//			only send to players that are currently controlled by a client
		if (PlayerList.Instance.ContainsConnection(connection))
		{
			connection.Send(msg, 0);
			Logger.LogTraceFormat("SentTo {0}: {1}", Category.Chat, recipient.name, this);
		}
		else
		{
			Logger.LogTraceFormat("Not sending message {0} to {1}", Category.Chat, this, recipient.name);
		}

		//Obsolete version:
		//NetworkServer.SendToClientOfPlayer(recipient, GetMessageType(), this);
	}
}
