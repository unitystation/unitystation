using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// The Chat API
/// Use the public methods for anything related
/// to chat stream
/// </summary>
public class Chat : MonoBehaviour
{
	private static Chat chat;

	public static Chat Instance
	{
		get
		{
			if (chat == null)
			{
				chat = FindObjectOfType<Chat>();
			}

			return chat;
		}
	}

	//Connections to scene based ChatRelay. This is null if in the lobby
	private ChatRelay chatRelay;
	private Action<ChatEvent> addChatLogServer;
	private Action<string,ChatChannel,string> addChatLogClient;

	/// <summary>
	/// Set the scene based chat relay at the start of every round
	/// </summary>
	public static void RegisterChatRelay(ChatRelay relay, Action<ChatEvent> serverChatMethod,
		Action<string, ChatChannel, string> clientChatMethod)
	{
		Instance.chatRelay = relay;
		Instance.addChatLogServer = serverChatMethod;
		Instance.addChatLogClient = clientChatMethod;
	}

	/// <summary>
	/// For all general action based messages (i.e. The clown hugged runtime)
	/// Do not use this method for combat messages
	/// Remember to only use this server side
	/// </summary>
	/// <param name="originator"> The player who caused the action</param>
	/// <param name="originatorMessage"> The message that should be given to the originator only (i.e you hugged ian) </param>
	/// <param name="othersMessage"> The message that will be shown to other players (i.e. Cuban Pete hugged ian)</param>
	/// <param name="worldPos"> The world position of the action so that people in the local area can receive the message</param>
	public static void AddActionMsgToChat(GameObject originator, string originatorMessage,
		string othersMessage, Vector2 worldPos)
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			Logger.LogError("A server only method was called on a client in chat.cs", Category.Chat);
			return;
		}

		Instance.addChatLogServer.Invoke(new ChatEvent
		{
			channels = ChatChannel.Action,
			speaker = originator.name,
			message = originatorMessage,
			messageOthers = othersMessage,
			position = worldPos
		});
	}
}
