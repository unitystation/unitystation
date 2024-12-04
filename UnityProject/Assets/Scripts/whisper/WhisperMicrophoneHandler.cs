using System;
using Logs;
using Messages.Client;
using SecureStuff;
using Shared.Managers;
using UI.Chat_UI;
using UnityEngine;
using Whisper;
using Whisper.Utils;

public class WhisperMicrophoneHandler : SingletonManager<WhisperMicrophoneHandler>
{
	[HideInInspector] public MicrophoneRecord microphoneRecord;
	private string _buffer;
	[HideInInspector] public WhisperManager whisper;

	public GameObject WhisperManagerPrefab;

	private bool Started;

	public override void Start()
	{
		Started = true;
		base.Start();
		this.gameObject.SetActive(false);
	}

	public void SetUpWhisper()
	{
		Instantiate(WhisperManagerPrefab, this.gameObject.transform);
		microphoneRecord = this.GetComponentInChildren<MicrophoneRecord>();
		whisper  = this.GetComponentInChildren<WhisperManager>();
		microphoneRecord.OnRecordStop += OnRecordStop;
	}

	public void OnDisable()
	{
		if (microphoneRecord != null)
		{
			MicrophoneAccess.ToggleRecordsState(microphoneRecord, false);
		}
	}

	public void OnEnable()
	{
		if (Started == false) return;
		if (MicrophoneAccess.MicEnabledPublic)
		{
			if (microphoneRecord == null)
			{
				SetUpWhisper();
			}

			MicrophoneAccess.ToggleRecordsState(microphoneRecord, true);
		}
		else
		{
			_ = MicrophoneAccess.RequestMicrophone(" So Speech to text can work ");
			this.gameObject.SetActive(false);
		}
	}


	private async void OnRecordStop(AudioChunk recordedAudio)
	{
		_buffer = "";

		var ToUesChatChannel = ChatChannel.Local;
		if (InputManagerWrapper.GetKey(KeyCode.Semicolon)) ToUesChatChannel |= ChatChannel.Common;
		if (InputManagerWrapper.GetKey(KeyCode.B)) ToUesChatChannel |= ChatChannel.Binary;
		if (InputManagerWrapper.GetKey(KeyCode.U)) ToUesChatChannel |= ChatChannel.Supply;
		//if (InputManagerWrapper.GetKey(KeyCode.Y)) ToUesChatChannel |= ChatChannel.CentComm; //Conflicts with opening chat with Local Preselected
		if (InputManagerWrapper.GetKey(KeyCode.C)) ToUesChatChannel |= ChatChannel.Command;
		if (InputManagerWrapper.GetKey(KeyCode.E)) ToUesChatChannel |= ChatChannel.Engineering;
		//if (InputManagerWrapper.GetKey(KeyCode.M)) ToUesChatChannel |= ChatChannel.Medical; //Conflicts with toggling STT (This very thing )
		//if (InputManagerWrapper.GetKey(KeyCode.N)) ToUesChatChannel |= ChatChannel.Science; //Conflicts with toggle voice chat
		//if (InputManagerWrapper.GetKey(KeyCode.S)) ToUesChatChannel |= ChatChannel.Security; //Conflicts with movement key
		if (InputManagerWrapper.GetKey(KeyCode.V)) ToUesChatChannel |= ChatChannel.Service;
		//if (InputManagerWrapper.GetKey(KeyCode.T)) ToUesChatChannel |= ChatChannel.Syndicate; //Conflicts with open chat Shortcut


		var res = await whisper.GetTextAsync(recordedAudio.Data, recordedAudio.Frequency, recordedAudio.Channels);
		if (res == null)
			return;

		var text = res.Result;

		var parsedInput = Chat.ParsePlayerInput(text, null);
		if (Chat.IsValidToSend(parsedInput.ClearMessage) == false) return;


		_ = SoundManager.Play(CommonSounds.Instance.Click01);

		if (ChatUI.Instance.Showing)
		{
			ChatUI.Instance.InputFieldChat.text += text;
			return;
		}

		if (PlayerManager.LocalMindScript.isGhosting)
		{
			PostToChatMessage.Send(text, ChatChannel.Ghost, languageId: 0,Voice:  PlayerManager.LocalMindScript.CurrentCharacterSettings.Voice);
		}
		else if (PlayerManager.LocalMindScript.isGhosting == false)
		{
			PostToChatMessage.Send(text, ToUesChatChannel, languageId: 0,Voice:  PlayerManager.LocalMindScript.CurrentCharacterSettings.Voice); //Languages automatically Set from the server
		}
		else
		{
			PostToChatMessage.Send(text, ChatChannel.OOC, languageId: 0,Voice:  PlayerManager.LocalMindScript.CurrentCharacterSettings.Voice);
		}
	}
}