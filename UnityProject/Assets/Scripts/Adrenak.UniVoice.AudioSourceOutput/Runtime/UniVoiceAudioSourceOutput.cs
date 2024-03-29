using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Adrenak.UniVoice.AudioSourceOutput {
    /// <summary>
    /// This class feeds incoming segments of audio to an AudioBuffer
    /// and plays the buffer's clip on an AudioSource. It also clears segments
    /// of the buffer based on the AudioSource's position.
    /// </summary>
    public class UniVoiceAudioSourceOutput : MonoBehaviour, IAudioOutput {
        const string TAG = "UniVoiceAudioSourceOutput";

        enum Status {
            Ahead,
            Current,
            Behind
        }

        Dictionary<int, Status> segments = new Dictionary<int, Status>();
        int GetSegmentCountByStatus(Status status) {
            var matches = segments.Where(x => x.Value == status);
            if (matches == null) return 0;
            return matches.Count();
        }
        public GameObject ParentTarget { get; private set; }
        public AudioSource AudioSource { get; private set; }
        public int MinSegCount { get; private set; }

        CircularAudioClip circularAudioClip;

        public string ID {
            get => circularAudioClip.AudioClip.name;
            set {
                gameObject.name = "UniVoice Peer #" + value;
                circularAudioClip.AudioClip.name = "UniVoice Peer #" + value;
            }
        }

        [System.Obsolete("Cannot use new keyword to create an instance. Use .New() method instead")]
        public UniVoiceAudioSourceOutput() { }

        /// <summary>
        /// Creates a new instance using the dependencies.
        /// </summary>
        ///
        /// <param name="buffer">
        /// The AudioBuffer that the streamer operates on.
        /// </param>
        ///
        /// <param name="source">
        /// The AudioSource from where the incoming audio is played.
        /// </param>
        ///
        /// <param name="minSegCount">
        /// The minimum number of audio segments <see cref="circularAudioClip"/>
        /// must have for the streamer to play the audio. This value is capped
        /// between 1 and <see cref="CircularAudioClip.SegCount"/> of the
        /// <see cref="circularAudioClip"/> passed.
        /// Default: 0. Results in the value being set to the max possible.
        /// </param>
        public static UniVoiceAudioSourceOutput New
        (CircularAudioClip buffer, AudioSource source, int minSegCount = 0) {
            var ctd = source.gameObject.AddComponent<UniVoiceAudioSourceOutput>();
            DontDestroyOnLoad(ctd.gameObject);

            source.loop = true;
            source.clip = buffer.AudioClip;

            if (minSegCount != 0)
                ctd.MinSegCount = Mathf.Clamp(minSegCount, 1, buffer.SegCount);
            else
                ctd.MinSegCount = buffer.SegCount;
            ctd.circularAudioClip = buffer;
            ctd.AudioSource = source;

            Debug.unityLogger.Log(TAG, $"Created with the following params:" +
            $"buffer SegCount: {buffer.SegCount}" +
            $"buffer SegDataLen: {buffer.SegDataLen}" +
            $"buffer MinSegCount: {ctd.MinSegCount}" +
            $"buffer AudioClip channels: {buffer.AudioClip.channels}" +
            $"buffer AudioClip frequency: {buffer.AudioClip.frequency}" +
            $"buffer AudioClip samples: {buffer.AudioClip.samples}");
            ctd.AudioSource.spatialBlend = 1;
            return ctd;
        }

        int lastIndex = -1;
        /// <summary>
        /// This is to make sure that if a segment is missed, its previous
        /// contents won't be played again when the clip loops back.
        /// </summary>
        private void Update() {
            if (AudioSource.clip == null) return;

            if (ParentTarget != null && gameObject.transform.parent != ParentTarget)
            {
	            gameObject.transform.SetParent(ParentTarget.transform);
	            gameObject.transform.localPosition = Vector3.zero;
	            gameObject.transform.localScale = Vector3.one;
            }

            var index = (int)(AudioSource.GetCurrentNormPosition() * circularAudioClip.SegCount);

            // Check every frame to see if the AudioSource has
            // just moved to a new segment in the AudioBuffer
            if (lastIndex != index) {
                // If so, clear the audio buffer so that in case the
                // AudioSource loops around, the old contents are not played.
                circularAudioClip.Clear(lastIndex);

                segments.EnsureKey(lastIndex, Status.Behind);
                segments.EnsureKey(index, Status.Current);

                lastIndex = index;
            }

            // Check if the number of ready segments is sufficient for us to
            // play the audio. Whereas if the number is 0, we must stop audio
            // and wait for the minimum ready segment count to be met again.
            var readyCount = GetSegmentCountByStatus(Status.Ahead);
            if (readyCount == 0)
                AudioSource.mute = true;
            else if (readyCount >= MinSegCount) {
                AudioSource.mute = false;
                if (!AudioSource.isPlaying)
                    AudioSource.Play();
            }
        }

        /// <summary>
        /// Feeds incoming audio into the audio buffer.
        /// </summary>
        ///
        /// <param name="index">
        /// The absolute index of the segment, as reported by the peer to know
        /// the normalized position of the segment on the buffer
        /// </param>
        ///
        /// <param name="audioSamples">The audio samples being fed</param>
        public void Feed(int index, int frequency, int channelCount, float[] audioSamples, uint Object) {
            // If we already have this index, don't bother
            // It's been passed already without playing.
            ParentTarget = Object.NetIdToGameObject();
            if (segments.ContainsKey(index)) return;

            int locIdx = (int)(AudioSource.GetCurrentNormPosition() * circularAudioClip.SegCount);
            locIdx = Mathf.Clamp(locIdx, 0, circularAudioClip.SegCount - 1);

            var bufferIndex = circularAudioClip.GetNormalizedIndex(index);

            // Don't write to the same segment index that we are reading
            if (locIdx == bufferIndex) return;

            // Finally write into the buffer
            segments.Add(index, Status.Ahead);
            circularAudioClip.Write(index, audioSamples);
        }

        /// <summary>
        /// Feeds an incoming <see cref="ChatroomAudioSegment"/> into the audio buffer.
        /// </summary>
        /// <param name="segment"></param>
        public void Feed(ChatroomAudioSegment segment, uint Object) =>
            Feed(segment.segmentIndex, segment.frequency, segment.channelCount, segment.samples, Object);

        /// <summary>
        /// Disposes the instance by deleting the GameObject of the component.
        /// </summary>
        public void Dispose() {
            Destroy(gameObject);
        }

        /// <summary>
        /// Creates <see cref="UniVoiceAudioSourceOutput"/> instances
        /// </summary>
        public class Factory : IAudioOutputFactory
        {
	        public AudioSource AudioSourcePrefab;

            public int BufferSegCount { get; private set; }
            public int MinSegCount { get; private set; }

            public Factory(AudioSource inAudioSourcePrefab) : this(10, 5, inAudioSourcePrefab) { }

            public Factory(int bufferSegCount, int minSegCount, AudioSource inAudioSourcePrefab) {
                BufferSegCount = bufferSegCount;
                MinSegCount = minSegCount;
                AudioSourcePrefab = inAudioSourcePrefab;
            }

            public IAudioOutput Create(int samplingRate, int channelCount, int segmentLength) {
                return New(
                    new CircularAudioClip(
                        samplingRate, channelCount, segmentLength, BufferSegCount
                    ),
                    Instantiate(AudioSourcePrefab),
                    MinSegCount
                );
            }
        }
    }
}
