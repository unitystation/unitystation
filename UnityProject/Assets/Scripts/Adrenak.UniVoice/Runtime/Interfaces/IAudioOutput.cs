using System;

namespace Adrenak.UniVoice {
    /// <summary>
    /// Responsible for playing audio that is sent to it.
    /// You'd normally want a <see cref="UnityEngine.AudioSource"/>
    /// based implementation to play the audio in Unity. But this class can
    /// be used in other ways such as streaming the received audio to a server
    /// or writing it to a local file. It's just an audio output and the
    /// destination depends on your implementation.
    /// </summary>
    public interface IAudioOutput : IDisposable {
        /// <summary>
        /// An ID associated with this audio output
        /// </summary>
        string ID { get; set; }

        /// <summary>
        /// Feeds the data to the output implementation
        /// </summary>
        ///
        /// <param name="segmentIndex">
        /// The index of the segment of samples from the audio
        /// </param>
        ///
        /// <param name="frequency">
        /// The frequency/sampling rate of the audio
        /// </param>
        ///
        /// <param name="channelCount">
        /// The number of channels in the audio
        /// </param>
        ///
        /// <param name="audioSamples">
        /// The audio samples/segment being fed
        /// </param>
        void Feed(int segmentIndex, int frequency, int channelCount, float[] audioSamples, uint Object);

        /// <summary>
        /// Feeds a <see cref="ChatroomAudioSegment"/> object to the audio output.
        /// </summary>
        /// <param name="segment">The audio data to be sent.</param>
        void Feed(ChatroomAudioSegment segment, uint Object);
    }
}
