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
	public MicrophoneRecord microphoneRecord;
	private string _buffer;
	public WhisperManager whisper;

	private bool Started;

	public override void Start()
	{
		Started = true;
		base.Start();
		this.gameObject.SetActive(false);
	}

	public override void Awake()
	{
		base.Awake();
		microphoneRecord.OnRecordStop += OnRecordStop;


		//Logic. Inside recording
		//button.onClick.AddListener(OnButtonPressed);
	}

	public void OnDisable()
	{
		MicrophoneAccess.ToggleRecordsState(microphoneRecord, false);
	}

	public void OnEnable()
	{
		if (Started == false) return;
		if (MicrophoneAccess.MicEnabledPublic)
		{
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
			PostToChatMessage.Send(text, ChatChannel.Ghost, languageId: 0);
		}
		else if (PlayerManager.LocalMindScript.isGhosting == false)
		{
			PostToChatMessage.Send(text, ChatChannel.Local,
				languageId: 0); //Languages automatically Set from the server
		}
		else
		{
			PostToChatMessage.Send(text, ChatChannel.OOC, languageId: 0);
		}
	}
}