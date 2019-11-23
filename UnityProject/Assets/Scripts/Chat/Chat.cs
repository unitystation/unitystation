using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using Tilemaps.Behaviours.Meta;

/// <summary>
/// The Chat API
/// Use the public methods for anything related
/// to chat stream
/// </summary>
public partial class Chat : MonoBehaviour
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
	private Action<string, ChatChannel> addChatLogClient;

	/// <summary>
	/// Set the scene based chat relay at the start of every round
	/// </summary>
	public static void RegisterChatRelay(ChatRelay relay, Action<ChatEvent> serverChatMethod,
		Action<string, ChatChannel> clientChatMethod)
	{
		Instance.chatRelay = relay;
		Instance.addChatLogServer = serverChatMethod;
		Instance.addChatLogClient = clientChatMethod;
	}

	/// <summary>
	/// Send a Chat Msg from a player to the selected Chat Channels
	/// Server only
	/// </summary>
	public static void AddChatMsgToChat(ConnectedPlayer sentByPlayer, string message, ChatChannel channels)
	{
		var player = sentByPlayer.Script;


		var chatEvent = new ChatEvent
		{
			message = message,
			modifiers = (player == null) ? ChatModifier.None : player.GetCurrentChatModifiers(),
			speaker = (player == null) ? sentByPlayer.Username : player.name,
			position = ((player == null) ? Vector2.zero : (Vector2) player.gameObject.transform.position),
			channels = channels,
			originator = sentByPlayer.GameObject
		};

		if (channels.HasFlag(ChatChannel.OOC))
		{
			chatEvent.speaker = sentByPlayer.Username;
			Instance.addChatLogServer.Invoke(chatEvent);
			return;
		}

		//Check if the player is allowed to talk:
		if (player.playerHealth != null)
		{
			if (player.playerHealth.IsCrit || player.playerHealth.IsCardiacArrest)
			{
				if (!player.playerHealth.IsDead)
				{
					return;
				}
				else
				{
					channels = ChatChannel.Ghost;
				}
			}
			else
			{
				if (!player.playerHealth.IsDead && !player.IsGhost)
				{
					{
						//Control the chat bubble
						player.playerNetworkActions.CmdToggleChatIcon(true, message, channels);
					}
				}
			}
		}

		// There could be multiple channels we need to send a message for each.
			// We do this on the server side that local chans can be determined correctly
			foreach (Enum value in Enum.GetValues(channels.GetType()))
			{
				if (channels.HasFlag((ChatChannel) value))
				{
					//Using HasFlag will always return true for flag at value 0 so skip it
					if ((ChatChannel) value == ChatChannel.None) continue;

					if (IsNamelessChan((ChatChannel) value)) continue;

					chatEvent.channels = (ChatChannel) value;
					Instance.addChatLogServer.Invoke(chatEvent);
				}
			}
		}

		/// <summary>
		/// Adds a system message to all players on the given matrix
		/// You must color your own system messages as they are not done automatically!
		/// Server side only
		/// </summary>
		/// <param name="message"> message to add to each clients chat stream</param>
		/// <param name="stationMatrix"> the matrix to broadcast the message too</param>
		public static void AddSystemMsgToChat(string message, MatrixInfo stationMatrix)
		{
			if (!IsServer()) return;

			Instance.addChatLogServer.Invoke(new ChatEvent
			{
				message = message,
				channels = ChatChannel.System,
				matrix = stationMatrix
			});
		}

		/// <summary>
		/// For game wide system messages like admin messages or
		/// messages related to the round itself.
		/// You must color your own system messages as they are not done automatically!
		/// </summary>
		public static void AddGameWideSystemMsgToChat(string message)
		{
			if (!IsServer()) return;

			Instance.addChatLogServer.Invoke(new ChatEvent
			{
				message = message,
				channels = ChatChannel.System
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
		public static void AddActionMsgToChat(GameObject originator, string originatorMessage,
			string othersMessage)
		{
			if (!IsServer()) return;

			Instance.addChatLogServer.Invoke(new ChatEvent
			{
				channels = ChatChannel.Action,
				speaker = originator.name,
				message = originatorMessage,
				messageOthers = othersMessage,
				position = originator.transform.position,
				originator = originator
			});
		}

		/// <summary>
		/// Use this for general combat messages
		/// </summary>
		/// <param name="originator"> Who is doing the attacking</param>
		/// <param name="originatorMsg"> The message to be shown to the originator</param>
		/// <param name="othersMsg"> The message to be shown to everyone else</param>
		/// <param name="hitZone"> Hitzone of the attack on the victim</param>
		public static void AddCombatMsgToChat(GameObject originator, string originatorMsg,
			string othersMsg, BodyPartType hitZone = BodyPartType.None)
		{
			if (!IsServer()) return;

			Instance.addChatLogServer.Invoke(new ChatEvent
			{
				channels = ChatChannel.Combat,
				message = originatorMsg,
				messageOthers = othersMsg,
				speaker = originator.name,
				position = originator.transform.position,
				originator = originator
			});
		}

		/// <summary>
		/// Sends a message to all players about an attack that took place
		/// </summary>
		/// <param name="attacker">GameObject of the player that attacked</param>
		/// <param name="victim">GameObject of the player hat was the victim</param>
		/// <param name="damage">damage done</param>
		/// <param name="hitZone">zone that was damaged</param>
		/// <param name="item">optional gameobject with an itemattributes, representing the item the attack was made with</param>
		/// <param name="customAttackVerb">If you want to override the attack verb then pass the verb here</param>
		public static void AddAttackMsgToChat(GameObject attacker, GameObject victim,
			BodyPartType hitZone = BodyPartType.None, GameObject item = null, string customAttackVerb = "")
		{
			string attackVerb;
			string attack;

			if (item)
			{
				var itemAttributes = item.GetComponent<IItemAttributes>();
				attackVerb = itemAttributes.ServerAttackVerbs.PickRandom() ?? "attacked";
				attack = $" with {itemAttributes.ItemName}";
			}
			else
			{
				// Punch attack as there is no item.
				attackVerb = "punched";
				attack = "";
			}

			if (!string.IsNullOrEmpty(customAttackVerb))
			{
				attackVerb = customAttackVerb;
			}

			var player = victim.Player();
			if (player == null)
			{
				hitZone = BodyPartType.None;
			}

			string victimName;
			string victimNameOthers = "";
			if (attacker == victim)
			{
				victimName = "yourself";
				if (player != null)
				{
					if (player.Script.characterSettings.Gender == Gender.Female)
					{
						victimNameOthers = "herself";
					}

					if (player.Script.characterSettings.Gender == Gender.Male)
					{
						victimNameOthers = "himself";
					}

					if (player.Script.characterSettings.Gender == Gender.Neuter)
					{
						victimNameOthers = "itself";
					}
				}
				else
				{
					victimNameOthers = "itself";
				}
			}
			else
			{
				victimName = victim.ExpensiveName();
				victimNameOthers = victimName;
			}

			var attackerName = attacker.Player()?.Name;
			if (string.IsNullOrEmpty(attackerName))
			{
				var mobAi = attacker.GetComponent<MobAI>();
				if (mobAi != null)
				{
					attackerName = mobAi.mobName;
				}
				else
				{
					attackerName = "Unknown";
				}
			}

			var messageOthers = $"{attackerName} has {attackVerb} {victimNameOthers}{InTheZone(hitZone)}{attack}!";
			var message = $"You {attackVerb} {victimName}{InTheZone(hitZone)}{attack}!";

			Instance.addChatLogServer.Invoke(new ChatEvent
			{
				channels = ChatChannel.Combat,
				message = message,
				messageOthers = messageOthers,
				position = attacker.transform.position,
				speaker = attacker.name,
				originator = attacker
			});
		}

		/// <summary>
		/// Adds a hit msg from a thrown item to all nearby players chat stream
		/// Serverside only
		/// </summary>
		public static void AddThrowHitMsgToChat(GameObject item, GameObject victim,
			BodyPartType hitZone = BodyPartType.None)
		{
			if (!IsServer()) return;

			var player = victim.Player();
			if (player == null)
			{
				hitZone = BodyPartType.None;
			}

			var message =
				$"{victim.ExpensiveName()} has been hit by {item.Item()?.ItemName ?? item.name}{InTheZone(hitZone)}";
			Instance.addChatLogServer.Invoke(new ChatEvent
			{
				channels = ChatChannel.Combat,
				message = message,
				position = victim.transform.position
			});
		}

		#region Destroy Message

		private static Dictionary<string, UniqueQueue<DestroyChatMessage>> messageQueueDict = new Dictionary<string, UniqueQueue<DestroyChatMessage>>();
		private static Coroutine composeMessageHandle;
		private static StringBuilder stringBuilder = new StringBuilder();
		private struct DestroyChatMessage
		{
			public string Message;
			public Vector2Int WorldPosition;
		}

		/// <summary>
		/// Allows grouping destruction messages into one if they happen in short period of time.
		/// Average position is calculated in that case.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="postfix">Common, amount agnostic postfix</param>
		/// <param name="worldPos"></param>
		public static void AddLocalDestroyMsgToChat( string message, string postfix, Vector2Int worldPos )
		{
			if ( !messageQueueDict.ContainsKey( postfix ) )
			{
				messageQueueDict.Add( postfix, new UniqueQueue<DestroyChatMessage>() );
			}

			messageQueueDict[postfix].Enqueue( new DestroyChatMessage{Message = message, WorldPosition = worldPos} );

			if ( composeMessageHandle == null )
			{
				Instance.StartCoroutine( Instance.ComposeDestroyMessage(), ref composeMessageHandle );
			}
		}



		private IEnumerator ComposeDestroyMessage()
		{
			yield return WaitFor.Seconds( 0.3f );

			foreach ( var postfix in messageQueueDict.Keys )
			{
				var messageQueue = messageQueueDict[postfix];

				if ( messageQueue.IsEmpty )
				{
					Instance.TryStopCoroutine( ref composeMessageHandle );
					continue;
				}

				//Normal separate messages with precise location
				if ( messageQueue.Count <= 3 )
				{
					while ( messageQueue.TryDequeue( out var msg ) )
					{
						AddLocalMsgToChat( msg.Message + postfix, msg.WorldPosition );
					}
					continue;
				}

				//Combined message at average position
				stringBuilder.Clear();

				int averageX = 0;
				int averageY = 0;
				int count = 1;

				while ( messageQueue.TryDequeue( out DestroyChatMessage msg ) )
				{
					if ( count > 1 )
					{
						stringBuilder.Append( ", " );
					}
					stringBuilder.Append( msg.Message );
					averageX += msg.WorldPosition.x;
					averageY += msg.WorldPosition.y;
					count++;
				}

				AddLocalMsgToChat( stringBuilder.Append( postfix ).ToString(), new Vector2Int(averageX/count,averageY/count) );
			}
		}

		#endregion

	/// <summary>
	/// For any other local messages that are not an Action or a Combat Action.
	/// I.E for machines
	/// Server side only
	/// </summary>
	/// <param name="message">The message to show in the chat stream</param>
	/// <param name="worldPos">The position of the local message</param>
	public static void AddLocalMsgToChat(string message, Vector2 worldPos)
	{
		if (!IsServer()) return;
		Instance.TryStopCoroutine( ref composeMessageHandle );

			Instance.addChatLogServer.Invoke(new ChatEvent
			{
				channels = ChatChannel.Local,
				message = message,
				position = worldPos
			});
		}

		/// <summary>
		/// Used on the server to send examine messages to a player
		/// Server only
		/// </summary>
		/// <param name="recipient">The player object to send the message too</param>
		/// <param name="msg">The examine message</param>
		public static void AddExamineMsgFromServer(GameObject recipient, string msg)
		{
			if (!IsServer()) return;
			UpdateChatMessage.Send(recipient, ChatChannel.Examine, ChatModifier.None, msg);
		}

		/// <summary>
		/// Used on the client for examine messages.
		/// Use client side only!
		/// </summary>
		/// <param name="message"> The message to add to the client chat stream</param>
		public static void AddExamineMsgToClient(string message)
		{
			Instance.addChatLogClient.Invoke(message, ChatChannel.Examine);
		}

		/// <summary>
		/// Creates an examine message for a particular client, correctly choosing the
		/// method to use based on the network side it's called from.
		/// </summary>
		/// <param name="recipient">The player object to send the message too</param>
		/// <param name="msg">The examine message</param>
		/// <param name="side">side this is being called from</param>
		public static void AddExamineMsg(GameObject recipient, string message, NetworkSide side)
		{
			switch (side)
			{
				case NetworkSide.Client:
					AddExamineMsgToClient(message);
					break;
				case NetworkSide.Server:
					AddExamineMsgFromServer(recipient, message);
					break;
			}
		}

		/// <summary>
		/// Creates an examine message for a particular client, correctly choosing the
		/// method to use based on the network side it's called from.
		/// </summary>
		/// <param name="recipient">The player object to send the message too</param>
		/// <param name="msg">The examine message</param>
		public static void AddExamineMsg(GameObject recipient, string message)
		{
			AddExamineMsg(recipient, message,
				CustomNetworkManager.IsServer ? NetworkSide.Server : NetworkSide.Client);
		}

		/// <summary>
		/// This should only be called via UpdateChatMessage
		/// on the client. Do not use for anything else!
		/// </summary>
		public static void ProcessUpdateChatMessage(uint recipient, uint originator, string message,
			string messageOthers, ChatChannel channels, ChatModifier modifiers, string speaker)
		{
			//If there is a message in MessageOthers then determine
			//if it should be the main message or not.
			if (!string.IsNullOrEmpty(messageOthers))
			{
				//This is not the originator so use the messageOthers
				if (recipient != originator)
				{
					message = messageOthers;
				}
			}

			var msg = ProcessMessageFurther(message, speaker, channels, modifiers);
			Instance.addChatLogClient.Invoke(msg, channels);
		}

		private static string InTheZone(BodyPartType hitZone)
		{
			return hitZone == BodyPartType.None ? "" : $" in the {hitZone.ToString().ToLower().Replace("_", " ")}";
		}

		private static bool IsServer()
		{
			if (!CustomNetworkManager.Instance._isServer)
			{
				Logger.LogError("A server only method was called on a client in chat.cs", Category.Chat);
				return false;
			}

			return true;
		}
	}