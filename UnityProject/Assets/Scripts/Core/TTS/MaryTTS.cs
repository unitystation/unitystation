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

public class MaryTTS : MonoBehaviour {
	public static MaryTTS Instance;

	private const string requestURL = "http://play.unitystation.org:59125/process";
	private MaryVoiceSettings defaultConfig = new MaryVoiceSettings();
	public AudioSource audioSource;

	private void Awake() {
		if ( Instance == null ) {
			Instance = this;
		} //else gets destroyed by parent
	}

	private void Start() {
		audioSource.outputAudioMixerGroup = AudioManager.Instance.TTSMixer;
	}

	public void Synthesize( string textToSynth ) {
		_= RequestSynth( textToSynth, bytes => audioSource.PlayOneShot( WavUtility.ToAudioClip( bytes, 0, "TTS_Clip" ) ) ) ;
	}
//
//    public void Announce(string textToSynth)
//    {
//	    StartCoroutine( RequestSynth( textToSynth, bytes => Synth.Instance.PlayAnnouncement( bytes ) ) );
//    }

	/// Do whatever you want with resulting bytes in callback (if/when you recieve them)
	public void Synthesize( string textToSynth, Action<byte[]> callback ) {
		_= RequestSynth( textToSynth, bytes => callback?.Invoke( bytes ) ) ;
	}

	async Task RequestSynth( string textToSynth, Action<byte[]> callback )
	{

		try
		{
			HttpResponseMessage response = await SafeHttpRequest.GetAsync(GetURL(textToSynth));

			if (response.IsSuccessStatusCode == false)
			{
				Loggy.LogError("Err: " + response.ReasonPhrase);
			}
			else
			{
				byte[] responseData = await response.Content.ReadAsByteArrayAsync();
				LoadManager.DoInMainThread(() => { callback.Invoke(responseData); });
			}
		}
		catch (Exception e)
		{
			Loggy.LogError(e.ToString());
		}
	}

	private string GetURL( string textInput ) {
		return requestURL + defaultConfig.GetConfigString() + textInput;
	}
}

public class MaryVoiceSettings {
	public string InputType = "TEXT";
	public string Audio = "WAVE_FILE";
	public string OutputType = "AUDIO";
	public string Locale = "en_US";

	public string GetConfigString() {
		return "?INPUT_TYPE=" + InputType + "&AUDIO="
		       + Audio + "&OUTPUT_TYPE=" + OutputType + "&LOCALE="
		       + Locale + "&INPUT_TEXT=";
	}
}