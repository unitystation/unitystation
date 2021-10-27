using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using Audio.Containers;
using UnityWebRequest = UnityEngine.Networking.UnityWebRequest;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

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
		StartCoroutine( RequestSynth( textToSynth, bytes => audioSource.PlayOneShot( WavUtility.ToAudioClip( bytes, 0, "TTS_Clip" ) ) ) );
	}
//
//    public void Announce(string textToSynth)
//    {
//	    StartCoroutine( RequestSynth( textToSynth, bytes => Synth.Instance.PlayAnnouncement( bytes ) ) );
//    }

	/// Do whatever you want with resulting bytes in callback (if/when you recieve them)
	public void Synthesize( string textToSynth, Action<byte[]> callback ) {
		StartCoroutine( RequestSynth( textToSynth, bytes => callback?.Invoke( bytes ) ) );
	}

	IEnumerator RequestSynth( string textToSynth, Action<byte[]> callback ) {
		UnityWebRequest request = UnityWebRequest.Get( GetURL( textToSynth ) );

		yield return request.SendWebRequest();

		if ( request.error != null ) {
			Logger.Log( "Err: " + request.error, Category.Audio );
		} else {
			callback.Invoke( request.downloadHandler.data );
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