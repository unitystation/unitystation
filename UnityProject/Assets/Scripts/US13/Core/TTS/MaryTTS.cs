using System;
using System.Threading.Tasks;
using SecureStuff;
using UnityEngine;
using US13.Core.Initialisation;
using US13.Managers;
using Util;

namespace US13.Core.TTS
{
	public class MaryTTS : MonoBehaviour
	{
		public static MaryTTS Instance;

		public AudioSource audioSource;
		public AudioSource AudioSourceRadio;
		public AudioSource AudioSourceRobot;

		public static int Fails = 0;
		private string lastMessage = "";
		private string lastVoice = "";
		public enum AudioSynthType
		{
			NormalSpeech,
			Radio,
			Robot
		}

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			} //else gets destroyed by parent
		}

		private void Start()
		{
			audioSource.outputAudioMixerGroup = AudioManager.Instance.TTSMixer;
			AudioSourceRadio.outputAudioMixerGroup = AudioManager.Instance.TTSMixerRadio;
			AudioSourceRobot.outputAudioMixerGroup = AudioManager.Instance.TTSMixerRobot;
		}

		public void Synthesize(string textToSynth, AudioSynthType type, string voice = "", uint originator = UInt32.MinValue, bool IgnoreRepeatMessage = true)
		{
			if (IgnoreRepeatMessage)
			{
				if ((textToSynth == lastMessage && lastVoice == voice) )
				{
					return;
				}
			}

			if (Fails > 10)
			{
				return;
			}

			lastMessage = textToSynth;
			lastVoice = voice;

			var source = audioSource;
			if (originator != uint.MinValue && type == AudioSynthType.NormalSpeech)
			{
				var originObject = originator.NetIdToGameObject();
				if (originObject != null && originObject.TryGetComponent<AudioSource>(out var speechSource)) source = speechSource;
			}
			else
			{
				switch (type)
				{
					case AudioSynthType.NormalSpeech:
						source = audioSource;
						break;
					case AudioSynthType.Radio:
						source = AudioSourceRadio;
						break;
					case AudioSynthType.Robot:
						source = AudioSourceRobot;
						break;
					default:
						source = audioSource;
						break;
				}
			}

			_ = RequestSynth(textToSynth, voice, bytes => source.PlayOneShot(WavUtility.ToAudioClip(bytes, 0, "TTS_Clip")));
		}

		async Task RequestSynth(string textToSynth, string voice, Action<byte[]> callback)
		{
			if (string.IsNullOrWhiteSpace(voice))
			{
				voice = TTSVoices.GetDefaultPreference();
			}
			byte[] responseData = await TTSCommunication.GenTTS(textToSynth, voice);

			if (responseData == null)
			{
				Fails++;
				return;
			}
			else
			{
				Fails = 0;
			}

			LoadManager.DoInMainThread(() => { callback.Invoke(responseData); });
		}
	}
}