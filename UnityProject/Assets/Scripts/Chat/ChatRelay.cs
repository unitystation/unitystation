using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;


/// <summary>
/// ChatRelay is only to be used internally via Chat.cs
/// Do not change any protection levels in this script
/// </summary>
public class ChatRelay : NetworkBehaviour
{
	public static ChatRelay Instance;

	private ChatChannel namelessChannels;
	public List<ChatEvent> ChatLog { get; } = new List<ChatEvent>();

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
			Chat.RegisterChatRelay(Instance, AddToChatLogServer, AddToChatLogClient, AddPrivMessageToClient);
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
	}

	[Server]
	private void AddToChatLogServer(ChatEvent chatEvent)
	{
		PropagateChatToClients(chatEvent);
	}

	[Server]
	private void PropagateChatToClients(ChatEvent chatEvent)
	{
		List<ConnectedPlayer> players;
		if (chatEvent.matrix != MatrixInfo.Invalid)
		{
			//get players only on provided matrix
			players = PlayerList.Instance.GetPlayersOnMatrix(chatEvent.matrix);
		}
		else
		{
			players = PlayerList.Instance.AllPlayers;
		}

		//Local chat range checks:
		if (chatEvent.channels == ChatChannel.Local || chatEvent.channels == ChatChannel.Combat
													|| chatEvent.channels == ChatChannel.Action)
		{
			//			var speaker = PlayerList.Instance.Get(chatEvent.speaker);
			LayerMask layerMask = LayerMask.GetMask("Walls", "Door Closed");
			for (int i = 0; i < players.Count(); i++)
			{
				if (players[i].Script == null)
				{
					//joined viewer, don't message them
					players.Remove(players[i]);
					continue;
				}

				if (players[i].Script.IsGhost)
				{
					//send all to ghosts
					continue;
				}

				if (Vector2.Distance(chatEvent.position, //speaker.GameObject.transform.position,
						players[i].GameObject.transform.position) > 14f)
				{
					//Player in the list is too far away for local chat, remove them:
					players.Remove(players[i]);
				}
				else
				{
					//within range, but check if they are in another room or hiding behind a wall
					if (Physics2D.Linecast(chatEvent.position, //speaker.GameObject.transform.position,
						players[i].GameObject.transform.position, layerMask))
					{
						//if it hit a wall remove that player
						players.Remove(players[i]);
					}
				}
			}

			//Get NPCs in vicinity
			var npcMask = LayerMask.GetMask("NPC");
			var npcs = Physics2D.OverlapCircleAll(chatEvent.position, 14f, npcMask);
			foreach (Collider2D coll in npcs)
			{
				if (!Physics2D.Linecast(chatEvent.position,
					coll.transform.position, layerMask))
				{
					//NPC is in hearing range, pass the message on:
					var mobAi = coll.GetComponent<MobAI>();
					if (mobAi != null)
					{
						mobAi.LocalChatReceived(chatEvent);
					}
				}
			}
		}

		for (var i = 0; i < players.Count; i++)
		{
			ChatChannel channels = chatEvent.channels;

			if (channels.HasFlag(ChatChannel.Combat) || channels.HasFlag(ChatChannel.Local) ||
				channels.HasFlag(ChatChannel.System) || channels.HasFlag(ChatChannel.Examine) ||
				channels.HasFlag(ChatChannel.Action))
			{
				if (!channels.HasFlag(ChatChannel.Binary) || players[i].Script.IsGhost)
				{
					UpdateChatMessage.Send(players[i].GameObject, channels, chatEvent.modifiers, chatEvent.message, chatEvent.messageOthers,
						chatEvent.originator, chatEvent.speaker);
					continue;
				}
			}

			if (players[i].Script == null)
			{
				channels &= ChatChannel.OOC;
			}
			else
			{
				channels &= players[i].Script.GetAvailableChannelsMask(false);
			}

			//if the mask ends up being a big fat 0 then don't do anything
			if (channels != ChatChannel.None)
			{
				UpdateChatMessage.Send(players[i].GameObject, channels, chatEvent.modifiers, chatEvent.message, chatEvent.messageOthers,
					chatEvent.originator, chatEvent.speaker);
			}
		}

		if (RconManager.Instance != null)
		{
			string name = "";
			if ((namelessChannels & chatEvent.channels) != chatEvent.channels)
			{
				name = "<b>[" + chatEvent.channels + "]</b> ";
			}

			RconManager.AddChatLog(name + chatEvent.message);
		}
	}

	[Client]
	private void AddToChatLogClient(string message, ChatChannel channels)
	{
		UpdateClientChat(message, channels);
	}

	[Client]
	private void AddPrivMessageToClient(string message, string adminId)
	{
		trySendingTTS(message);

		ChatUI.Instance.AddAdminPrivEntry(message, adminId);
	}

	[Client]
	private void UpdateClientChat(string message, ChatChannel channels)
	{
		if (string.IsNullOrEmpty(message)) return;

		trySendingTTS(message);

		if (PlayerManager.LocalPlayerScript == null)
		{
			channels = ChatChannel.OOC;
		}

		if (channels != ChatChannel.None)
		{
			ChatUI.Instance.AddChatEntry(message);
		}
	}

	///Runs the espeak program with a message. After inital implementation additional arguments will be passed.
	///Windows/Mac might need some tweaking as this was tested on Linux
	private void StartEspeak(string message)
	{
	string relative_Path = "";
	///Note due to error in upstream espeak-ng it expects the folder to be named espeak-data. NOT espeak-ng-data. I could fix this on my fork but there's no benefit to doing so.
	///Also it requires you to leave the optional second / off. --path in general is kind of shit. Maybe I could help upstream here :thinking:
	string SS13DataPath = GameData.SS13DATAPATH;
	Process espeak = new Process();
	espeak.StartInfo.RedirectStandardOutput = true;
	espeak.StartInfo.RedirectStandardError = true;         
	espeak.StartInfo.UseShellExecute = false; 
	espeak.StartInfo.CreateNoWindow = true;
	#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
		relative_Path = @"/StreamingAssets/Espeak/Windows";
		espeak.StartInfo.FileName = SS13DataPath + "/StreamingAssets/Espeak/Windows/espeak-ng.exe";
	#endif
	///#if UNITY_STANDALONE_OSX
		///relative_Path = "/Resources/Data/StreamingAssets/Espeak/MacOS/share ";
		///espeak.StartInfo.FileName = SS13DataPath + "/Resources/Data/StreamingAssets/Espeak/MacOS/espeak";
	///#endif
	//#if UNITY_EDITOR_OSX
		//relative_Path = "/StreamingAssets/Espeak/MacOS/share";
		//espeak.StartInfo.FileName= SS13DataPath +  "/StreamingAssets/Espeak/MacOS/espeak";
	//#ENDIF
	#if (UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX)
		relative_Path = "/StreamingAssets/Espeak/Linux/share";
		espeak.StartInfo.FileName= SS13DataPath +  "/StreamingAssets/Espeak/Linux/espeak";
	#endif
	espeak.StartInfo.Arguments = "--path=\"" +SS13DataPath + relative_Path + "\" " + message;
        espeak.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
	#if DEVELOPMENT_BUILD
	UnityEngine.Debug.Log("This was passed to espeak: " + espeak.StartInfo.Arguments);
	#endif
        espeak.Start();

	///string stdoutx = .StandardOutput.ReadToEnd();         
        string stderrx = espeak.StandardError.ReadToEnd();             
	espeak.WaitForExit();


	///UnityEngine.Debug.Log("Espeak Stdout : ", stdoutx);
       UnityEngine.Debug.Log("Espeak Stderr : " + stderrx); ///It's always useful knowing if a program you tried to call threw an exception so I'm leaving this one uncommented.
	}

	/// <summary>
	/// Sends a message to TTS to vocalize.
	/// They are required to contain the saysChar.
	/// Messages must also contain at least one letter from the alphabet.
	/// </summary>0020
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
					#if UNITY_STANDALONE_OSX
						MaryTTS.Instance.Synthesize(messageAfterSaysChar);	
					#else
					///This regex is slightly suboptimal, as I couldn't get spaces to work when replacing with a blank space,
					///but by replacing every one with a space, it won't affect pronounciation as espeak does not care about consecutive spaces.
					Regex alphanumpunc = new Regex("[^abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.,;?! ]");
					messageAfterSaysChar = alphanumpunc.Replace(messageAfterSaysChar, "");
					///We just stripped out all double quotes but we need the surrounding ones back still.
					messageAfterSaysChar = "\"" + messageAfterSaysChar + "\"";
					Thread espeakThread = new Thread(() => StartEspeak(messageAfterSaysChar));
					espeakThread.IsBackground = true;
					espeakThread.Start();
					#endif
				}
			}
		}
	}

}