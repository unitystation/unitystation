using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using Systems.MobAIs;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Managers;
using Systems.Ai;
using Messages.Server;
using Messages.Server.SoundMessages;
using Player.Language;
using Systems.Communications;
using TMPro;
using UI.Chat_UI;

/// <summary>
/// ChatRelay is only to be used internally via Chat.cs
/// Do not change any protection levels in this script
/// </summary>
public class ChatRelay : NetworkBehaviour
{
	public static ChatRelay Instance;

	private ChatChannel namelessChannels;
	private LayerMask layerMask;
	private LayerMask npcMask;

	private bool radioCheckIsOnCooldown = false;
	[SerializeField] private float radioCheckRadius = 4f;
	private float whisperFalloffDistance = 2.5f;

	private static readonly List<string> whisperPrefix = new List<string> { "w!", "#", "/w" };

	private RconManager rconManager;

	/// <summary>
	/// The char indicating that the following text is speech.
	/// For example: Player says, [Character goes here]"ALL CLOWNS MUST SUFFER"
	/// </summary>
	private char saysChar = ' '; // This is U+200A, a hair space.

	private void Awake()
	{
		//ensures the static instance is cleaned up after scene changes:
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	public void Start()
	{
		namelessChannels = ChatChannel.Examine | ChatChannel.Local | ChatChannel.None | ChatChannel.System |
		                   ChatChannel.Combat;
		layerMask = LayerMask.GetMask("Door Closed");
		npcMask = LayerMask.GetMask("NPC");

		rconManager = RconManager.Instance;
	}

	private void WhisperCheck(ChatEvent chatEvent)
	{
		var willWhisper = whisperPrefix.Any(prefix => chatEvent.message.Contains(prefix));
		chatEvent.IsWhispering = willWhisper;
	}

	[Server]
	public void PropagateChatToClients(ChatEvent chatEvent)
	{
		List<PlayerInfo> players = PlayerList.Instance.AllPlayers;
		if (chatEvent.originator != null) WhisperCheck(chatEvent);

		bool DistanceCheck(Vector3 playerPos)
		{
			if (Vector2.Distance(chatEvent.position, playerPos) > 14f)
			{
				//Player in the list is too far away for local chat, remove them:
				return false;
			}

			//Within range, but check if they are in another room or hiding behind a wall
			if (MatrixManager.Linecast(chatEvent.position, LayerTypeSelection.Walls,
				    layerMask, playerPos).ItHit)
			{
				//If it hit a wall remove that player
				return false;
			}

			//Player can see the position
			return true;
		}

		//Local chat range checks:
		if (chatEvent.channels.HasFlag(ChatChannel.Local)
		    || chatEvent.channels.HasFlag(ChatChannel.Combat)
		    || chatEvent.channels.HasFlag(ChatChannel.Action))
		{
			for (int i = players.Count - 1; i >= 0; i--)
			{
				if (players[i].Script == null)
				{
					//joined viewer, don't message them
					players.RemoveAt(i);
					continue;
				}

				if (players[i].Script.gameObject == chatEvent.originator)
				{
					//Always send the originator chat to themselves
					continue;
				}

				if (players[i].Script.IsGhost)
				{
					//send all to ghosts
					continue;
				}

				if (chatEvent.position == TransformState.HiddenPos)
				{
					//show messages with no provided position to everyone
					continue;
				}

				//Send chat to PlayerChatLocation pos, usually just the player object but for AI is its vessel
				var playerPosition = players[i].Script.PlayerChatLocation.OrNull()?.AssumedWorldPosServer()
				                     ?? players[i].Script.gameObject.AssumedWorldPosServer();

				//Do player position to originator distance check
				if (DistanceCheck(playerPosition) == false)
				{
					//Distance check failed so if we are Ai, then try send action and combat messages to their camera location
					//as well as if possible
					if (chatEvent.channels.HasFlag(ChatChannel.Local) == false &&
					    players[i].Script.PlayerType == PlayerTypes.Ai &&
					    players[i].Script.TryGetComponent<AiPlayer>(out var aiPlayer) &&
					    aiPlayer.IsCarded == false)
					{
						playerPosition = players[i].Script.gameObject.AssumedWorldPosServer();

						//Check camera pos
						if (DistanceCheck(playerPosition))
						{
							//Camera can see player, allow Ai to see action/combat messages
							continue;
						}
					}

					//Player failed distance checks remove them
					players.RemoveAt(i);
				}
			}

			if (chatEvent.originator != null)
			{
				//Get NPCs in vicinity
				var npcs = Physics2D.OverlapCircleAll(chatEvent.position, 14f, npcMask);
				foreach (Collider2D coll in npcs)
				{
					var npcPosition = coll.gameObject.AssumedWorldPosServer();
					if (MatrixManager.Linecast(chatEvent.position, LayerTypeSelection.Walls,
						layerMask, npcPosition).ItHit == false)
					{
						//NPC is in hearing range, pass the message on: Physics2D.OverlapCircleAll(chatEvent.originator.AssumedWorldPosServer(), 8f, itemsMask);
						//TODO: Make mobAI use chat influencer to avoid dependency
						var mobAi = coll.GetComponent<MobAI>();
						if (mobAi != null)
						{
							mobAi.LocalChatReceived(chatEvent);
						}
					}
				}

				if (radioCheckIsOnCooldown == false) chatEvent = CheckForRadios(chatEvent);
			}
		}

		ChatChannel channel = chatEvent.channels;

		if (channel.HasFlag(ChatChannel.Combat) || channel.HasFlag(ChatChannel.Local) ||
		    channel.HasFlag(ChatChannel.System) || channel.HasFlag(ChatChannel.Examine) ||
		    channel.HasFlag(ChatChannel.Action))
		{

			//Check here to avoid speaking in local when speaking on non verbal channels
			//If local chat check for any Chat.NonVerbalChannels in all the channels sent and don't do local
			var doNotDoLocal = channel.HasFlag(ChatChannel.Local) &&
			                   (chatEvent.allChannels & Chat.NonVerbalChannels) != 0;

			if (doNotDoLocal)
			{
				//Basically if we shouldn't do local due to channels containing binary or some other nonverbal (see above)
				//Then AND in all SpeechChannels, remove local if there none then it means we don't need to be verbal
				//As a channel such as command wont be there
				//This whole system allows for e.g Ai to speak to command and binary (and so do local), but if only binary
				//then no local (Yes it's complicated just for that)
				var channelsCleaned = chatEvent.allChannels;
				channelsCleaned &= Chat.SpeechChannels;
				channelsCleaned ^= ChatChannel.Local;
				doNotDoLocal = channelsCleaned == ChatChannel.None;

				if(doNotDoLocal) return;
			}

			for (int i = 0; i < players.Count; i++)
			{
				SendMessage(chatEvent, players[i].GameObject, channel);
			}

			return;
		}

		for (var i = 0; i < players.Count; i++)
		{
			channel = chatEvent.channels;

			if (players[i].Script == null)
			{
				channel &= ChatChannel.OOC;
			}
			else
			{
				channel &= players[i].Script.GetAvailableChannelsMask(false);
			}

			//if the mask ends up being a big fat 0 then don't do anything
			if (channel != ChatChannel.None)
			{
				SendMessage(chatEvent, players[i].GameObject, channel);
			}
		}

		if (rconManager != null)
		{
			string message = $"{chatEvent.speaker} {chatEvent.message}";
			if ((namelessChannels & chatEvent.channels) != chatEvent.channels)
			{
				message = $"<b>[{chatEvent.channels}]</b> {message}";
			}

			RconManager.AddChatLog(message);
		}
	}

	private static void SendMessage(ChatEvent chatEvent, GameObject playerToSend, ChatChannel channel)
	{
		var copiedString = chatEvent.message;
		PlayerScript playerScript = null;
		ushort languageId = 0;

		//Check to see if the target player can understand the language!
		if (chatEvent.modifiers.HasFlag(ChatModifier.Emote) == false &&
		    chatEvent.language != null && playerToSend.TryGetComponent(out playerScript))
		{
			languageId = chatEvent.language.LanguageUniqueId;

			copiedString = LanguageManager.Scramble(chatEvent.language, playerScript, string.Copy(chatEvent.message));
		}

		if (chatEvent.IsWhispering)
		{
			foreach (var prefix in whisperPrefix)
			{
				copiedString = copiedString.Replace(prefix, "");
			}
		}

		if (string.IsNullOrWhiteSpace(chatEvent.message)) return;

		UpdateChatMessage.Send(playerToSend, channel, chatEvent.modifiers, copiedString, chatEvent.VoiceLevel,
			chatEvent.messageOthers, chatEvent.originator, chatEvent.speaker, chatEvent.stripTags, languageId, chatEvent.IsWhispering);
		ShowChatBubbleToPlayer( playerToSend, ref chatEvent);
	}

	public static void ShowChatBubbleToPlayer(GameObject toShowTo, ref ChatEvent chatEvent)
	{
		if (chatEvent.originator == null) return;

		if (chatEvent.channels != ChatChannel.Local) return;

		if (chatEvent.modifiers.HasFlag(ChatModifier.Emote)) return;

		var msg = "";
		if (chatEvent.IsWhispering)
		{
			if ((toShowTo.transform.position - chatEvent.originator.transform.position).magnitude > 1.5f)
			{
				msg = HideWhisperedText(ref chatEvent.message);
			}
			else
			{
				msg = chatEvent.message;
			}

		}
		else
		{
			msg = chatEvent.message;
		}

		ShowChatBubbleMessage.SendTo(toShowTo,  chatEvent.originator, msg, chatEvent.language);
	}

	public static void HideWhisperedText(ref GameObject originator, ref string message, ref GameObject playerToSend)
	{
		if (originator == null || playerToSend == originator) return;
		if (Vector2.Distance(originator.AssumedWorldPosServer(), playerToSend.AssumedWorldPosServer()) < Instance.whisperFalloffDistance) return;
		message = HideWhisperedText(ref message);
	}

	public static string HideWhisperedText(ref string message)
	{
		var msg = string.Empty;
		foreach (var character in message.ToList())
		{
			var c = character;
			if (DMMath.Prob(50))
			{
				c = '*';
			}
			msg += c;
		}
		return msg;
	}

	private ChatEvent CheckForRadios(ChatEvent chatEvent)
	{
		HandleRadioCheckCooldown();

		// Only spoken messages should be forwarded
		if (chatEvent.channels.HasFlag(ChatChannel.Local) == false)
		{
			return chatEvent;
		}

		//Check for chat three tiles around the player
		foreach (Collider2D coll in Physics2D.OverlapCircleAll(chatEvent.position,
			radioCheckRadius))
		{
			if (chatEvent.originator == coll.gameObject) continue;
			if (coll.gameObject.TryGetComponent<IChatInfluencer>(out var listener) == false || listener.WillInfluenceChat() == false) continue;
			var radioPos = coll.gameObject.AssumedWorldPosServer();
			if (MatrixManager.Linecast(chatEvent.position, LayerTypeSelection.Walls,
				layerMask, radioPos).ItHit == false)
			{
				return listener.InfluenceChat(chatEvent);
			}
		}

		return chatEvent;
	}

	private async void HandleRadioCheckCooldown()
	{
		radioCheckIsOnCooldown = true;
		await Task.Delay(500).ConfigureAwait(false);
		radioCheckIsOnCooldown = false;
	}


	[Client]
	public void AddAdminPrivMessageToClient(string message)
	{
		trySendingTTS(message);

		ChatUI.Instance.AddAdminPrivEntry(message);
	}

	[Client]
	public void AddPrayerPrivMessageToClient(string message)
	{
		trySendingTTS(message);

		ChatUI.Instance.AddChatEntry(message);
	}

	[Client]
	public void AddMentorPrivMessageToClient(string message)
	{
		trySendingTTS(message);

		ChatUI.Instance.AddMentorPrivEntry(message);
	}

	[Client]
	public void UpdateClientChat(string message, ChatChannel channels, bool isOriginator, GameObject recipient,
		Loudness loudness, ChatModifier modifiers, ushort languageId = 0, bool isWhispering = false)
	{
		if (string.IsNullOrWhiteSpace(message)) return;

		trySendingTTS(message);

		if (PlayerManager.LocalPlayerScript == null)
		{
			channels = ChatChannel.OOC;
		}

		if (channels != ChatChannel.None)
		{
			// replace action messages with chat bubble
			if (channels.HasFlag(ChatChannel.Combat) || channels.HasFlag(ChatChannel.Action) ||
			    channels.HasFlag(ChatChannel.Examine) || modifiers.HasFlag(ChatModifier.Emote))
			{
				if (isOriginator)
				{
					ChatBubbleManager.Instance.ShowAction(Regex.Replace(message, "<.*?>", string.Empty), recipient);
				}
			}

			var languageSprite = GetLanguageSprite(languageId);

			ChatUI.Instance.AddChatEntry(message, languageSprite);
		}

		AudioSourceParameters audioSourceParameters = new AudioSourceParameters();
		switch (channels)
		{
			case ChatChannel.Syndicate:
				audioSourceParameters.Volume = PlayerPrefs.GetFloat(PlayerPrefKeys.RadioVolumeKey);
				_ = SoundManager.Play(Chat.Instance.commonSyndicteChannelSound,audioSourceParameters: audioSourceParameters);
				break;
			case ChatChannel.Security:

				audioSourceParameters.Volume = PlayerPrefs.GetFloat(PlayerPrefKeys.RadioVolumeKey);
				_ = SoundManager.Play(Chat.Instance.commonSecurityChannelSound, audioSourceParameters:audioSourceParameters);
				break;
			case ChatChannel.Binary:
			case ChatChannel.Medical:
			case ChatChannel.Command:
			case ChatChannel.Supply:
			case ChatChannel.CentComm:
			case ChatChannel.Science:
			case ChatChannel.Engineering:
			case ChatChannel.Common:
				if (PlayerPrefs.GetInt(PlayerPrefKeys.CommonRadioToggleKey) == 1)
				{
					audioSourceParameters.Volume = PlayerPrefs.GetFloat(PlayerPrefKeys.RadioVolumeKey);
					_ = SoundManager.Play(Chat.Instance.commonRadioChannelSound, audioSourceParameters:audioSourceParameters);
				}

				break;
		}

	}

	private static TMP_SpriteAsset GetLanguageSprite(ushort languageId)
	{
		var language = LanguageManager.Instance.GetLanguageById(languageId);

		if (PlayerManager.LocalPlayerScript != null &&
		    PlayerManager.LocalPlayerScript.TryGetComponent<MobLanguages>(out var playerLanguages) && language != null)
		{
			var canUnderstand = playerLanguages.CanUnderstandLanguage(language);

			if (canUnderstand && language.Flags.HasFlag(LanguageFlags.HideIconIfUnderstood))
			{
				return null;
			}
			else if (canUnderstand == false && language.Flags.HasFlag(LanguageFlags.HideIconIfNotUnderstood))
			{
				return null;
			}
			else
			{
				return language.ChatSprite;
			}
		}

		return null;
	}

	/// <summary>
	/// Sends a message to TTS to vocalize.
	/// They are required to contain the saysChar.
	/// Messages must also contain at least one letter from the alphabet.
	/// </summary>
	/// <param name="message">The message to try to vocalize.</param>
	private void trySendingTTS(string message)
	{
		if (UIManager.Instance.ttsToggle)
		{
			message = Regex.Replace(message, @"<[^>]*>", String.Empty); // Style tags
			int saysCharIndex = message.IndexOf(saysChar);
			if (saysCharIndex != -1)
			{
				string messageAfterSaysChar = message.Substring(message.IndexOf(saysChar) + 1);
				if (messageAfterSaysChar.Length > 0 && messageAfterSaysChar.Any(char.IsLetter))
				{
					MaryTTS.Instance.Synthesize(messageAfterSaysChar);
				}
			}
		}
	}
}
