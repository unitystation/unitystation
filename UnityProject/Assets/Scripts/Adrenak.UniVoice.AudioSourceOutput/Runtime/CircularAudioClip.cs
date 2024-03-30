using UnityEngine;

namespace Adrenak.UniVoice.AudioSourceOutput {
    /// <summary>
    /// Used to arrange irregular, out of order and skipped audio segments for better playback.
    /// </summary>
    public class CircularAudioClip {
        public AudioClip AudioClip { get; private set; }
        public int SegCount { get; private set; }
        public int SegDataLen { get; private set; }

        // Holds the first valid segment index received by the buffer to make sure that future
        // writes are not of older indices
        int firstIndex;

        /// <summary>
        /// Create an instance
        /// </summary>
        /// <param name="frequency">The frequency of the audio</param>
        /// <param name="channels">Number of channels in the audio</param>
        /// <param name="segDataLen">Number of samples in the audio </param>
        /// <param name="segCount">Number of segments stored in buffer </param>
        public CircularAudioClip(
            int frequency,
            int channels,
            int segDataLen,
            int segCount = 3,
            string clipName = null
        ) {
            clipName = clipName ?? "clip";
            AudioClip = AudioClip.Create(
                clipName,
                segDataLen * segCount,
                channels,
                frequency,
                false
            );



            firstIndex = -1;
            SegDataLen = segDataLen;
            SegCount = segCount;
        }

        /// <summary>
        /// Feed an audio segment to the buffer.
        /// </summary>
        ///
        /// <param name="absoluteIndex">
        /// Absolute index of the audio segment from the source.
        /// </param>
        ///
        /// <param name="audioSegment">Audio samples data</param>
        public bool Write(int absoluteIndex, float[] audioSegment) {
            // Reject if the segment length is wrong
            if (audioSegment.Length != SegDataLen) return false;

            if (absoluteIndex < 0 || absoluteIndex < firstIndex) return false;

            // If this is the first segment fed
            if (firstIndex == -1) firstIndex = absoluteIndex;

            // Convert the absolute index into a looped-around index
            var localIndex = GetNormalizedIndex(absoluteIndex);

            // Set the segment at the clip data at the right index
            if (localIndex >= 0)
                AudioClip.SetData(audioSegment, localIndex * SegDataLen);
            return true;
        }

        /// <summary>
        /// Returns the index after looping around the buffer
        /// </summary>
        public int GetNormalizedIndex(int absoluteIndex) {
            if (firstIndex == -1 || absoluteIndex <= firstIndex) return -1;
            return (absoluteIndex - firstIndex) % SegCount;
        }

        /// <summary>
        /// Clears the buffer at the specified local index
        /// </summary>
        /// <param name="index"></param>
        public bool Clear(int index) {
            if (index < 0) return false;

            // If the index is out of bounds, then we
            // loop that around and use the local index
            if (index >= SegCount)
                index = GetNormalizedIndex(index);
            AudioClip.SetData(new float[SegDataLen], index * SegDataLen);
            return true;
        }

        /// <summary>
        /// Clear the entire buffer
        /// </summary>
        public void Clear() {
            AudioClip.SetData(new float[SegDataLen * SegCount], 0);
        }
    }
}