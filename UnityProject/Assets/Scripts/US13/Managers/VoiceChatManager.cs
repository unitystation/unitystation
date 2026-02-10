using System;
using System.Collections.Generic;
using Adrenak.UniVoice.AudioSourceOutput.Runtime;
using Adrenak.UniVoice.MirrorNetwork.Runtime;
using Adrenak.UniVoice.Runtime;
using Adrenak.UniVoice.UniMicInput.Runtime;
using Mirror;
using UniMic.Runtime;
using UnityEngine;
using US13.Core.Initialisation;
using US13.Managers.UpdateManager;
using US13.Messages.Client;
using US13.Messages.Server;
using US13.PlayerPrefs;
using US13.UI;

namespace US13.Managers
{
	public class VoiceChatManager : NetworkBehaviour, IInitialise
	{

		//preferences
		//TODO Volume??
		//TODO chat Icon when someone is speaking
		//TODO Is alive Calculations

		public static VoiceChatManager Instance;

		public AudioSource AudioPrefab;

		private ChatroomAgent chatroomAgent;

		public UniVoiceMirrorNetwork UniVoiceMirrorNetwork;

		public UniVoiceUniMicInput UniVoiceUniMicInput;

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

			ClientEnabled = UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.VoiceChatToggle, 1) == 1;
			ClientPushToTalk = UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.PushToTalkToggle, 1) == 1;
			EventManager.AddHandler(Event.SceneUnloading, RoundEnd);

		}

		public void OnDestroy()
		{
			UpdateManager.UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}


		public void RoundEnd()
		{
			SyncEnabled(Enabled, false);
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
				UpdateManager.UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
				chatroomAgent.Dispose();
				chatroomAgent = null;
				UniVoiceUniMicInput.Dispose();
				UniVoiceUniMicInput = null;
				UniVoiceMirrorNetwork = null;
				Destroy(Mic.Instance.gameObject);
				OnEnabledChange?.Invoke();
				MicrophoneIcon.Instance.gameObject.SetActive(false);
			}

		}

		public void SetUp()
		{
			UniVoiceMirrorNetwork = new UniVoiceMirrorNetwork();
			SetUpUniVoiceMirrorNetwork();
			UniVoiceUniMicInput = new UniVoiceUniMicInput(0, 8000, 27);

			chatroomAgent = new ChatroomAgent (
				UniVoiceMirrorNetwork,
				UniVoiceUniMicInput,
				new UniVoiceAudioSourceOutput.Factory(AudioPrefab)
			);
			UpdateManager.UpdateManager.Add(CallbackType.UPDATE, UpdateMe);

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
}
