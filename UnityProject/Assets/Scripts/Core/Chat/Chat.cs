using UnityEngine;
using System;
using AdminTools;
using Tilemaps.Behaviours.Meta;
using DiscordWebhook;
using DatabaseAPI;
using Systems.MobAIs;
using System.Text;
using System.Text.RegularExpressions;
using Core.Chat;
using Items;
using Messages.Server;

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
	//Does the ghost hear everyone or just local
	public bool GhostHearAll { get; set; } = true;

	public bool OOCMute = false;

	public EmoteActionManager emoteActionManager;

	private static Regex htmlRegex = new Regex(@"^(http|https)://.*$");

	public static void InvokeChatEvent(ChatEvent chatEvent)
	{
		var channels = chatEvent.channels;
		StringBuilder discordMessageBuilder = new StringBuilder();

		// There could be multiple channels we need to send a message for each.
		// We do this on the server side so that local chans can be validated correctly
		foreach (ChatChannel channel in channels.GetFlags())
		{
			if (IsNamelessChan(channel) || channel == ChatChannel.None)
			{
				continue;
			}

			// A temporary solution until proper telecomms is implemented
			if (Channels.RadioChannels.HasFlag(channel))
			{
				if (InGameEvents.EventCommsBlackout.CommsDown) return;

				if (InGameEvents.EventProcessorOverload.ProcessorOverload)
				{
					chatEvent.message = InGameEvents.EventProcessorOverload.ProcessMessage(chatEvent.message);
				}
			}

			chatEvent.channels = channel;
			ChatRelay.Instance.PropagateChatToClients(chatEvent);
			discordMessageBuilder.Append($"[{channel}] ");
		}

		discordMessageBuilder.Append($"\n```css\n{chatEvent.speaker}: {chatEvent.message}\n```\n");

		string discordMessage = discordMessageBuilder.ToString();
		//Sends All Chat messages to a discord webhook
		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAllChatURL, discordMessage, "");
	}

	/// <summary>
	/// Send a Chat Msg from a player to the selected Chat Channels
	/// Server only
	/// </summary>
	public static void AddChatMsgToChat(ConnectedPlayer sentByPlayer, string message, ChatChannel channels)
	{
		message = AutoMod.ProcessChatServer(sentByPlayer, message);
		if (string.IsNullOrWhiteSpace(message)) return;

		var player = sentByPlayer.Script;

		// The exact words that leave the player's mouth (or that are narrated). Already includes HONKs, stutters, etc.
		// This step is skipped when speaking in the OOC channel.
		(string message, ChatModifier chatModifiers) processedMessage = (string.Empty, ChatModifier.None); // Placeholder values
		bool isOOC = channels.HasFlag(ChatChannel.OOC);
		if (!isOOC)
		{
			processedMessage = ProcessMessage(sentByPlayer, message);
		}

		var chatEvent = new ChatEvent
		{
			message = isOOC ? message : processedMessage.message,
			modifiers = (player == null) ? ChatModifier.None : processedMessage.chatModifiers,
			speaker = (player == null) ? sentByPlayer.Username : player.name,
			position = (player == null) ? TransformState.HiddenPos : player.gameObject.AssumedWorldPosServer(),
			channels = channels,
			originator = sentByPlayer.GameObject
		};

		if (channels.HasFlag(ChatChannel.OOC))
		{
			chatEvent.speaker = sentByPlayer.Username;

			var isAdmin = PlayerList.Instance.IsAdmin(sentByPlayer.UserId);

			if (isAdmin)
			{
				chatEvent.speaker = "<color=red>[Admin]</color> " + chatEvent.speaker;
			}
			else if(PlayerList.Instance.IsMentor(sentByPlayer.UserId)){
				chatEvent.speaker = "<color=#6400ff>[Mentor]</color> " + chatEvent.speaker;
			}

			if (Instance.OOCMute && !isAdmin) return;

			//http/https links in OOC chat
			if (isAdmin || !GameManager.Instance.AdminOnlyHtml)
			{
				if (htmlRegex.IsMatch(chatEvent.message))
				{
					var messageParts = chatEvent.message.Split(' ');

					var builder = new StringBuilder();

					foreach (var part in messageParts)
					{
						if (!htmlRegex.IsMatch(part))
						{
							builder.Append(part);
							builder.Append(" ");
							continue;
						}

						builder.Append($"<link={part}><color=blue>{part}</color></link> ");
					}

					chatEvent.message = builder.ToString();

					//TODO have a config file available to whitelist/blacklist links if all players are allowed to post links
					//disables client side tag protection to allow <link=></link> tag
					chatEvent.stripTags = false;
				}
			}

			ChatRelay.Instance.PropagateChatToClients(chatEvent);

			//Sends OOC message to a discord webhook
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookOOCURL, message, chatEvent.speaker, ServerData.ServerConfig.DiscordWebhookOOCMentionsID);

			if (!ServerData.ServerConfig.DiscordWebhookSendOOCToAllChat) return;

			//Send it to All chat
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAllChatURL, $"[{ChatChannel.OOC}]  {message}\n", chatEvent.speaker);

			return;
		}

		// TODO the following code uses player.playerHealth, but ConciousState would be more appropriate.
		// Check if the player is allowed to talk:
		if (player != null && player.playerHealth != null)
		{
			if (!player.IsDeadOrGhost && player.mind.IsMiming && !processedMessage.chatModifiers.HasFlag(ChatModifier.Emote))
			{
				AddWarningMsgFromServer(sentByPlayer.GameObject, "You can't talk because you made a vow of silence.");
				return;
			}

			if (player.playerHealth.IsCrit)
			{
				if (!player.playerHealth.IsDead)
				{
					return;
				}
				else
				{
					chatEvent.channels = ChatChannel.Ghost;
				}
			}
			else if (!player.playerHealth.IsDead && !player.IsGhost)
			{
				//Control the chat bubble
				player.playerNetworkActions.CmdToggleChatIcon(true, processedMessage.message, channels, processedMessage.chatModifiers);
			}
		}

		InvokeChatEvent(chatEvent);
	}

	/// <summary>
	/// Broadcast a comm. message to chat, by machine. Useful for e.g. AutomatedAnnouncer.
	/// </summary>
	/// <param name="sentByMachine">Machine broadcasting the message</param>
	/// <param name="message">The message to broadcast.</param>
	/// <param name="channels">The channels to broadcast on.</param>
	/// <param name="chatModifiers">Chat modifiers to use e.g. ChatModifier.ColdlyState.</param>
	/// <param name="broadcasterName">Optional name for the broadcaster. Pulls name from GameObject if not used.</param>
	public static void AddCommMsgByMachineToChat(
			GameObject sentByMachine, string message, ChatChannel channels,
			ChatModifier chatModifiers = ChatModifier.None, string broadcasterName = default)
	{
		if (string.IsNullOrWhiteSpace(message)) return;

		var chatEvent = new ChatEvent
		{
			message = message,
			modifiers = chatModifiers,
			speaker = broadcasterName != default ? broadcasterName : sentByMachine.ExpensiveName(),
			position = sentByMachine.WorldPosServer(),
			channels = channels,
			originator = sentByMachine
		};

		InvokeChatEvent(chatEvent);
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

		ChatRelay.Instance.PropagateChatToClients(new ChatEvent
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

		ChatRelay.Instance.PropagateChatToClients(new ChatEvent
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

		//dont send message if originator message is blank
		if (string.IsNullOrWhiteSpace(originatorMessage)) return;

		ChatRelay.Instance.PropagateChatToClients(new ChatEvent
		{
			channels = ChatChannel.Action,
			speaker = originator.name,
			message = originatorMessage,
			messageOthers = othersMessage,
			position = originator.AssumedWorldPosServer(),
			originator = originator
		});
	}

	/// <see cref="Chat.AddActionMsgToChat"/>
	/// <param name="interaction"> The interaction which caused this action (performer will be originator)</param>
	/// <param name="originatorMessage"> The message that should be given to the originator only (i.e you hugged ian) </param>
	/// <param name="othersMessage"> The message that will be shown to other players (i.e. Cuban Pete hugged ian)</param>
	public static void AddActionMsgToChat(Interaction interaction, string originatorMessage,
		string othersMessage)
	{
		AddActionMsgToChat(interaction.Performer, originatorMessage, othersMessage);
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

		ChatRelay.Instance.PropagateChatToClients(new ChatEvent
		{
			channels = ChatChannel.Combat,
			message = originatorMsg,
			messageOthers = othersMsg,
			speaker = originator.name,
			position = originator.AssumedWorldPosServer(),
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
	/// <param name="attackedTile">If attacking a particular tile, the layer tile being attacked</param>
	public static void AddAttackMsgToChat(GameObject attacker, GameObject victim,
		BodyPartType hitZone = BodyPartType.None, GameObject item = null, string customAttackVerb = "", LayerTile attackedTile = null, Vector3 posOverride = new Vector3())
	{
		string attackVerb;
		string attack;

		if (item)
		{
			var itemAttributes = item.GetComponent<ItemAttributesV2>();
			attackVerb = itemAttributes.ServerAttackVerbs.PickRandom() ?? "attacked";
			attack = $" with {itemAttributes.ArticleName}";
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
				if (player.Script.characterSettings.BodyType == BodyType.Female)
				{
					victimNameOthers = "herself";
				}
				else if (player.Script.characterSettings.BodyType == BodyType.Male)
				{
					victimNameOthers = "himself";
				}
				else
				{
					victimNameOthers = "themselves";
				}
			}
			else
			{
				victimNameOthers = "itself";
			}
		}
		else if (attackedTile != null)
		{
			victimName = attackedTile.DisplayName;
			victimNameOthers = victimName;
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

		var pos = attacker.AssumedWorldPosServer();

		if (posOverride != Vector3.zero)
		{
			pos = posOverride;
		}

		ChatRelay.Instance.PropagateChatToClients(new ChatEvent
		{
			channels = ChatChannel.Combat,
			message = message,
			messageOthers = messageOthers,
			position = pos,
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

		BodyPartType effectiveHitZone = hitZone;

		var player = victim.Player();
		if (player == null)
		{
			effectiveHitZone = BodyPartType.None;
		}

		var message =
			$"{victim.ExpensiveName()} has been hit by a {item.Item()?.ArticleName ?? item.name}{InTheZone(effectiveHitZone)}";
		ChatRelay.Instance.PropagateChatToClients(new ChatEvent
		{
			channels = ChatChannel.Combat,
			message = message,
			position = victim.AssumedWorldPosServer(),
			originator = victim
		});
	}

	/// <summary>
	/// Allows grouping destruction messages into one if they happen in short period of time.
	/// Average position is calculated in that case.
	/// </summary>
	/// <param name="message"></param>
	/// <param name="postfix">Common, amount agnostic postfix</param>
	/// <param name="worldPos"></param>
	public static void AddLocalDestroyMsgToChat(string message, string postfix, GameObject destroyedObject)
	{
		if (!messageQueueDict.ContainsKey(postfix))
		{
			messageQueueDict.Add(postfix, new UniqueQueue<DestroyChatMessage>());
		}

		messageQueueDict[postfix].Enqueue(new DestroyChatMessage { Message = message, WorldPosition = destroyedObject.AssumedWorldPosServer() });

		if (composeMessageHandle == null)
		{
			Instance.StartCoroutine(Instance.ComposeDestroyMessage(), ref composeMessageHandle);
		}
	}

	/// <summary>
	/// For any other local messages that are not an Action or a Combat Action.
	/// I.E for machines
	/// Server side only
	/// </summary>
	/// <param name="message">The message to show in the chat stream</param>
	/// <param name="worldPos">The position of the local message</param>
	/// <param name="originator">The object (i.e. vending machine) that said message</param>
	public static void AddLocalMsgToChat(string message, Vector2 worldPos, GameObject originator, string speakerName = null)
	{
		if (!IsServer()) return;
		Instance.TryStopCoroutine(ref composeMessageHandle);

		ChatRelay.Instance.PropagateChatToClients(new ChatEvent
		{
			channels = ChatChannel.Local,
			message = message,
			position = worldPos,
			originator = originator,
			speaker = speakerName
		});
	}

	/// <summary>
	/// For any other local messages that are not an Action or a Combat Action.
	/// I.E for machines
	/// Server side only
	/// </summary>
	/// <param name="message">The message to show in the chat stream</param>
	/// <param name="originator">The object (i.e. vending machine) that said message</param>
	public static void AddLocalMsgToChat(string message, GameObject originator, string speakerName = null)
	{
		AddLocalMsgToChat(message, originator.AssumedWorldPosServer(), originator, speakerName);
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

	/// <inheritdoc cref="AddExamineMsgFromServer(GameObject, string)"/>
	public static void AddExamineMsgFromServer(ConnectedPlayer recipient, string msg)
	{
		if (recipient == null || recipient.Equals(ConnectedPlayer.Invalid))
		{
			Logger.LogError($"Can't send message \"{msg}\" to invalid player!", Category.Chat);
			return;
		}

		AddExamineMsgFromServer(recipient.GameObject, msg);
	}

	/// <summary>
	/// Used on the client for examine messages.
	/// Use client side only!
	/// </summary>
	/// <param name="message"> The message to add to the client chat stream</param>
	public static void AddExamineMsgToClient(string message)
	{
		ChatRelay.Instance.UpdateClientChat(message, ChatChannel.Examine, true, PlayerManager.LocalPlayer);
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
		switch(side)
		{
			case NetworkSide.Client:
				AddExamineMsgToClient(message);
				break;

			case NetworkSide.Server:
				AddExamineMsgFromServer(recipient, message);
				break;
			default:
				Debug.Assert(false, "Unknown Network Side");
				break;
		}
	}

	public static void AddWarningMsgFromServer(GameObject recipient, string msg)
	{
		if (!IsServer()) return;
		UpdateChatMessage.Send(recipient, ChatChannel.Warning, ChatModifier.None, msg);
	}

	public static void AddWarningMsgToClient(string message)
	{
		message = ProcessMessageFurther(message, "", ChatChannel.Warning, ChatModifier.None); //TODO: Put processing in a unified place for server and client.
		ChatRelay.Instance.UpdateClientChat(message, ChatChannel.Warning, true, PlayerManager.LocalPlayer);
	}

	public static void AddAdminPrivMsg(string message)
	{
		ChatRelay.Instance.AddAdminPrivMessageToClient(message);
	}

	public static void AddMentorPrivMsg(string message)
	{
		ChatRelay.Instance.AddMentorPrivMessageToClient(message);
	}

	/// <summary>
	/// replaces the provided string occurence's of {performer} with the performer's name
	/// </summary>
	/// <param name="toReplace"></param>
	/// <returns></returns>
	public static string ReplacePerformer(string toReplace, GameObject performer)
	{
		if (!string.IsNullOrWhiteSpace(toReplace))
		{
			return toReplace.Replace("{performer}", performer.ExpensiveName());
		}

		return toReplace;
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
}
