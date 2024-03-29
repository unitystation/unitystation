using System;

using Adrenak.UniMic;

using UnityEngine;

namespace Adrenak.UniVoice.UniMicInput {
    /// <summary>
    /// An <see cref="IAudioInput"/> implementation based on UniMic.
    /// For more on UniMic, visit https://www.github.com/adrenak/unimic
    /// </summary>
    public class UniVoiceUniMicInput : IAudioInput {
        const string TAG = "UniVoiceUniMicInput";

        public event Action<int, float[]> OnSegmentReady;

        public int Frequency => Mic.Instance.Frequency;

        public int ChannelCount =>
            Mic.Instance.AudioClip == null ? 0 : Mic.Instance.AudioClip.channels;

        public int SegmentRate => 1000 / Mic.Instance.SampleDurationMS;

        public UniVoiceUniMicInput(int deviceIndex = 0, int frequency = 16000, int sampleLen = 100) {
            if (Mic.Instance.Devices.Count == 0)
                throw new Exception("Must have recording devices for Microphone input");

            Mic.Instance.SetDeviceIndex(deviceIndex);
            Mic.Instance.StartRecording(frequency, sampleLen);
            Debug.unityLogger.Log(TAG, "Start recording.");
            Mic.Instance.OnSampleReady += Mic_OnSampleReady;
        }

        void Mic_OnSampleReady(int segmentIndex, float[] samples) {
            OnSegmentReady?.Invoke(segmentIndex, samples);
        }

        public void Dispose() {
            Mic.Instance.OnSampleReady -= Mic_OnSampleReady;
        }
    }
}
