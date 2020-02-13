using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using System.Text;
using Mirror;
using Tilemaps.Behaviours.Meta;

public partial class Chat
{
	private static Dictionary<string, UniqueQueue<DestroyChatMessage>> messageQueueDict = new Dictionary<string, UniqueQueue<DestroyChatMessage>>();
	private static Coroutine composeMessageHandle;
	private static StringBuilder stringBuilder = new StringBuilder();
	private struct DestroyChatMessage
	{
		public string Message;
		public Vector2Int WorldPosition;
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
	public Color localColor;
	public Color combatColor;
	public Color warningColor;
	public Color defaultColor;

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
	private static (string, ChatModifier) ProcessMessage(ConnectedPlayer sentByPlayer, string message)
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

		if (playerConsciousState == ConsciousState.UNCONSCIOUS || playerConsciousState == ConsciousState.DEAD)
		{
			// Only the Mute modifier matters if the player cannot speak. We can skip everything else.
			return (message, ChatModifier.Mute);
		}

		// Emote
		if (message.StartsWith("/me "))
		{
			message = message.Substring(4);
			chatModifiers |= ChatModifier.Emote;
		}
		// Whisper
		else if (message.StartsWith("#"))
		{
			message = message.Substring(1);
			chatModifiers |= ChatModifier.Whisper;
		}
		// Involuntaly whisper due to not being fully concious
		else if (playerConsciousState == ConsciousState.BARELY_CONSCIOUS)
		{
			chatModifiers |= ChatModifier.Whisper;
		}
		// Yell
		else if ((message == message.ToUpper(CultureInfo.InvariantCulture) // Is it all caps?
			&& message.Any(char.IsLetter)))// AND does it contain at least one letter?
		{
			chatModifiers |= ChatModifier.Yell;
		}
		// Question
		else if (message.EndsWith("?"))
		{
			chatModifiers |= ChatModifier.Question;
		}
		// Exclaim
		else if (message.EndsWith("!"))
		{
			chatModifiers |= ChatModifier.Exclaim;
		}

		// Clown
		if (sentByPlayer.Script.mind != null &&
			sentByPlayer.Script.mind.occupation != null &&
			sentByPlayer.Script.mind.occupation.JobType == JobType.CLOWN)
		{
			int intensity = UnityEngine.Random.Range(1, 4);
			for (int i = 0; i < intensity; i++)
			{
				message += " HONK!";
			}
			chatModifiers |= ChatModifier.Clown;
		}

		// TODO None of the followinger modifiers are currently in use.
		// They have been commented out to prevent compile warnings.

		// Stutter
		//if (false) // TODO Currently players can not stutter.
		//{
		//	//Stuttering people randomly repeat beginnings of words
		//	//Regex - find word boundary followed by non digit, non special symbol, non end of word letter. Basically find the start of words.
		//	Regex rx = new Regex(@"(\b)+([^\d\W])\B");
		//	message = rx.Replace(message, Stutter);
		//	chatModifiers |= ChatModifier.Stutter;
		//}
		//
		//// Hiss
		//if (false) // TODO Currently players can never speak like a snek.
		//{
		//	Regex rx = new Regex("s+|S+");
		//	message = rx.Replace(message, Hiss);
		//	chatModifiers |= ChatModifier.Hiss;
		//}
		//
		//// Drunk
		//if (false) // TODO Currently players can not get drunk.
		//{
		//	//Regex - find 1 or more "s"
		//	Regex rx = new Regex("s+|S+");
		//	message = rx.Replace(message, Slur);
		//	//Regex - find 1 or more whitespace
		//	rx = new Regex(@"\s+");
		//	message = rx.Replace(message, Hic);
		//	//50% chance to ...hic!... at end of sentance
		//	if (UnityEngine.Random.Range(1, 3) == 1)
		//	{
		//		message = message + " ...hic!...";
		//	}
		//	chatModifiers |= ChatModifier.Drunk;
		//}

		return (message, chatModifiers);
	}

	/// <summary>
	/// Processes message further for the chat log.
	/// Adds text styling, color and channel prefixes depending on the message and its modifiers.
	/// </summary>
	/// <returns>The chat message, formatted to suit the chat log.</returns>
	public static string ProcessMessageFurther(string message, string speaker, ChatChannel channels,
		ChatModifier modifiers)
	{
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
		if (channels.HasFlag(ChatChannel.Examine) || channels.HasFlag(ChatChannel.Action)
			|| channels.HasFlag(ChatChannel.Local) && string.IsNullOrEmpty(speaker))
		{
			return AddMsgColor(channels, $"<i>{message}</i>");
		}

		// Skip everything if the message is a local warning
		if (channels.HasFlag(ChatChannel.Warning))
		{
			return AddMsgColor(channels, $"<i>{message}</i>");
		}

		message = StripTags(message);

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
			message = AddMsgColor(channels, $"[ooc] <b>{speaker}: {message}</b>");
			return message;
		}

		//Ghosts don't get modifiers
		if (channels.HasFlag(ChatChannel.Ghost))
		{
			return AddMsgColor(channels, $"[dead] <b>{speaker}</b>: {message}");
		}

		var verb = "says,";

		if ((modifiers & ChatModifier.Mute) == ChatModifier.Mute)
		{
			return "";
		}

		if ((modifiers & ChatModifier.Whisper) == ChatModifier.Whisper)
		{
			verb = "whispers,";
			message = $"<i>{message}</i>";
		}
		else if ((modifiers & ChatModifier.Yell) == ChatModifier.Yell)
		{
			verb = "yells,";
			message = $"<b>{message}</b>";
		}
		else if (message.EndsWith("!"))
		{
			verb = "exclaims,";
		}
		else if (message.EndsWith("?"))
		{
			verb = "asks,";
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

		return AddMsgColor(channels,
			$"{chan}<b>{speaker}</b> {verb}"    // [cmd] Username says,
			+ "  "                              // Two hair spaces. This triggers Text-to-Speech.
			+ "\"" + message + "\"");           // "This text will be spoken by TTS!"
	}

	private static string StripTags(string input)
	{
		//Regex - find "<" followed by any number of not ">" and ending in ">". Matches any HTML tags.
		Regex rx = new Regex("[<][^>]+[>]");
		string output = rx.Replace(input, "");

		return output;
	}

	private static string Slur(Match m)
	{
		string x = m.ToString();
		if (char.IsLower(x[0]))
		{
			x = x + "h";
		}
		else
		{
			x = x + "H";
		}

		return x;
	}

	private static string Hic(Match m)
	{
		string x = m.ToString();
		//10% chance to hic at any given space
		if (Random.Range(1, 11) == 1)
		{
			x = " ...hic!... ";
		}

		return x;
	}

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

	private static string Stutter(Match m)
	{
		string x = m.ToString();
		string stutter = "";
		//20% chance to stutter at any given consonant
		if (Random.Range(1, 6) == 1)
		{
			//Randomly pick how bad is the stutter
			int intensity = Random.Range(1, 4);
			for (int i = 0; i < intensity; i++)
			{
				stutter = stutter + x + "... "; //h... h... h...
			}

			stutter = stutter + x; //h... h... h... h[ello]
		}
		else
		{
			stutter = x;
		}
		return stutter;
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
					AddLocalMsgToChat(msg.Message + postfix, msg.WorldPosition);
				}
				continue;
			}

			//Combined message at average position
			stringBuilder.Clear();

			int averageX = 0;
			int averageY = 0;
			int count = 1;

			while (messageQueue.TryDequeue(out DestroyChatMessage msg))
			{
				if (count > 1)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(msg.Message);
				averageX += msg.WorldPosition.x;
				averageY += msg.WorldPosition.y;
				count++;
			}

			AddLocalMsgToChat(stringBuilder.Append(postfix).ToString(), new Vector2Int(averageX / count, averageY / count));
		}
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

		if (GhostValidationRejection(originator, channels)) return;

		var msg = ProcessMessageFurther(message, speaker, channels, modifiers);
		Instance.addChatLogClient.Invoke(msg, channels);
	}

	private static bool GhostValidationRejection(uint originator, ChatChannel channels)
	{
		if (PlayerManager.PlayerScript == null) return false;
		if (!PlayerManager.PlayerScript.IsGhost) return false;
		if (Instance.GhostHearAll) return false;

		if (NetworkIdentity.spawned.ContainsKey(originator))
		{
			var getOrigin = NetworkIdentity.spawned[originator];
			if (channels == ChatChannel.Local || channels == ChatChannel.Combat
											  || channels == ChatChannel.Action)
			{
				LayerMask layerMask = LayerMask.GetMask("Walls", "Door Closed");
				if (Vector2.Distance(getOrigin.transform.position,
						PlayerManager.LocalPlayer.transform.position) > 14f)
				{
					return true;
				}
				else
				{
					if (Physics2D.Linecast(getOrigin.transform.position,
						PlayerManager.LocalPlayer.transform.position, layerMask))
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
		if (channel.HasFlag(ChatChannel.Local)) return ColorUtility.ToHtmlStringRGBA(Instance.localColor);
		if (channel.HasFlag(ChatChannel.Combat)) return ColorUtility.ToHtmlStringRGBA(Instance.combatColor);
		if (channel.HasFlag(ChatChannel.Warning)) return ColorUtility.ToHtmlStringRGBA(Instance.warningColor);
		return ColorUtility.ToHtmlStringRGBA(Instance.defaultColor); ;
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
	public readonly static Dictionary<char, ChatChannel> ChanelsTags = new Dictionary<char, ChatChannel>()
	{
		{'b',  ChatChannel.Binary},
		{'u', ChatChannel.Supply},
		{'y', ChatChannel.CentComm},
		{'c', ChatChannel.Command },
		{'e', ChatChannel.Engineering },
		{'m', ChatChannel.Medical },
		{'n', ChatChannel.Science },
		{'s', ChatChannel.Security },
		{'v', ChatChannel.Service },
		{'t', ChatChannel.Syndicate },
		{'g', ChatChannel.Ghost }
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
			return new ParsedChatInput(playerInput, playerInput, ChatChannel.None);

		// all extracted channels from special chars 
		ChatChannel extractedChanel = ChatChannel.None;
		// how many special chars we need to delete
		int specialCharCount = 0;

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
				var secondLetter = playerInput[1];
				// let's try find desired chanel
				if (ChanelsTags.ContainsKey(secondLetter))
				{
					extractedChanel = ChanelsTags[secondLetter];
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
						Debug.LogWarning("Chat context is null - can't resolve :h tag");
						extractedChanel = ChatChannel.None;
					}

					specialCharCount = 2;
				}
			}
		}

		// delete all special chars
		var clearMsg = playerInput.Substring(specialCharCount).TrimStart(' ');
		return new ParsedChatInput(playerInput, clearMsg, extractedChanel);
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
}
