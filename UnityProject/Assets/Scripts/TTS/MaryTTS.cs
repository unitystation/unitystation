using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

public class MaryTTS : MonoBehaviour
{
    public static MaryTTS Instance;

    private const string requestURL = "http://play.unitystation.org:59125/process";
    private MaryVoiceSettings defaultConfig = new MaryVoiceSettings();
    public AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } //else gets destroyed by parent
    }

    public void Synthesize(string textToSynth)
    {
        StartCoroutine(RequestSynth(textToSynth));
    }

    public void Announce(string textToSynth)
    {
        StartCoroutine(PlayAnnouncement(textToSynth));
    }

    IEnumerator RequestSynth(string textToSynth)
    {
        UnityWebRequest request = UnityWebRequest.Get(GetURL(textToSynth));

        yield return request.SendWebRequest();

        if (request.error != null)
        {
            Debug.Log("Err: " + request.error);
        } else
        {
            AudioClip ttsClip = WavUtility.ToAudioClip(request.downloadHandler.data, 0, "TTS_Clip");
            audioSource.PlayOneShot(ttsClip);
        }
    }

    IEnumerator PlayAnnouncement(string textToSynth)
    {
        UnityWebRequest request = UnityWebRequest.Get(GetURL(textToSynth));

        yield return request.SendWebRequest();

        if (request.error != null)
        {
            Debug.Log("Err: " + request.error);
        } else
        {
            Synth.Instance.PlayAnnouncement( request.downloadHandler.data );
        }
    }

    private string GetURL(string textInput)
    {
        return requestURL + defaultConfig.GetConfigString()
            + textInput;
    }
}

public class MaryVoiceSettings
{
    public string InputType = "TEXT";
    public string Audio = "WAVE_FILE";
    public string OutputType = "AUDIO";
    public string Locale = "en_US";

    public string GetConfigString()
    {
        return "?INPUT_TYPE=" + InputType + "&AUDIO="
            + Audio + "&OUTPUT_TYPE=" + OutputType + "&LOCALE="
            + Locale + "&INPUT_TEXT=";
    }
}
