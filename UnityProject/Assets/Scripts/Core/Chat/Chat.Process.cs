using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using System.Text;
using AddressableReferences;
using Core.Chat;
using DatabaseAPI;
using Managers;
using Mirror;
using NaughtyAttributes;
using ScriptableObjects;
using Strings;
using Tilemaps.Behaviours.Meta;
using Unitystation.Options;
using WebSocketSharp;
using Antagonists;
using Logs;
using SecureStuff;
using Random = UnityEngine.Random;

public partial class Chat
{
	private static Dictionary<string, UniqueQueue<DestroyChatMessage>> messageQueueDict =
		new Dictionary<string, UniqueQueue<DestroyChatMessage>>();

	private static Coroutine composeMessageHandle;
	private static StringBuilder stringBuilder = new StringBuilder();

	private struct DestroyChatMessage
	{
		public string Message;
		public Vector2 WorldPosition;
	}

	public Color oocColor;
	public Color ghostColor;
	public Color binaryColor;
	public Color supplyColor;
	public Color centComColor;
	public Color commandColor;
	public Color commonColor;
	public Color engineeringColor;
	public Color medicalColor;
	public Color scienceColor;
	public Color securityColor;
	public Color serviceColor;
	public Color syndicateColor;
	public Color localColor;
	public Color combatColor;
	public Color warningColor;
	public Color blobColor;
	public Color alienColor;
	public Color defaultColor;

	private static bool playedSound;

	[BoxGroup("Sounds")] public AddressableAudioSource commonRadioChannelSound;
	[BoxGroup("Sounds")] public AddressableAudioSource commonSecurityChannelSound;
	[BoxGroup("Sounds")] public AddressableAudioSource commonSyndicteChannelSound;

	/// <summary>
	/// This channels can't be heared as sound by other players (like binary or changeling hivemind)
	/// </summary>
	public static readonly ChatChannel NonVerbalChannels = ChatChannel.Binary | ChatChannel.Ghost | ChatChannel.Blob | ChatChannel.Alien;

	/// <summary>
	/// This channels are OOC or service messages and shouldn't affect IC communications
	/// </summary>
	public static readonly ChatChannel ServiceChannels = ChatChannel.Action | ChatChannel.Admin | ChatChannel.Combat
	                                                     | ChatChannel.Examine | ChatChannel.OOC | ChatChannel.System |
	                                                     ChatChannel.Warning;

	/// <summary>
	/// This channels are either non verbal communication (Ghost, Binary) or some serivice channel (OOC, Action)
	/// </summary>
	public static readonly ChatChannel NonSpeechChannels = NonVerbalChannels | ServiceChannels;

	/// <summary>
	/// This channels that are verbal
	/// </summary>
	public static readonly ChatChannel SpeechChannels = ChatChannel.Local | ChatChannel.Command | ChatChannel.CentComm
	                                                    | ChatChannel.Supply | ChatChannel.Engineering | ChatChannel.Medical |
	                                                    ChatChannel.Science | ChatChannel.Security | ChatChannel.Service
	                                                    | ChatChannel.Syndicate | ChatChannel.Engineering | ChatChannel.Medical |
	                                                    ChatChannel.Science;

	/// <summary>
	/// Processes a message to be used in the chat log and chat bubbles.
	/// 1. Detects which modifiers should be present in the messages.
	///    - Some of the modifiers will come from the player being unconcious, being a clown, etc.
	///    - Other modifiers are voluntary, such as shouting. They normally cannot override involuntary modifiers.
	///    - Certain emotes override each other and will never be present at the same time.
	///      - Emote overrides whispering, and whispering overrides yelling.
	/// 2. Modifies the message to match the previously detected modifiers.
	///    - Stutters, honks, drunkenness, etc. are directly applied to the message.
	///    - The chat log and chat bubble may add minor changes to the text, such as narration ("Player says, ...").
	/// </summary>
	/// <param name="sendByPlayer">The player sending the message. Used for detecting conciousness and occupation.</param>
	/// <param name="message">The chat message to process.</param>
	/// <returns>A tuple of the processed chat message and the detected modifiers.</returns>
	private static (string, ChatModifier) ProcessMessage(PlayerInfo sentByPlayer, string message)
	{
		ChatModifier chatModifiers = ChatModifier.None; // Modifier that will be returned in the end.
		ConsciousState playerConsciousState = ConsciousState.DEAD;

		if (sentByPlayer.Script == null)
		{
			return (message, chatModifiers);
		}

		if (sentByPlayer.Script.playerHealth != null)
		{
			playerConsciousState = sentByPlayer.Script.playerHealth.ConsciousState;
		}

		//Semi should be able to speak as health shouldnt affect them
		if (sentByPlayer.Script.IsPlayerSemiGhost)
		{
			playerConsciousState = ConsciousState.CONSCIOUS;
		}

		if (playerConsciousState == ConsciousState.UNCONSCIOUS || playerConsciousState == ConsciousState.DEAD)
		{
			// Only the Mute modifier matters if the player cannot speak. We can skip everything else.
			return (message, ChatModifier.Mute);
		}

		// Emote
		if (message.StartsWith("*") || message.StartsWith("/me ", true, CultureInfo.CurrentCulture))
		{
			message = message.Replace("/me", ""); // note that there is no space here as compared to the above if
			message = message.Substring(1); // so that this substring can properly cut off both * and the space

			if(CheckForEmoteAction(message))
			{
				DoEmoteAction(message, sentByPlayer.GameObject);

				//Message is done in DoEmoteAction()
				message = "";
			}

			chatModifiers |= ChatModifier.Emote;
		}
		// Sing
		else if (message.StartsWith("%") || message.StartsWith("/s ", true, CultureInfo.CurrentCulture))
		{
			message = message.Replace("/s", "");
			message = message.Substring(1);
			message = Sing(message);
			chatModifiers |= ChatModifier.Sing;
		}
		// Involuntaly whisper due to not being fully concious
		else if (playerConsciousState == ConsciousState.BARELY_CONSCIOUS)
		{
			chatModifiers |= ChatModifier.Whisper;
		}
		// Yell
		else if ((message == message.ToUpper(CultureInfo.InvariantCulture) // Is it all caps?
		          && message.Any(char.IsLetter))) // AND does it contain at least one letter?
		{
			chatModifiers |= ChatModifier.Yell;
		}
		// Question
		else if (message.EndsWith("?"))
		{
			chatModifiers |= sentByPlayer.Script.PlayerType == PlayerTypes.Ai ?
				ChatModifier.Query : ChatModifier.Question;
		}
		// Exclaim
		else if (message.EndsWith("!"))
		{
			chatModifiers |= ChatModifier.Exclaim;
		}
		//Ai state message
		else if (sentByPlayer.Script.PlayerType == PlayerTypes.Ai)
		{
			chatModifiers |= ChatModifier.State;
		}

		// Assign character trait speech mods
		//TODO Assigning from character creation for now, they exclude each others
		chatModifiers |= Instance.CharacterSpeech[sentByPlayer.Script.characterSettings.Speech];

		//TODO Assign racial speech mods

		// Assign inventory speech mods
		chatModifiers |= sentByPlayer.Script.inventorySpeechModifiers;

		if (sentByPlayer.Script.playerHealth != null)
		{
			chatModifiers |= sentByPlayer.Script.playerHealth.BodyChatModifier;
		}


		/////// Process Speech mutations
		message = SpeechModManager.Instance.ApplyMod(chatModifiers, message);

		return (message, chatModifiers);
	}

	/// <summary>
	/// Processes message further for the chat log.
	/// Adds text styling, color and channel prefixes depending on the message and its modifiers.
	/// </summary>
	/// <returns>The chat message, formatted to suit the chat log.</returns>
	public static string ProcessMessageFurther(string message, string speaker, ChatChannel channels,
		ChatModifier modifiers, Loudness loudness, bool isWhispering, uint originatorUint = 0, bool stripTags = true)
	{
		playedSound = false;
		//Highlight in game name by bolding and underlining if possible
		//Dont play sound here as it could be examine or action, we only play sound for someone speaking
		message = HighlightInGameName(message, false);

		//Skip everything if system message
		if (channels.HasFlag(ChatChannel.System))
		{
			return message;
		}

		//Skip everything in case of combat channel
		if (channels.HasFlag(ChatChannel.Combat))
		{
			return AddMsgColor(channels, $"<i>{message}</i>"); //POC
		}

		//Skip everything if it is an action or examine message or if it is a local message
		//without a speaker (which is used by machines)
		if (channels.HasFlag(ChatChannel.Examine) ||
		    channels.HasFlag(ChatChannel.Action) ||
		    channels.HasFlag(ChatChannel.Local)
		    && string.IsNullOrEmpty(speaker))
		{
			return AddMsgColor(channels, $"<i>{message}</i>");
		}

		// Skip everything if the message is a local warning
		if (channels.HasFlag(ChatChannel.Warning))
		{
			return AddMsgColor(channels, $"<i>{message}</i>");
		}

		if (stripTags)
		{
			message = StripTags(message);

			//Bold names again after tag stripping
			message = HighlightInGameName(message);
		}

		//Check for emote. If found skip chat modifiers, make sure emote is only in Local channel
		if ((modifiers & ChatModifier.Emote) == ChatModifier.Emote)
		{
			// /me message
			channels = ChatChannel.Local;

			message = AddMsgColor(channels, $"<i><b>{speaker}</b> {message}</i>");
			return message;
		}

		//Check for OOC. If selected, remove all other channels and modifiers (could happen if UI fucks up or someone tampers with it)
		if (channels.HasFlag(ChatChannel.OOC))
		{
			//ooc name quick fix
			var name = Regex.Replace(speaker, @"\t\n\r", "");
			if (string.IsNullOrWhiteSpace(name))
			{
				name = "nerd";
			}

			//highlight OOC name by bolding and underlining if possible
			message = HighlightName(message, ServerData.Auth.CurrentUser.DisplayName);

			message = AddMsgColor(channels, $"[ooc] <b>{name}: {message}</b>");
			return message;
		}

		//Ghosts don't get modifiers
		if (channels.HasFlag(ChatChannel.Ghost))
		{
			string[] _ghostVerbs = {"cries", "moans"};
			return AddMsgColor(channels, $"[dead] <b>{speaker}</b> {_ghostVerbs.PickRandom()}: {message}");
		}

		//HighLightCodeWordsForTraitors
		message = HighLightCodeWords(message);
		string verb = "says,";

		if ((modifiers & ChatModifier.Mute) == ChatModifier.Mute)
		{
			return "";
		}

		if ((modifiers & ChatModifier.Whisper) == ChatModifier.Whisper || isWhispering)
		{
			verb = "whispers,";
			message = $"<i>{message}</i>";
			loudness = Loudness.QUIET;
		}
		else if ((modifiers & ChatModifier.Sing) == ChatModifier.Sing)
		{
			verb = "sings,";
			message += " ♫";
		}
		else if ((modifiers & ChatModifier.Yell) == ChatModifier.Yell)
		{
			verb = "yells,";
			message = $"<b>{message}</b>";
		}
		else if ((modifiers & ChatModifier.Query) == ChatModifier.Query)
		{
			verb = "queries,";
		}
		else if ((modifiers & ChatModifier.State) == ChatModifier.State)
		{
			verb = "states,";
		}
		else if ((modifiers & ChatModifier.ColdlyState) == ChatModifier.ColdlyState)
		{
			verb = "coldly states,";
		}
		else if ((modifiers & ChatModifier.Exclaim) == ChatModifier.Exclaim)
		{
			verb = "exclaims,";
		}
		else if ((modifiers & ChatModifier.Question) == ChatModifier.Question)
		{
			verb = "asks,";
		}

		if (string.IsNullOrEmpty(speaker))
		{
			verb = "";
		}

		var chan = $"[{channels.ToString().ToLower().Substring(0, 3)}] ";

		if (channels.HasFlag(ChatChannel.Command))
		{
			chan = "[cmd] ";
		}

		if (channels.HasFlag(ChatChannel.Local))
		{
			chan = "";
		}

		var textSize = loudness switch
		{
			Loudness.QUIET => ChatTemplates.SmallText,
			Loudness.LOUD => ChatTemplates.LargeText,
			Loudness.SCREAMING => ChatTemplates.VeryLargeText,
			Loudness.MEGAPHONE => ChatTemplates.ExtremelyLargeText,
			Loudness.EARRAPE => ChatTemplates.AnnoyingText,
			_ => message.Contains("!!") ? ChatTemplates.LargeText : ChatTemplates.NormalText,
		};

		return AddMsgColor(channels,
			$"{chan}<b>{speaker}</b> {verb}" // [cmd]  Username says,
			+ "  " // Two hair spaces. This triggers Text-to-Speech.
			+ $"<size={textSize + PlayerPrefs.GetInt(ChatOptions.FONTSCALE_KEY, 1)}>" + "\"" +  message + "\"" + "</size>"); // "This text will be spoken by TTS!"
	}

	private static string StripAll(string input)
	{
		string output = StripTags(input);
		output = StripSquareBrackets(output);

		return output;
	}

	public static string StripTags(string input)
	{
		//Regex - find "<" followed by any number of not ">" and ending in ">". Matches any HTML tags.
		Regex rx = new Regex("[<][^>]+[>]");
		string output = rx.Replace(input, "");

		return output;
	}

	private static string StripSquareBrackets(string input)
	{
		//Regex - find "[" followed by any number of not "]" and ending in "]". Matches any HTML tags.
		Regex rx = new Regex("[[][^]]+[]]");
		string output = rx.Replace(input, "");

		return output;
	}

	private static string HighlightInGameName(string input, bool playSound = true)
	{
		if(ThemeManager.ChatHighlight == false && ThemeManager.MentionSound == false)
		{
			return input;
		}

		var boldedName = input;

		//Do in game name if possible
		if (PlayerManager.LocalPlayerScript != null)
		{
			foreach (var nameSplit in PlayerManager.LocalPlayerScript.playerName.Split(' '))
			{
				boldedName = HighlightName(boldedName, nameSplit, playSound);
			}
		}

		return boldedName;
	}

	private static string HighLightCodeWords(string input)
	{

		if (PlayerManager.LocalPlayerScript == null) return input;

		if (PlayerManager.LocalMindScript == null) return input;

		if (PlayerManager.LocalMindScript.IsAntag == false) return input;

		if (CodeWordManager.Instance.CodeWordRoles.Contains(PlayerManager.LocalMindScript.NetworkedAntagJob) == false) return input;

		string[] coloredText = input.Split(' '); //Split at each Word

		for (int j = 0; j < coloredText.Length; j++)
		{
			for (int i = 0; i < CodeWordManager.WORD_COUNT; i++)
			{
				if (Regex.IsMatch(coloredText[j], $@"(?:^|\W){CodeWordManager.Instance.Words[i]}(?:$|\W)", RegexOptions.IgnoreCase))
				{
					coloredText[j] = $"<b><color=red>{coloredText[j]}</color></b>";
				}
				if (Regex.IsMatch(coloredText[j], $@"(?:^|\W){CodeWordManager.Instance.Responses[i]}(?:$|\W)", RegexOptions.IgnoreCase))
				{
					coloredText[j] = $"<b><color=blue>{coloredText[j]}</color></b>";
				}
			}
		}

		return string.Join(" ", coloredText);
	}

	private static string HighlightName(string input, string name, bool playSound = true)
	{
		if ((ThemeManager.ChatHighlight == false && ThemeManager.MentionSound == false) || name.IsNullOrEmpty())
		{
			return input;
		}

		var output = input.Split(' ');

		for (int i = 0; i < output.Count(); i++)
		{
			if (Regex.IsMatch(output[i], $@"(?:^|\W){name}(?:$|\W)", RegexOptions.IgnoreCase))
			{
				// Bold and underline it
				output[i] = $"<u><b>{output[i]}</b></u>";

				if (ThemeManager.MentionSound && playedSound == false && playSound)
				{
					_ = SoundManager.Play(ThemeManager.CurrentMentionSound);
					playedSound = true;

					if (ThemeManager.ChatHighlight == false)
					{
						return input;
					}
				}
			}
		}

		return string.Join(" ", output);
	}


// TODO move all these methods to a proper SpeechModifier SO
	private static string Hiss(Match m)
	{
		string x = m.ToString();
		if (char.IsLower(x[0]))
		{
			x = x + "ss";
		}
		else
		{
			x = x + "SS";
		}

		return x;
	}

	private static string Sing(string m)
	{
		string song = "";

		foreach (char c in m)
		{
			char current = c;
			if (Random.Range(1, 6) == 1)
			{
				current = char.ToUpper(c);
			}

			song += current;
		}

		return song;
	}

	private static string AddMsgColor(ChatChannel channel, string message)
	{
		return $"<color=#{GetChannelColor(channel)}>{message}</color>";
	}

	private IEnumerator ComposeDestroyMessage()
	{
		yield return WaitFor.Seconds(0.3f);

		foreach (var postfix in messageQueueDict.Keys)
		{
			var messageQueue = messageQueueDict[postfix];

			if (messageQueue.IsEmpty)
			{
				Instance.TryStopCoroutine(ref composeMessageHandle);
				continue;
			}

			//Normal separate messages with precise location
			if (messageQueue.Count <= 3)
			{
				while (messageQueue.TryDequeue(out var msg))
				{
					AddLocalMsgToChat(msg.Message + postfix, msg.WorldPosition, null);
				}

				continue;
			}

			//Combined message at average position
			stringBuilder.Clear();

//			int averageX = 0;
//			int averageY = 0;
			var lastPos = Vector2.zero;
			int count = 1;

			while (messageQueue.TryDequeue(out DestroyChatMessage msg))
			{
				if (count > 1)
				{
					stringBuilder.Append(", ");
				}

				stringBuilder.Append(msg.Message);
//				averageX += msg.WorldPosition.x;
//				averageY += msg.WorldPosition.y;
				lastPos = msg.WorldPosition;
				count++;
			}

//			AddLocalMsgToChat(stringBuilder.Append(postfix).ToString(), new Vector2Int(averageX / count, averageY / count));
			AddLocalMsgToChat(stringBuilder.Append(postfix).ToString(), lastPos, null);
		}
	}

	/// <summary>
	/// This should only be called via UpdateChatMessage
	/// on the client. Do not use for anything else!
	/// </summary>
	public static void ProcessUpdateChatMessage(uint recipientUint, uint originatorUint, string message,
		string messageOthers, ChatChannel channels, ChatModifier modifiers, string speaker, GameObject recipient,
		Loudness loudness, bool stripTags = true, ushort languageId = 0, bool isWhispering = false)
	{

		var isOriginator = true;
		if (recipientUint != originatorUint)
		{
			isOriginator = false;
			if (!string.IsNullOrEmpty(messageOthers))
			{
				//If there is a message in MessageOthers then determine
				//if it should be the main message or not.
				message = messageOthers;
			}
		}

		if (GhostValidationRejection(originatorUint, channels)) return;

		if (PlayerManager.LocalPlayerScript != null
		    && PlayerManager.LocalPlayerScript.IsDeadOrGhost == false
		    && PlayerManager.LocalPlayerScript.playerHealth != null
		    && PlayerManager.LocalPlayerScript.playerHealth.CannotRecognizeNames)
		{
			speaker = "<color=red>Unknown</color>";
		}

		var msg = ProcessMessageFurther(message, speaker, channels, modifiers, loudness, isWhispering, originatorUint, stripTags);
		ChatRelay.Instance.UpdateClientChat(msg, channels, isOriginator, recipient, loudness, modifiers, languageId, isWhispering);
	}

	private static bool GhostValidationRejection(uint originator, ChatChannel channels)
	{
		if (PlayerManager.LocalPlayerScript == null) return false;
		if (PlayerManager.LocalPlayerScript.IsGhost == false) return false;
		if (Instance.GhostHearAll && PlayerManager.LocalPlayerScript.IsPlayerSemiGhost == false) return false;

		var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;

		if (spawned.TryGetValue(originator, out var getOrigin))
		{
			if (channels == ChatChannel.Local || channels == ChatChannel.Combat
			                                  || channels == ChatChannel.Action)
			{
				LayerMask layerMask = LayerMask.GetMask("Door Closed");
				if (Vector2.Distance(getOrigin.transform.position,
					PlayerManager.LocalPlayerObject.transform.position) > 14f)
				{
					return true;
				}
				else
				{
					if (MatrixManager.RayCast(getOrigin.transform.position, Vector2.zero, 0, LayerTypeSelection.Walls,
						layerMask, PlayerManager.LocalPlayerObject.transform.position).ItHit)
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	private static string InTheZone(BodyPartType hitZone)
	{
		return hitZone == BodyPartType.None ? "" : $" in the {hitZone.GetDescription().ToLower().Replace("_", " ")}";
	}

	private static bool IsServer()
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			Loggy.LogError("A server only method was called on a client in chat.cs", Category.Chat);
			return false;
		}

		return true;
	}

	private static string GetChannelColor(ChatChannel channel)
	{
		if (channel.HasFlag(ChatChannel.OOC)) return ColorUtility.ToHtmlStringRGBA(Instance.oocColor);
		if (channel.HasFlag(ChatChannel.Ghost)) return ColorUtility.ToHtmlStringRGBA(Instance.ghostColor);
		if (channel.HasFlag(ChatChannel.Binary)) return ColorUtility.ToHtmlStringRGBA(Instance.binaryColor);
		if (channel.HasFlag(ChatChannel.Supply)) return ColorUtility.ToHtmlStringRGBA(Instance.supplyColor);
		if (channel.HasFlag(ChatChannel.CentComm)) return ColorUtility.ToHtmlStringRGBA(Instance.centComColor);
		if (channel.HasFlag(ChatChannel.Command)) return ColorUtility.ToHtmlStringRGBA(Instance.commandColor);
		if (channel.HasFlag(ChatChannel.Common)) return ColorUtility.ToHtmlStringRGBA(Instance.commonColor);
		if (channel.HasFlag(ChatChannel.Engineering)) return ColorUtility.ToHtmlStringRGBA(Instance.engineeringColor);
		if (channel.HasFlag(ChatChannel.Medical)) return ColorUtility.ToHtmlStringRGBA(Instance.medicalColor);
		if (channel.HasFlag(ChatChannel.Science)) return ColorUtility.ToHtmlStringRGBA(Instance.scienceColor);
		if (channel.HasFlag(ChatChannel.Security)) return ColorUtility.ToHtmlStringRGBA(Instance.securityColor);
		if (channel.HasFlag(ChatChannel.Service)) return ColorUtility.ToHtmlStringRGBA(Instance.serviceColor);
		if (channel.HasFlag(ChatChannel.Syndicate)) return ColorUtility.ToHtmlStringRGBA(Instance.syndicateColor);
		if (channel.HasFlag(ChatChannel.Local)) return ColorUtility.ToHtmlStringRGBA(Instance.localColor);
		if (channel.HasFlag(ChatChannel.Combat)) return ColorUtility.ToHtmlStringRGBA(Instance.combatColor);
		if (channel.HasFlag(ChatChannel.Warning)) return ColorUtility.ToHtmlStringRGBA(Instance.warningColor);
		if (channel.HasFlag(ChatChannel.Blob)) return ColorUtility.ToHtmlStringRGBA(Instance.blobColor);
		if (channel.HasFlag(ChatChannel.Alien)) return ColorUtility.ToHtmlStringRGBA(Instance.alienColor);
		return ColorUtility.ToHtmlStringRGBA(Instance.defaultColor);
	}

	private static bool IsNamelessChan(ChatChannel channel)
	{
		if (channel.HasFlag(ChatChannel.System) ||
		    channel.HasFlag(ChatChannel.Combat) ||
		    channel.HasFlag(ChatChannel.Action) ||
		    channel.HasFlag(ChatChannel.Examine))
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// All tags for a radio msg that goes after '.' or ':'
	/// For example ':e' sends message to engineering channel
	/// </summary>
	public static readonly Dictionary<char, ChatChannel> ChannelsTags = new Dictionary<char, ChatChannel>
	{
		{'b', ChatChannel.Binary},
		{'u', ChatChannel.Supply},
		{'y', ChatChannel.CentComm},
		{'c', ChatChannel.Command},
		{'e', ChatChannel.Engineering},
		{'m', ChatChannel.Medical},
		{'n', ChatChannel.Science},
		{'s', ChatChannel.Security},
		{'v', ChatChannel.Service},
		{'t', ChatChannel.Syndicate},
		{'g', ChatChannel.Ghost},
		{'a', ChatChannel.Alien}
	};

	/// <summary>
	/// This function is called on a client side when player changed chat input field
	/// It tries to find channel modifiers in a begining of the playerInput and parse channels tags like ':e'
	/// NOTE: only one channel is suported. You can't select multiple channels like ';:e' or ':me'
	/// </summary>
	/// <param name="playerInput"></param>
	/// <returns></returns>
	public static ParsedChatInput ParsePlayerInput(string playerInput, IChatInputContext context = null)
	{
		// check if message is valid
		if (string.IsNullOrEmpty(playerInput))
			return new ParsedChatInput(playerInput, playerInput, ChatChannel.None, 0);

		var extractedChanel = CheckRadio(playerInput, context, out var specialCharCount);

		var languageId = CheckLanguage(playerInput, ref specialCharCount);

		// delete all special chars
		var clearMsg = playerInput.Substring(specialCharCount).TrimStart(' ');
		return new ParsedChatInput(playerInput, clearMsg, extractedChanel, languageId);
	}

	private static ChatChannel CheckRadio(string playerInput, IChatInputContext context, out int specialCharCount)
	{
		// all extracted channels from special chars
		ChatChannel extractedChanel = ChatChannel.None;
		// how many special chars we need to delete
		specialCharCount = 0;

		var firstLetter = playerInput.First();
		if (firstLetter == ';')
		{
			// it's a common message!
			extractedChanel = ChatChannel.Common;
			specialCharCount = 1;
		}
		else if (firstLetter == '.' || firstLetter == ':')
		{
			// it's a channel message! Can we take a second char?
			if (playerInput.Length > 1)
			{
				var secondLetter = char.ToLower(playerInput[1]);
				// let's try find desired chanel
				if (ChannelsTags.ContainsKey(secondLetter))
				{
					extractedChanel = ChannelsTags[secondLetter];
					specialCharCount = 2;
				}
				else if (secondLetter == 'h')
				{
					// need some additional information about default channel
					if (context != null)
					{
						extractedChanel = context.DefaultChannel;
					}
					else
					{
						Loggy.LogWarning("Chat context is null - can't resolve :h tag", Category.Chat);
						extractedChanel = ChatChannel.None;
					}

					specialCharCount = 2;
				}
			}
		}

		return extractedChanel;
	}

	private static ushort CheckLanguage(string playerInput, ref int specialCharCount)
	{
		if (playerInput.Length - specialCharCount < 2)
		{
			//Radio channels go first the language channels so if they exists and the input length is too short
			//dont check for languages
			return 0;
		}

		var firstLetter = playerInput[specialCharCount];
		if (firstLetter == ',')
		{
			var secondLetter = char.ToLower(playerInput[specialCharCount + 1]);

			var language = LanguageManager.Instance.GetLanguageByKey(secondLetter);
			if (language == null) return 0;

			specialCharCount += 2;

			var canUse = PlayerManager.LocalPlayerScript.MobLanguages.CanSpeakLanguage(language);

			return canUse ? language.LanguageUniqueId : (ushort) 0;
		}

		return 0;
	}

	/// <summary>
	/// Checks if chat message is valid and can be send over the network
	/// </summary>
	/// <param name="message">The player message from chat</param>
	/// <returns></returns>
	public static bool IsValidToSend(string message)
	{
		if (message == null)
			return false;

		return !string.IsNullOrEmpty(message.Trim());
	}

	private readonly Dictionary<Speech, ChatModifier> CharacterSpeech = new Dictionary<Speech, ChatModifier>()
	{
		{Speech.None, ChatModifier.None},
		{Speech.Canadian, ChatModifier.Canadian},
		{Speech.French, ChatModifier.French},
		{Speech.Italian, ChatModifier.Italian},
		{Speech.Swedish, ChatModifier.Swedish},
		{Speech.Chav, ChatModifier.Chav},
		{Speech.Stutter, ChatModifier.Stutter},
		{Speech.Scotsman, ChatModifier.Scotsman}
	};

	private static bool CheckForEmoteAction(string emote)
	{
		return EmoteActionManager.HasEmote(emote);
	}
	private static void DoEmoteAction(string emoteName, GameObject player)
	{
		EmoteActionManager.DoEmote(emoteName, player);
	}
}
