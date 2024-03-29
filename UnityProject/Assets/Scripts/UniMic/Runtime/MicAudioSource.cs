using UnityEngine;

namespace Adrenak.UniMic {
    /// <summary>
    /// A simple AudioSource based component that just plays what 
    /// the <see cref="Mic"/> instance is receiving.
    /// Provides optional feature to start the recording by itself (as a testing tool)
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MicAudioSource : MonoBehaviour {
        public bool startRecordingAutomatically = true;
        [Header("If startRecordingAutomatically is true:")]
        public int recordingFrequency = 44000;
        public int sampleDurationMS = 100;

        void Start() {
            var audioSource = gameObject.GetComponent<AudioSource>();

            var mic = Mic.Instance;

            if(startRecordingAutomatically)
                mic.StartRecording(recordingFrequency, sampleDurationMS);

            mic.OnTimestampedSampleReady += (index, segment) => {
                var clip = AudioClip.Create("clip", mic.SampleLength, mic.AudioClip.channels, mic.AudioClip.frequency, false);
                clip.SetData(segment, 0);
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.Play();
            };
        }
    }

}
