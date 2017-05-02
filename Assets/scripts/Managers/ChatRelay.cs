using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;

public class ChatRelay : NetworkBehaviour {

	public static ChatRelay chatRelay;
	public SyncListString chatlog = new SyncListString();

	public static ChatRelay Instance {
		get {
			if (!chatRelay) {
				chatRelay = FindObjectOfType<ChatRelay>();
			}
			return chatRelay;
		}
	}

	public override void OnStartClient(){
		chatlog.Callback = RefreshChatLog; 
		base.OnStartClient();
	}

	void RefreshChatLog(SyncListString.Operation op, int index){
		UIManager.Chat.CurrentChannelText.text = "";
		foreach (string chatline in chatlog) {
			string curList = UIManager.Chat.CurrentChannelText.text;
			UIManager.Chat.CurrentChannelText.text = curList + chatline + "\r\n";
		}
	}
}

