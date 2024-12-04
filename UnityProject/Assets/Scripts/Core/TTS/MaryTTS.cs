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
		_ = RequestSynth(textToSynth, voice, bytes => audioSource.PlayOneShot(WavUtility.ToAudioClip(bytes, 0, "TTS_Clip")));
	}

	async Task RequestSynth(string textToSynth, string voice, Action<byte[]> callback)
	{
		if (string.IsNullOrWhiteSpace(voice))
		{
			voice = "Male 01";
		}
		byte[] responseData = await TTSCommunication.GenTTS(textToSynth, voice);
		LoadManager.DoInMainThread(() => { callback.Invoke(responseData); });
	}
}