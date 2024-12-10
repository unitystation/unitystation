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

	public static int Fails = 0;

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
	}

	public void Synthesize(string textToSynth, string voice = "")
	{
		if (Fails > 10)
		{
			return;
		}

		_ = RequestSynth(textToSynth, voice, bytes => audioSource.PlayOneShot(WavUtility.ToAudioClip(bytes, 0, "TTS_Clip")));
	}

	async Task RequestSynth(string textToSynth, string voice, Action<byte[]> callback)
	{
		if (string.IsNullOrWhiteSpace(voice))
		{
			voice =TTSVoices.GetDefaultPreference();
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