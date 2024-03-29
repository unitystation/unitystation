using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Adrenak.UniMic {
    [ExecuteAlways]
    public class Mic : MonoBehaviour {
        // ================================================
        #region MEMBERS
        // ================================================
        /// <summary>
        /// Whether the microphone is running
        /// </summary>
        public bool IsRecording { get; private set; }

        /// <summary>
        /// The frequency at which the mic is operating
        /// </summary>
        public int Frequency { get; private set; }

        /// <summary>
        /// Last populated audio sample
        /// </summary>
        public float[] Sample { get; private set; }

        /// <summary>
        /// Sample duration/length in milliseconds
        /// </summary>
        public int SampleDurationMS { get; private set; }

        /// <summary>
        /// The length of the sample float array
        /// </summary>
        public int SampleLength {
            get { return Frequency * SampleDurationMS / 1000; }
        }

        /// <summary>
        /// The AudioClip currently being streamed in the Mic
        /// </summary>
        public AudioClip AudioClip { get; private set; }

        /// <summary>
        /// List of all the available Mic devices
        /// </summary>
        public List<string> Devices => Microphone.devices.ToList();

        /// <summary>
        /// Index of the current Mic device in m_Devices
        /// </summary>
        public int CurrentDeviceIndex { get; private set; } = -1;

        /// <summary>
        /// Gets the name of the Mic device currently in use
        /// </summary>
        public string CurrentDeviceName {
            get {
                if (CurrentDeviceIndex < 0 || CurrentDeviceIndex >= Microphone.devices.Length)
                    return string.Empty;
                return Devices[CurrentDeviceIndex];
            }
        }

        int m_SampleCount = 0;
        #endregion

        // ================================================
        #region EVENTS
        // ================================================
        /// <summary>
        /// Invoked when the instance starts Recording.
        /// </summary>
        public event Action OnStartRecording;

        /// <summary>
        /// Invoked everytime an audio frame is collected. Includes the frame count.
        /// NOTE: There isn't much use for the index of a sample. Refer to 
        /// <see cref="OnTimestampedSampleReady"/> for an event that gives you the
        /// unix timestamp with a millisecond precision.
        /// </summary>
        public event Action<int, float[]> OnSampleReady;

        /// <summary>
        /// Invoked everytime an audio sample is collected. Includes the unix timestamp
        /// from when the sample was captured with a millisecond precision.
        /// </summary>
        public event Action<long, float[]> OnTimestampedSampleReady;

        /// <summary>
        /// Invoked when the instance stop Recording.
        /// </summary>
        public event Action OnStopRecording;
        #endregion

        // ================================================
        #region METHODS
        // ================================================

        static Mic m_Instance;
        public static Mic Instance {
            get {
                if(m_Instance == null)
                    m_Instance = FindObjectOfType<Mic>();
                if (m_Instance == null) 
                    m_Instance = new GameObject("UniMic.Mic").AddComponent<Mic>();
                return m_Instance;
            }
        }

        // Prevent 'new' keyword construction
        [Obsolete("Mic is a MonoBehaviour class. Use Mic.Instance to get the instance", true)]
        public Mic() { }

        /// <summary>
        /// Ensures an instance of the Mic class
        /// </summary>
        public static Mic Instantiate() {
            return Instance;
        }

        void Awake() {
            if(Application.isPlaying)
                DontDestroyOnLoad(gameObject);
            if (Devices.Count > 0)
                CurrentDeviceIndex = 0;
        }

        /// <summary>
        /// Sets a Mic device for Recording
        /// </summary>
        /// <param name="index">The index of the Mic device. Refer to <see cref="Devices"/> for available devices</param>
        public void SetDeviceIndex(int index) {
            Microphone.End(CurrentDeviceName);
            CurrentDeviceIndex = index;
            if (IsRecording)
                StartRecording(Frequency, SampleDurationMS);
        }

        /// <summary>
        /// Resumes recording at the frequency and sample duration that was 
        /// previously being used.
        /// </summary>
        public void ResumeRecording() {
            StartRecording(Frequency, SampleDurationMS);
        }

        /// <summary>
        /// Starts to stream the input of the current Mic device
        /// </summary>
        public void StartRecording(int frequency = 16000, int sampleDurationMS = 10) {
            StopRecording();
            IsRecording = true;

            Frequency = frequency;
            SampleDurationMS = sampleDurationMS;

            AudioClip = Microphone.Start(CurrentDeviceName, true, 1, Frequency);
            Sample = new float[Frequency / 1000 * SampleDurationMS * AudioClip.channels];

            StartCoroutine(ReadRawAudio());

            OnStartRecording?.Invoke();
        }

        /// <summary>
        /// Ends the Mic stream.
        /// </summary>
        public void StopRecording() {
            if (!Microphone.IsRecording(CurrentDeviceName)) return;

            IsRecording = false;

            Microphone.End(CurrentDeviceName);
            Destroy(AudioClip);
            AudioClip = null;

            StopCoroutine(ReadRawAudio());

            OnStopRecording?.Invoke();
        }

        IEnumerator ReadRawAudio() {
            int loops = 0;
            int readAbsPos = 0;
            int prevPos = 0;
            float[] temp = new float[Sample.Length];

            while (AudioClip != null && Microphone.IsRecording(CurrentDeviceName)) {
                bool isNewDataAvailable = true;

                while (isNewDataAvailable) {
                    int currPos = Microphone.GetPosition(CurrentDeviceName);
                    if (currPos < prevPos)
                        loops++;
                    prevPos = currPos;

                    var currAbsPos = loops * AudioClip.samples + currPos;
                    var nextReadAbsPos = readAbsPos + temp.Length;

                    if (nextReadAbsPos < currAbsPos) {
                        AudioClip.GetData(temp, readAbsPos % AudioClip.samples);

                        Sample = temp;
                        m_SampleCount++;
                        OnSampleReady?.Invoke(m_SampleCount, Sample);

                        OnTimestampedSampleReady?.Invoke(
                            (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds),
                            Sample
                        );

                        readAbsPos = nextReadAbsPos;
                        isNewDataAvailable = true;
                    }
                    else
                        isNewDataAvailable = false;
                }
                yield return null;
            }
        }
        #endregion

        [Obsolete("UpdateDevices method is no longer needed. Devices property is now always up to date")]
        public void UpdateDevices() { }

        /// <summary>
        /// Changes to a Mic device for Recording
        /// </summary>
        /// <param name="index">The index of the Mic device. Refer to <see cref="Devices"/></param>
        [Obsolete("ChangeDevice may go away in the future. Use SetDeviceIndex instead", false)]
        public void ChangeDevice(int index) {
            SetDeviceIndex(index);
        }
    }
}