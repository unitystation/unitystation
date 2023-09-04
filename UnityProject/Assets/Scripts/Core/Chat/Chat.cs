using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Tilemaps.Behaviours.Meta;
using AdminTools;
using Communications;
using DiscordWebhook;
using DatabaseAPI;
using Systems.Communications;
using Systems.MobAIs;
using Messages.Server;
using Items;
using Logs;
using Managers;
using Objects.Machines.ServerMachines.Communications;
using Player.Language;
using Shared.Util;
using Tiles;

/// <summary>
/// The Chat API
/// Use the public methods for anything related
/// to chat stream
/// </summary>
public partial class Chat : MonoBehaviour
{
	private static Chat chat;

	public static Chat Instance => FindUtils.LazyFindObject(ref chat);

	//Does the ghost hear everyone or just local
	public bool GhostHearAll { get; set; } = true;

	public bool OOCMute = false;

	private static Regex htmlRegex = new Regex(@"^(http|https)://.*$");

	private static Collider2D[] nonAllocPhysicsSphereResult = new Collider2D[150];
	private static float searchRadiusForSphereResult = 20f;

	public static void InvokeChatEvent(ChatEvent chatEvent)
	{
		var channels = chatEvent.channels;
		StringBuilder discordMessageBuilder = new StringBuilder();

		chatEvent.allChannels = channels;

		// There could be multiple channels we need to send a message for each.
		// We do this on the server side so that local chans can be validated correctly
		foreach (ChatChannel channel in channels.GetFlags())
		{
			if (IsNamelessChan(channel) || channel == ChatChannel.None)
			{
				continue;
			}

			// if we have a channel that requires transmission, find an emitter and send it via a signal.
			if (Channels.RadioChannels.HasFlag(channel))
			{
				var radioMessageData = new CommsServer.RadioMessageData
				{
					ChatEvent = chatEvent,
				};
				chatEvent.channels = channel;

				if (chatEvent.originator.TryGetComponent<PlayerScript>(out var playerScript) == false) continue;
				//There are some cases where the player might not have a dynamic item storage (like the AI)
				if (playerScript.DynamicItemStorage == null)
				{
					NoDynamicInventoryChatInfulencerSearch(playerScript, chatEvent);
					continue;
				}
				//for normal players, just grab the headset that's on their dynamic item storage.
				DynamicInventoryRadioSignal(playerScript, radioMessageData);
				BodyPartInventoryRadioSignal(playerScript, radioMessageData);
				continue;
			}

			chatEvent.channels = channel;
			ChatRelay.Instance.PropagateChatToClients(chatEvent);
			discordMessageBuilder.Append($"[{channel}] ");
		}

		discordMessageBuilder.Append($"{(chatEvent.language != null ? $"[{chatEvent.language.LanguageName}]" : "")}\n```css\n{chatEvent.speaker}: {chatEvent.message}\n```\n");

		string discordMessage = discordMessageBuilder.ToString();
		//Sends All Chat messages to a discord webhook
		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAllChatURL, discordMessage, "");
	}

	private static void NoDynamicInventoryChatInfulencerSearch(PlayerScript playerScript, ChatEvent chatEvent)
	{
		Physics2D.OverlapCircleNonAlloc(playerScript.PlayerChatLocation.AssumedWorldPosServer(), searchRadiusForSphereResult, nonAllocPhysicsSphereResult);
		foreach (var item in nonAllocPhysicsSphereResult)
		{
			var module = item.gameObject.GetComponentInChildren<IChatInfluencer>();
			if(module == null) continue;
			if(module.WillInfluenceChat() == false) continue;
			module.InfluenceChat(chatEvent);
			break;
		}
	}

	private static void DynamicInventoryRadioSignal(PlayerScript playerScript,CommsServer.RadioMessageData radioMessageData)
	{
		foreach (var slot in playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.ear)
			         .Where(slot => slot.IsEmpty == false))
		{
			if(slot.ItemObject.TryGetComponent<Headset>(out var headset) == false) continue;
			//The headset is responsible for sending this chatEvent to an in-game server that
			//relays this chatEvent to other players
			headset.TrySendSignal(null, radioMessageData);
		}
	}

	private static void BodyPartInventoryRadioSignal(PlayerScript playerScript,CommsServer.RadioMessageData radioMessageData)
	{
		foreach (var BodyPart in playerScript.playerHealth.BodyPartList)
		{
			if(BodyPart.TryGetComponent<SignalEmitter>(out var Emitter) == false) continue;
			//The headset is responsible for sending this chatEvent to an in-game server that
			//relays this chatEvent to other players
			Emitter.TrySendSignal(null, radioMessageData);
		}
	}

	/// <summary>
	/// Send a Chat Msg from a player to the selected Chat Channels
	/// Server only
	/// </summary>
	public static void AddChatMsgToChatServer(PlayerInfo sentByPlayer, string message, ChatChannel channels,
		Loudness loudness = Loudness.NORMAL, ushort languageId = 0)
	{
		message = AutoMod.ProcessChatServer(sentByPlayer, message);
		if (string.IsNullOrWhiteSpace(message)) return;

		//Sanity check for null username
		if (string.IsNullOrWhiteSpace(sentByPlayer.Username))
		{
			Loggy.Log($"Null/empty Username, Details: Username: {sentByPlayer.Username}, ClientID: {sentByPlayer.ClientId}, IP: {sentByPlayer.ConnectionIP}",
				Category.Admin);
			return;
		}

		var player = sentByPlayer.Script;

		//Check to see whether this player is allowed to send on the chosen channels
		if (player != null)
		{
			channels &= player.GetAvailableChannelsMask(true);
		}
		else
		{
			//If player is null, must be in lobby therefore lock to OOC
			channels = ChatChannel.OOC;
		}

		if (channels == ChatChannel.None) return;

		// The exact words that leave the player's mouth (or that are narrated). Already includes HONKs, stutters, etc.
		// This step is skipped when speaking in the OOC channel.
		(string message, ChatModifier chatModifiers) processedMessage = (string.Empty, ChatModifier.None); // Placeholder values

		bool isOOC = channels.HasFlag(ChatChannel.OOC);
		if (isOOC == false)
		{
			processedMessage = ProcessMessage(sentByPlayer, message);

			if (string.IsNullOrWhiteSpace(processedMessage.message)) return;
		}

		var chatEvent = new ChatEvent
		{
			message = isOOC ? message : processedMessage.message,
			modifiers = (player == null) ? ChatModifier.None : processedMessage.chatModifiers,
			speaker = (player == null) ? sentByPlayer.Username : sentByPlayer.Mind.name,
			position = (player == null) ? TransformState.HiddenPos : player.PlayerChatLocation.AssumedWorldPosServer(),
			channels = channels,
			originator = sentByPlayer.GameObject,
			VoiceLevel = loudness,

		};

		//This is to make sure OOC doesn't break
		if (sentByPlayer.Job != JobType.NULL)
		{
			CheckVoiceLevel(sentByPlayer.Script, chatEvent.channels);
		}

		//If OOC or Ghost then show the Admin and Mentor tags
		if (isOOC || chatEvent.channels == ChatChannel.Ghost)
		{
			chatEvent.speaker = StripAll(sentByPlayer.Username);

			//Show admin tag for ghosts
			var isAdmin = PlayerList.Instance.IsAdmin(sentByPlayer.UserId);
			if (isAdmin)
			{
				chatEvent.speaker = "<color=red>[A]</color> " + chatEvent.speaker;
				chatEvent.VoiceLevel = Loudness.LOUD;
			}

			//Handle OOC messages
			if (isOOC)
			{
				//Add mentor tag for non-admin mentors for OOC
				if (isAdmin == false && PlayerList.Instance.IsMentor(sentByPlayer.UserId))
				{
					chatEvent.speaker = "<color=#6400ff>[M]</color> " + chatEvent.speaker;
				}

				AddOOCChatMessage(sentByPlayer, message, chatEvent);
				return;
			}
		}

		//Try find the language
		if (TryGetLanguage(languageId, player, out var languageToUse) == false) return;
		chatEvent.language = languageToUse;

		// TODO the following code uses player.playerHealth, but ConsciousState would be more appropriate.
		// Check if the player is allowed to talk:
		if (player != null)
		{
			if (player.playerHealth != null)
			{
				if (player.IsDeadOrGhost == false && player.Mind.IsMute && !processedMessage.chatModifiers.HasFlag(ChatModifier.Emote))
				{
					AddWarningMsgFromServer(sentByPlayer.GameObject, "You can't talk"); // because you made a vow of silence.
					//TODO Explain why you can't talk
					return;
				}

				if (player.playerHealth.IsCrit)
				{
					if (player.playerHealth.IsDead == false)
					{
						//Crit players can't talk
						return;
					}

					//Crit and dead ghost in body then only ghost channel
					chatEvent.channels = ChatChannel.Ghost;
				}

				if (player.IsDeadOrGhost == false)
				{
					//Check if there's any items on the player that affects their chat (e.g : headphones, muzzles, etc)
					foreach (var slots in player.DynamicItemStorage.ServerContents.Values)
					{
						foreach (var slot in slots)
						{
							if (slot.IsEmpty) continue;
							if (slot.Item.TryGetComponent<IChatInfluencer>(out var listener)
							    && listener.WillInfluenceChat() == true)
							{
								chatEvent = listener.InfluenceChat(chatEvent);
							}
						}
					}
				}
			}
		}

		InvokeChatEvent(chatEvent);
	}

	private static bool TryGetLanguage(ushort languageId, PlayerScript player, out LanguageSO languageToUse)
	{
		var playerLanguages = player.MobLanguages.OrNull();

		//If the player sent a custom language in chat use that if allowed, or default to the current set language
		languageToUse = null;
		if (playerLanguages != null)
		{
			if (languageId != 0)
			{
				languageToUse = LanguageManager.Instance.GetLanguageById(languageId);
			}

			//Check to make sure we can speak that language, if not get the default language
			if (playerLanguages.CanSpeakLanguage(languageToUse) == false)
			{
				languageToUse = playerLanguages.CurrentLanguage;

				if (languageToUse == null)
				{
					AddExamineMsgFromServer(player.gameObject, "You have no selected language!");
					return false;
				}
			}
		}

		return true;
	}

	/// <summary>
	/// ServerSide Only, note there is no validation of message contents here for this type, normal player messages do no go this route
	/// Chat modifiers do not work here
	/// </summary>
	public static void AddChatMsgToChatServer(string message, ChatChannel channels, LanguageSO language, Loudness loudness = Loudness.NORMAL)
	{
		if (channels == ChatChannel.None) return;

		// The exact words that leave the player's mouth (or that are narrated). Already includes HONKs, stutters, etc.
		// This step is skipped when speaking in the OOC channel.
		(string message, ChatModifier chatModifiers) processedMessage = (string.Empty, ChatModifier.None); // Placeholder values

		processedMessage.message = message;

		bool isOOC = channels.HasFlag(ChatChannel.OOC);

		var chatEvent = new ChatEvent
		{
			message = isOOC ? message : processedMessage.message,
			modifiers = ChatModifier.None,
			speaker = "",
			position = TransformState.HiddenPos,
			channels = channels,
			originator = null,
			VoiceLevel = loudness,
			language = language
		};

		//Handle OOC messages
		if (isOOC)
		{
			ChatRelay.Instance.PropagateChatToClients(chatEvent);
			return;
		}

		InvokeChatEvent(chatEvent);
	}

	private static void AddOOCChatMessage(PlayerInfo sentByPlayer, string message, ChatEvent chatEvent)
	{
		//Check to see if this player has been OOC muted
		if (sentByPlayer.IsOOCMuted)
		{
			Chat.AddWarningMsgFromServer(sentByPlayer.GameObject, "You are OOC muted!");
			return;
		}

		var isAdmin = PlayerList.Instance.IsAdmin(sentByPlayer.UserId);

		//If global OOCMute don't allow anyone but admins to talk on OOC
		if (Instance.OOCMute && isAdmin == false) return;

		//http/https links in OOC chat
		if (isAdmin || GameManager.Instance.AdminOnlyHtml == false)
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

		var strippedSpeaker = StripTags(chatEvent.speaker);

		//Sends OOC message to a discord webhook
		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookOOCURL, message,
			strippedSpeaker, ServerData.ServerConfig.DiscordWebhookOOCMentionsID);

		if (ServerData.ServerConfig.DiscordWebhookSendOOCToAllChat == false) return;

		//Send it to All chat
		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAllChatURL,
			$"[{ChatChannel.OOC}]  {message}\n", strippedSpeaker);
	}

	private static Loudness CheckVoiceLevel(PlayerScript script, ChatChannel channels)
	{
		//Check if is not a ghost/spectator and the player has an inventory.
		if (script == null || script.IsDeadOrGhost || script.DynamicItemStorage == null)
		{
			return Loudness.NORMAL;
		}

		foreach (ItemSlot slot in script.DynamicItemStorage.GetNamedItemSlots(NamedSlot.ear))
		{
			Headset headset = slot.Item?.gameObject.GetComponent<Headset>();
			if (headset == null) continue;

			//TODO this sets the voice level by the first headset found, if multiple should we choose loudest instead?
			if (headset.LoudSpeakOn && IsOnCorrectChannels(channels))
			{
				return headset.LoudspeakLevel;
			}
		}

		return Loudness.NORMAL;
	}

	private static bool IsOnCorrectChannels(ChatChannel channels)
	{
		if (channels.HasFlag(ChatChannel.Common) ||
		    channels.HasFlag(ChatChannel.Command) || channels.HasFlag(ChatChannel.Security)
		    || channels.HasFlag(ChatChannel.Engineering) || channels.HasFlag(ChatChannel.Medical)
		    || channels.HasFlag(ChatChannel.Science)
		    || channels.HasFlag(ChatChannel.Syndicate) || channels.HasFlag(ChatChannel.Supply))
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Broadcast a comm. message to chat, by machine. Useful for e.g. AutomatedAnnouncer.
	/// </summary>
	/// <param name="sentByMachine">Machine broadcasting the message</param>
	/// <param name="message">The message to broadcast.</param>
	/// <param name="channels">The channels to broadcast on.</param>
	/// <param name="chatModifiers">Chat modifiers to use e.g. ChatModifier.ColdlyState.</param>
	/// <param name="broadcasterName">Optional name for the broadcaster. Pulls name from GameObject if not used.</param>
	/// <param name="voiceLevel">How loud is this message?</param>
	/// <param name="language">The language the message is in, null for no language</param>
	public static void AddCommMsgByMachineToChat(
			GameObject sentByMachine, string message, ChatChannel channels, Loudness voiceLevel,
			ChatModifier chatModifiers = ChatModifier.None, string broadcasterName = default, LanguageSO language = null)
	{
		if (string.IsNullOrWhiteSpace(message)) return;

		var chatEvent = new ChatEvent
		{
			message = message,
			modifiers = chatModifiers,
			speaker = broadcasterName != default ? broadcasterName : sentByMachine.ExpensiveName(),
			position = sentByMachine.AssumedWorldPosServer(),
			channels = channels,
			originator = sentByMachine,
			VoiceLevel = voiceLevel,
			language = language
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
	/// <param name="language">The language the message is in, null for no language</param>
	public static void AddSystemMsgToChat(string message, MatrixInfo stationMatrix, LanguageSO language = null)
	{
		if (!IsServer()) return;

		ChatRelay.Instance.PropagateChatToClients(new ChatEvent
		{
			message = message,
			channels = ChatChannel.System,
			matrix = stationMatrix,
			language = language
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

	/// <summary>
	/// For all general action based messages (i.e. The clown hugged runtime)
	/// Do not use this method for combat messages
	/// Remember to only use this server side
	/// </summary>
	/// <param name="originator"> The player who caused the action</param>
	/// <param name="everyoneMessage"> The message that everyone (including the orignator) will see</param>
	public static void AddActionMsgToChat(GameObject originator, string everyoneMessage)
	{
		if (!IsServer()) return;
		if (string.IsNullOrWhiteSpace(everyoneMessage)) return;

		ChatRelay.Instance.PropagateChatToClients(new ChatEvent
		{
			channels = ChatChannel.Action,
			speaker = originator.name,
			message = everyoneMessage,
			messageOthers = everyoneMessage,
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
	/// <param name="language">Language of the message (null means everyone can understand)</param>
	/// <param name="speakerName">The speakers name</param>
	/// <param name="doSpeechBubble">Do speech bubble at originator?</param>
	public static void AddLocalMsgToChat(string message, Vector2 worldPos, GameObject originator,
		LanguageSO language = null, string speakerName = null, bool doSpeechBubble = false)
	{
		if (!IsServer()) return;
		Instance.TryStopCoroutine(ref composeMessageHandle);

		ChatRelay.Instance.PropagateChatToClients(new ChatEvent
		{
			channels = ChatChannel.Local,
			message = message,
			position = worldPos,
			originator = originator,
			speaker = speakerName,
			ShowChatBubble = doSpeechBubble,
		});
	}

	/// <summary>
	/// For any other local messages that are not an Action or a Combat Action.
	/// I.E for machines
	/// Server side only
	/// </summary>
	/// <param name="message">The message to show in the chat stream</param>
	/// <param name="originator">The object (i.e. vending machine) that said message</param>
	/// <param name="language">Language of the message (null means everyone can understand)</param>
	/// <param name="speakerName">The speakers name</param>
	/// /// <param name="doSpeechBubble">Do speech bubble at originator?</param>
	public static void AddLocalMsgToChat(string message, GameObject originator, LanguageSO language = null,
		string speakerName = null, bool doSpeechBubble = false)
	{
		AddLocalMsgToChat(message, originator.AssumedWorldPosServer(), originator, language,
			speakerName, doSpeechBubble);
	}

	/// <summary>
	/// Used on the server to send examine messages to a player
	/// Server only
	/// </summary>
	/// <param name="recipient">The player object to send the message too</param>
	/// <param name="msg">The examine message</param>
	public static void AddExamineMsgFromServer(GameObject recipient, string msg)
	{
		if (recipient == null) return;
		if (!IsServer()) return;
		UpdateChatMessage.Send(recipient, ChatChannel.Examine, ChatModifier.None, msg, Loudness.NORMAL);
	}

	/// <inheritdoc cref="AddExamineMsgFromServer(GameObject, string)"/>
	public static void AddExamineMsgFromServer(PlayerInfo recipient, string msg)
	{
		if (recipient == null || recipient.Equals(PlayerInfo.Invalid))
		{
			Loggy.LogError($"Can't send message \"{msg}\" to invalid player!", Category.Chat);
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
		ChatRelay.Instance.UpdateClientChat(message, ChatChannel.Examine, true, PlayerManager.LocalPlayerObject, Loudness.NORMAL, ChatModifier.None);
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
		message = ProcessMessageFurther(message, "", ChatChannel.Warning, ChatModifier.None, Loudness.NORMAL, false); //TODO: Put processing in a unified place for server and client.
		ChatRelay.Instance.UpdateClientChat(message, ChatChannel.Warning, true, PlayerManager.LocalPlayerObject, Loudness.NORMAL, ChatModifier.None);
	}

	public static void AddAdminPrivMsg(string message)
	{
		ChatRelay.Instance.AddAdminPrivMessageToClient(message);
	}

	public static void AddPrayerPrivMsg(string message)
	{
		ChatRelay.Instance.AddPrayerPrivMessageToClient(message);
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
