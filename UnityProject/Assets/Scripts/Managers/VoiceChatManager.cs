using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Adrenak.BRW;
using Adrenak.UniMic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.AudioSourceOutput;
using Adrenak.UniVoice.MirrorNetwork;
using Adrenak.UniVoice.UniMicInput;
using Initialisation;
using Logs;
using Messages.Client;
using Messages.Server;
using Mirror;
using NUnit.Framework.Constraints;
using Shared.Managers;
using UnityEngine;

public class VoiceChatManager : NetworkBehaviour, IInitialise
{

	//preferences
	//TODO Volume??
	//TODO chat Icon when someone is speaking
	//TODO Is alive Calculations

	public static VoiceChatManager Instance;

	public AudioSource AudioPrefab;

	private ChatroomAgent chatroomAgent;

	private UniVoiceMirrorNetwork UniVoiceMirrorNetwork;

	[SyncVar(hook = nameof(SyncEnabled))]
	public bool Enabled = false;

	public event Action OnEnabledChange;

	public bool ClientEnabled = false;

	public bool ClientPushToTalk = true;

	public bool ClientPushToTalkPressed = true;

	public static List<ServerVoiceData.UniVoiceMessage> CachedMessage = new  List<ServerVoiceData.UniVoiceMessage>();

	public void Awake()
	{

		Instance = this;

		ClientEnabled = PlayerPrefs.GetInt(PlayerPrefKeys.VoiceChatToggle, 1) == 1;
		ClientPushToTalk = PlayerPrefs.GetInt(PlayerPrefKeys.PushToTalkToggle, 1) == 1;
	}



	public void SyncEnabled(bool Oldv, bool Newv)
	{
		Enabled = Newv;
		if (Newv && chatroomAgent == null)
		{
			SetUp();
			OnEnabledChange?.Invoke();
		}
		else if (Oldv && Newv == false && chatroomAgent != null)
		{
			NetworkManager.singleton.transport.OnClientConnected -= UniVoiceMirrorNetwork.Client_OnConnected;
			NetworkManager.singleton.transport.OnClientDisconnected -= UniVoiceMirrorNetwork.Client_OnDisconnected;

			// When a client joins and leaves the server
			NetworkManager.singleton.transport.OnServerConnected -= UniVoiceMirrorNetwork.Server_OnClientConnected;
			NetworkManager.singleton.transport.OnServerDisconnected -= UniVoiceMirrorNetwork.Server_OnClientDisconnected;
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			chatroomAgent.Dispose();
			chatroomAgent = null;
			OnEnabledChange?.Invoke();
		}

	}

	public void SetUp()
	{
		UniVoiceMirrorNetwork = new UniVoiceMirrorNetwork();
		SetUpUniVoiceMirrorNetwork();
		chatroomAgent = new ChatroomAgent (
			UniVoiceMirrorNetwork,
			new UniVoiceUniMicInput(0, 8000, 27),
			new UniVoiceAudioSourceOutput.Factory(AudioPrefab)
		);
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);

		chatroomAgent.MuteSelf = ClientPushToTalk || ClientEnabled;



		if (isServer)
		{
			foreach (var Player in PlayerList.Instance.loggedIn)
			{
				if (Player.Connection == null) continue;
				UniVoiceMirrorNetwork.Server_OnClientConnected(Player.Connection.connectionId);
			}
		}

	}

	public void Client_OnMessage(ServerVoiceData.UniVoiceMessage message)
	{
		UniVoiceMirrorNetwork.Client_OnMessage(message);
	}

	public void Server_OnMessage(NetworkConnectionToClient connection, ClientVoiceData.UniVoiceMessage message)
	{
		if (Enabled == false) return;
		UniVoiceMirrorNetwork.Server_OnMessage(connection,message);
	}

	public void UpdateMe()
	{
		if (ClientEnabled == false)
		{
			chatroomAgent.MuteSelf = true;
			MicrophoneIcon.Instance.gameObject.SetActive(false);
		}
		else if (ClientPushToTalk && ClientPushToTalkPressed == false)
		{
			chatroomAgent.MuteSelf = true;
			MicrophoneIcon.Instance.gameObject.SetActive(false);
		}
		else
		{
			chatroomAgent.MuteSelf = false;
			MicrophoneIcon.Instance.gameObject.SetActive(true);
		}

		UniVoiceMirrorNetwork.OnUpdate();

	}

	public void SetUpUniVoiceMirrorNetwork() {


		// Client joining and leaving a server
		NetworkManager.singleton.transport.OnClientConnected += UniVoiceMirrorNetwork.Client_OnConnected;
		NetworkManager.singleton.transport.OnClientDisconnected += UniVoiceMirrorNetwork.Client_OnDisconnected;

		// When a client joins and leaves the server
		NetworkManager.singleton.transport.OnServerConnected += UniVoiceMirrorNetwork.Server_OnClientConnected;
		NetworkManager.singleton.transport.OnServerDisconnected += UniVoiceMirrorNetwork.Server_OnClientDisconnected;

	}

	public InitialisationSystems Subsystem => InitialisationSystems.VoiceChat;

	public void Initialise()
	{
	}
}
