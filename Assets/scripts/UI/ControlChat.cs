using System;
using System.Text;
using System.Collections.Generic;
using ExitGames.Client.Photon.Chat;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using Network;

namespace UI{
	public class ControlChat : MonoBehaviour,IChatClientListener  
	{


		public string ChatAppId; // set in inspector

		public string[] ChannelsToJoinOnConnect; // set in inspector

		public string[] FriendsList;

		public int HistoryLengthToFetch; // set in inspector. Up to a certain degree, previously sent messages can be fetched for context

		public string UserName { get; set; }

		private string selectedChannelName; // mainly used for GUI/input

		public ChatClient chatClient;

		public GameObject chatInputWindow;
		public InputField usernameInput;
		public RectTransform ChatPanel;     // set in inspector (to enable/disable panel)
		public GameObject UserIdFormPanel;
		public InputField InputFieldChat;   // set in inspector
		public InputField CurrentChannelText;     // set in inspector

		public bool isChatFocus = false;
		private readonly Dictionary<string, Toggle> channelToggles = new Dictionary<string, Toggle>();

		public bool ShowState = true;
	


		private string userIdInput = "";





		public void Start()
		{
			


			chatInputWindow.SetActive (false);

//			ChatPanel.gameObject.SetActive(false);


			if (string.IsNullOrEmpty(UserName))
			{
				UserName = "user" + Environment.TickCount%99; //made-up username
			}

			bool _AppIdPresent = string.IsNullOrEmpty(ChatAppId);
		
			if (UserIdFormPanel != null) {
				this.UserIdFormPanel.gameObject.SetActive (!_AppIdPresent);
			}
			if (string.IsNullOrEmpty(ChatAppId))
			{
				Debug.LogError("You need to set the chat app ID in the inspector in order to continue.");
				return;
			}
		}

		void OnLevelWasLoaded(){

			if (chatClient != null) {
				if (chatClient.CanChat) {
				
					UserIdFormPanel.SetActive (false);
				
				}
			
			
			}


		}

		public void Connect()
		{
			NetworkManager.control.Connect (); //Also connect to the game server!
			if (SoundManager.control != null) {
				SoundManager.control.sounds [5].Play ();
			}
			UserName = usernameInput.text;
			this.chatClient = new ChatClient(this);
			this.chatClient.Connect(ChatAppId, "1.0", new ExitGames.Client.Photon.Chat.AuthenticationValues(UserName));
			this.UserIdFormPanel.gameObject.SetActive(false);
			Debug.Log ("ATTEMPTING CONNECT");
			ReportToChannel ("Connecting as: " + UserName);

		
		}

		/// <summary>To avoid that the Editor becomes unresponsive, disconnect all Photon connections in OnApplicationQuit.</summary>
		public void OnApplicationQuit()
		{
			if (this.chatClient != null)
			{
				this.chatClient.Disconnect();
			}
		}

		public void Update()
		{
			if (this.chatClient != null)
			{
				this.chatClient.Service(); // make sure to call this regularly! it limits effort internally, so calling often is ok!
			}

			if (chatClient != null) {
				if (chatClient.CanChat && !GameData.control.isInGame && UIManager.control.displayControl.tempSceneButton) {
					//TODO: Remove this when a better transition handler is implemented 
					UIManager.control.displayControl.tempSceneButton.SetActive (true);
			
			
				}
			}

			if (chatClient != null) {
				if (Input.GetKeyDown (KeyCode.T) && !isChatFocus && chatClient.CanChat) {

					if (!chatInputWindow.activeSelf) {
				
						chatInputWindow.SetActive (true);
				
					}
					isChatFocus = true;
					EventSystem.current.SetSelectedGameObject (InputFieldChat.gameObject, null);
					InputFieldChat.OnPointerClick (new PointerEventData (EventSystem.current));


				}
			}

			if (isChatFocus) {
			
				if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
				{
					SendChatMessage(this.InputFieldChat.text);
					this.InputFieldChat.text = "";
					CloseChatWindow ();
				}
			
			
			}
				
		}
			

		public void OnClickSend()
		{
			if (this.InputFieldChat != null)
			{
				SoundManager.control.sounds [5].Play ();
				SendChatMessage(this.InputFieldChat.text);
				this.InputFieldChat.text = "";
				CloseChatWindow ();
			}
		}

		public void OnChatCancel(){
			SoundManager.control.sounds [5].Play ();
			this.InputFieldChat.text = "";
			CloseChatWindow ();

		}

		void CloseChatWindow(){

			isChatFocus = false;
			chatInputWindow.SetActive (false);

		}

		public int TestLength = 2048;
		private byte[] testBytes = new byte[2048];

		private void SendChatMessage(string inputLine)
		{
			if (string.IsNullOrEmpty(inputLine))
			{
				return;
			}
			if ("test".Equals(inputLine))
			{
				if (this.TestLength != this.testBytes.Length)
				{
					this.testBytes = new byte[this.TestLength];
				}

				this.chatClient.SendPrivateMessage(this.chatClient.AuthValues.UserId, testBytes, true);
			}


			bool doingPrivateChat = this.chatClient.PrivateChannels.ContainsKey(this.selectedChannelName);
			string privateChatTarget = string.Empty;
			if (doingPrivateChat)
			{
				// the channel name for a private conversation is (on the client!!) always composed of both user's IDs: "this:remote"
				// so the remote ID is simple to figure out

				string[] splitNames = this.selectedChannelName.Split(new char[] { ':' });
				privateChatTarget = splitNames[1];
			}
			//UnityEngine.Debug.Log("selectedChannelName: " + selectedChannelName + " doingPrivateChat: " + doingPrivateChat + " privateChatTarget: " + privateChatTarget);


			if (inputLine[0].Equals('\\'))
			{
				string[] tokens = inputLine.Split(new char[] {' '}, 2);
				if (tokens[0].Equals("\\help"))
				{
					PostHelpToCurrentChannel();
				}
				if (tokens[0].Equals("\\state"))
				{
					int newState = int.Parse(tokens[1]);
					this.chatClient.SetOnlineStatus(newState, new string[] {"i am state " + newState}); // this is how you set your own state and (any) message
				}
				else if ((tokens[0].Equals("\\subscribe") || tokens[0].Equals("\\s")) && !string.IsNullOrEmpty(tokens[1]))
				{
					this.chatClient.Subscribe(tokens[1].Split(new char[] {' ', ','}));
				}
				else if ((tokens[0].Equals("\\unsubscribe") || tokens[0].Equals("\\u")) && !string.IsNullOrEmpty(tokens[1]))
				{
					this.chatClient.Unsubscribe(tokens[1].Split(new char[] {' ', ','}));
				}
				else if (tokens[0].Equals("\\clear"))
				{
					if (doingPrivateChat)
					{
						this.chatClient.PrivateChannels.Remove(this.selectedChannelName);
					}
					else
					{
						ChatChannel channel;
						if (this.chatClient.TryGetChannel(this.selectedChannelName, doingPrivateChat, out channel))
						{
							channel.ClearMessages();
						}
					}
				}
				else if (tokens[0].Equals("\\msg") && !string.IsNullOrEmpty(tokens[1]))
				{
					string[] subtokens = tokens[1].Split(new char[] {' ', ','}, 2);
					if (subtokens.Length < 2) return;

					string targetUser = subtokens[0];
					string message = subtokens[1];
					this.chatClient.SendPrivateMessage(targetUser, message);
				}
				else if ((tokens[0].Equals("\\join") || tokens[0].Equals("\\j")) && !string.IsNullOrEmpty(tokens[1]))
				{
					string[] subtokens = tokens[1].Split(new char[] { ' ', ',' }, 2);

					// If we are already subscribed to the channel we directly switch to it, otherwise we subscribe to it first and then switch to it implicitly
					if (channelToggles.ContainsKey(subtokens[0]))
					{
						ShowChannel(subtokens[0]);
					}
					else
					{
						this.chatClient.Subscribe(new string[] { subtokens[0] });
					}
				}
				else
				{
					Debug.Log("The command '" + tokens[0] + "' is invalid.");
				}
			}
			else
			{
				if (doingPrivateChat)
				{
					this.chatClient.SendPrivateMessage(privateChatTarget, inputLine);
				}
				else
				{
					this.chatClient.PublishMessage(this.selectedChannelName, inputLine);
				}
			}
		}

		public void PostHelpToCurrentChannel()
		{

		}

		public void DebugReturn(ExitGames.Client.Photon.DebugLevel level, string message)
		{
			if (level == ExitGames.Client.Photon.DebugLevel.ERROR)
			{
				UnityEngine.Debug.LogError(message);
			}
			else if (level == ExitGames.Client.Photon.DebugLevel.WARNING)
			{
				UnityEngine.Debug.LogWarning(message);
			}
			else
			{
				UnityEngine.Debug.Log(message);
			}
		}

		public void OnConnected()
		{
			if (this.ChannelsToJoinOnConnect != null && this.ChannelsToJoinOnConnect.Length > 0)
			{
				this.chatClient.Subscribe(this.ChannelsToJoinOnConnect, this.HistoryLengthToFetch);
			}
				

//			UserIdText.text = "Connected as "+ this.UserName;

//			this.ChatPanel.gameObject.SetActive(true);

			if (FriendsList!=null  && FriendsList.Length>0)
			{
				this.chatClient.AddFriends(FriendsList); // Add some users to the server-list to get their status updates
			}

			this.chatClient.SetOnlineStatus(ChatUserStatus.Online); // You can set your online state (without a mesage).
		}

		public void OnDisconnected()
		{
			Debug.Log ("DO DISCONNECTED STUFF");
		}

		public void OnChatStateChange(ChatState state)
		{
			// use OnConnected() and OnDisconnected()
			// this method might become more useful in the future, when more complex states are being used.
			Debug.Log(state.ToString());
			ReportToChannel( state.ToString());
		}

		public void OnSubscribed(string[] channels, bool[] results)
		{

			// Switch to the first newly created channel
			ShowChannel(channels[0]);
			//send a message into each channel. This is NOT a must have!
			foreach (string channel in channels)
			{
//				this.chatClient.PublishMessage(channel, "says 'hi'."); // you don't HAVE to send a msg on join but you could.
			
				ReportToChannel("Welcome to unitystation. Press T to chat");
			}



			/*
        // select first subscribed channel in alphabetical order
			if (this.chatClient.PublicChannels.Count > 0)
			{
				var l = new List<string>(this.chatClient.PublicChannels.Keys);
				l.Sort();
				string selected = l[0];
				if (this.channelToggles.ContainsKey(selected))
				{
					ShowChannel(selected);
					foreach (var c in this.channelToggles)
					{
						c.Value.isOn = false;
					}
					this.channelToggles[selected].isOn = true;
					AddMessageToSelectedChannel(WelcomeText);
				}
			}
			*/


		}



		public void OnUnsubscribed(string[] channels)
		{
			foreach (string channelName in channels)
			{
				if (this.channelToggles.ContainsKey(channelName))
				{
					Toggle t = this.channelToggles[channelName];
					Destroy(t.gameObject);

					this.channelToggles.Remove(channelName);

					Debug.Log("Unsubscribed from channel '" + channelName + "'.");

					// Showing another channel if the active channel is the one we unsubscribed from before
					if (channelName == selectedChannelName && channelToggles.Count > 0)
					{
						IEnumerator<KeyValuePair<string, Toggle>> firstEntry = channelToggles.GetEnumerator();
						firstEntry.MoveNext();

						ShowChannel(firstEntry.Current.Key);

						firstEntry.Current.Value.isOn = true;
					}
				}
				else
				{
					Debug.Log("Can't unsubscribe from channel '" + channelName + "' because you are currently not subscribed to it.");
				}
			}
		}

	

		public void OnPrivateMessage(string sender, object message, string channelName)
		{
			// as the ChatClient is buffering the messages for you, this GUI doesn't need to do anything here
			// you also get messages that you sent yourself. in that case, the channelName is determinded by the target of your msg
		

			byte[] msgBytes = message as byte[];
			if (msgBytes != null)
			{
				Debug.Log("Message with byte[].Length: "+ msgBytes.Length);
			}
			if (this.selectedChannelName.Equals(channelName))
			{
				ShowChannel(channelName);
			}
		}

		public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
		{
			// this is how you get status updates of friends.
			// this demo simply adds status updates to the currently shown chat.
			// you could buffer them or use them any other way, too.

			// TODO: add status updates
			//if (activeChannel != null)
			//{
			//    activeChannel.Add("info", string.Format("{0} is {1}. Msg:{2}", user, status, message));
			//}

			Debug.LogWarning("status: " + string.Format("{0} is {1}. Msg:{2}", user, status, message));
		}

		public void AddMessageToSelectedChannel(string msg)
		{
			ChatChannel channel = null;
			bool found = this.chatClient.TryGetChannel(this.selectedChannelName, out channel);
			if (!found)
			{
				Debug.Log("AddMessageToSelectedChannel failed to find channel: " + this.selectedChannelName);
				return;
			}

			if (channel != null)
			{
				channel.Add("Bot", msg);
			}
		}

		public void OnGetMessages(string channelName, string[] senders, object[] messages)
		{
			if (channelName.Equals(this.selectedChannelName))
			{
				// update text
				ShowChannel(this.selectedChannelName);
			}
		}

		public void ShowChannel(string channelName)
		{
			if (string.IsNullOrEmpty(channelName))
			{
				return;
			}

			ChatChannel channel = null;
			bool found = this.chatClient.TryGetChannel(channelName, out channel);
			if (!found)
			{
				Debug.Log("ShowChannel failed to find channel: " + channelName);
				return;
			}

			this.selectedChannelName = channelName;
			this.CurrentChannelText.text = ToStringMessages(channel);

			foreach (KeyValuePair<string, Toggle> pair in channelToggles)
			{
				pair.Value.isOn = pair.Key == channelName ? true : false;
			}
		}

		//UNDER WORK: ALL OF THE FOLLOWING FUNCTIONS ARE UNDER DEVELOPMENT
		/// <summary>Provides a string-representation of all messages in this channel.</summary>
		/// <returns>All known messages in format "Channle: Sender: Message", line by line.</returns>
		public string ToStringMessages(ChatChannel channel)
		{
			StringBuilder txt = new StringBuilder();
			for (int i = 0; i < channel.Messages.Count; i++)
			{
				txt.AppendLine(string.Format("OOC: {0}: {1}", channel.Senders[i], channel.Messages[i]));
			}
			return txt.ToString();
		}

		public void ReportToChannel(string reportText){

			StringBuilder txt = new StringBuilder (reportText + "\r\n");


			this.CurrentChannelText.text += txt.ToString();

		}
			
}
}
