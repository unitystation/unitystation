using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using Audio.Containers;
using Initialisation;
using Logs;
using Mirror;
using SecureStuff;

public class MaryTTS : MonoBehaviour
{
	public static MaryTTS Instance;

	public AudioSource audioSource;
	public AudioSource AudioSourceRadio;
	public AudioSource AudioSourceRobot;

	public static int Fails = 0;
	private string lastMessage = "";

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

	public void Synthesize(string textToSynth, AudioSynthType type, string voice = "", uint originator = UInt32.MinValue)
	{
		if (Fails > 10 || textToSynth == lastMessage)
		{
			return;
		}
		lastMessage = textToSynth;

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