using SecureStuff;
using Shared.Managers;
using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Chat;
using US13.Core.Input_System;
using US13.Managers;
using US13.Messages.Client;
using US13.Player;
using Whisper;
using Whisper.Utils;

namespace whisper
{
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
			if (CommonInput.GetKey(KeyCode.Semicolon)) ToUesChatChannel |= ChatChannel.Common;
			if (CommonInput.GetKey(KeyCode.B)) ToUesChatChannel |= ChatChannel.Binary;
			if (CommonInput.GetKey(KeyCode.U)) ToUesChatChannel |= ChatChannel.Supply;
			//if (CommonInput.GetKey(KeyCode.Y)) ToUesChatChannel |= ChatChannel.CentComm; //Conflicts with opening chat with Local Preselected
			if (CommonInput.GetKey(KeyCode.C)) ToUesChatChannel |= ChatChannel.Command;
			if (CommonInput.GetKey(KeyCode.E)) ToUesChatChannel |= ChatChannel.Engineering;
			//if (CommonInput.GetKey(KeyCode.M)) ToUesChatChannel |= ChatChannel.Medical; //Conflicts with toggling STT (This very thing )
			//if (CommonInput.GetKey(KeyCode.N)) ToUesChatChannel |= ChatChannel.Science; //Conflicts with toggle voice chat
			//if (CommonInput.GetKey(KeyCode.S)) ToUesChatChannel |= ChatChannel.Security; //Conflicts with movement key
			if (CommonInput.GetKey(KeyCode.V)) ToUesChatChannel |= ChatChannel.Service;
			//if (CommonInput.GetKey(KeyCode.T)) ToUesChatChannel |= ChatChannel.Syndicate; //Conflicts with open chat Shortcut


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
}