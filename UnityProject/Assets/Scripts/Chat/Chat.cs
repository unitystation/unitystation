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
	/// Adds a system message to all players on the given matrix
	/// Server side only
	/// </summary>
	/// <param name="message"> message to add to each clients chat stream</param>
	/// <param name="stationMatrix"> the matrix to broadcast the message too</param>
	public static void AddSystemMsgToChat(string message, MatrixInfo stationMatrix)
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			Logger.LogError("A server only method was called on a client in chat.cs", Category.Chat);
			return;
		}

		Instance.addChatLogServer.Invoke(new ChatEvent
		{
			message = message,
			channels = ChatChannel.System,
			matrix = stationMatrix
		});
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
		string othersMessage, Vector2 worldPos, float radius = 10f)
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
			position = worldPos,
			radius = radius
		});
	}

	/// <summary>
	/// For any other local messages that are not an Action or a Combat Action
	/// Server side only
	/// </summary>
	/// <param name="message">The message to show in the chat stream</param>
	/// <param name="worldPos">The position of the local message</param>
	/// <param name="radius">Sets the radius of who should be included in this local msg based on their distance
	/// message position</param>
	public static void AddLocalMsgToChat(string message, Vector2 worldPos, float radius = 10f)
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			Logger.LogError("A server only method was called on a client in chat.cs", Category.Chat);
			return;
		}

		Instance.addChatLogServer.Invoke(new ChatEvent{
			channels = ChatChannel.Local,
			message = message,
			position = worldPos,
			radius = radius
			});
	}

	/// <summary>
	/// Used on the client for examine messages
	/// Use client side only
	/// </summary>
	/// <param name="message"> The message to add to the client chat stream</param>
	public static void AddExamineMsgToClient(string message)
	{
		Instance.addChatLogClient.Invoke(message, ChatChannel.Examine, "");
	}
}
