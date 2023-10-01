using Logs;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	/// Message that tells client to add a ChatEvent to their chat.
	/// NetMsg is being used so each recipient can be targeted individually.
	/// This allows for easy direct client updating (i.e. for whispering)
	/// and ensures snooping can not happen on communications
	/// </summary>
	public class UpdateChatMessage : ServerMessage<UpdateChatMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
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
			public Loudness Loudness;
			public ushort LanguageId;
			public bool IsWhispering;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.Recipient);
			var recipientObject = NetworkObject;
			LoadNetworkObject(msg.Originator);
			var orginatorObject = NetworkObject;

			//(Max): this only works on the client for some reason.
			//So it will stay like this until I figure out how to make it work on the server.
			if (msg.IsWhispering)
			{
				ChatRelay.HideWhisperedText(ref orginatorObject, ref msg.Message, ref recipientObject);
			}

			Chat.ProcessUpdateChatMessage(msg.Recipient, msg.Originator,
				msg.Message, msg.OthersMessage, msg.Channels, msg.ChatModifiers, msg.Speaker, recipientObject,
				msg.Loudness, msg.StripTags, msg.LanguageId, msg.IsWhispering);
		}

		/// <summary>
		/// Do not use this message directly. If you need to do work with the chat system use
		/// the Chat API (the only exception to this rule is if you just need to send 1 msg to 1 client from the server
		/// i.e syndi special roles)
		/// </summary>
		public static NetMessage Send(GameObject recipient, ChatChannel channels, ChatModifier chatMods, string chatMessage,
			Loudness loudness = Loudness.NORMAL, string othersMsg = "",
			GameObject originator = null, string speaker = "", bool stripTags = true, ushort languageId = 0, bool isWhispering = false)
		{
			uint origin = NetId.Empty;
			if (originator != null)
			{
				origin = originator.GetComponent<NetworkIdentity>().netId;
			}

			if (recipient == null)
			{
				Loggy.LogError("null recipient for Update chat message Please fix");
				return new NetMessage();
			}

			NetMessage msg =
				new NetMessage {Recipient = recipient.GetComponent<NetworkIdentity>().netId,
					Channels = channels,
					ChatModifiers = chatMods,
					Message = chatMessage,
					OthersMessage = othersMsg,
					Originator = origin,
					Speaker = speaker,
					StripTags = stripTags,
					Loudness = loudness,
					LanguageId = languageId,
					IsWhispering = isWhispering,
				};

			SendTo(recipient, msg, Category.Chat, 2);
			return msg;
		}
	}
}
