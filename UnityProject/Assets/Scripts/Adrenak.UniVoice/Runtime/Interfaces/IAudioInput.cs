using System;

namespace Adrenak.UniVoice {
    /// <summary>
    /// Source of user voice input. This would usually be implemented 
    /// over a microphone to get the users voice. But it can also be used
    /// in other ways such as streaming an mp4 file from disk. It's just 
    /// an input and the source doesn't matter.
    /// </summary>
    public interface IAudioInput : IDisposable {
        /// <summary>
        /// Fired when a segment (sequence of audio samples) is ready
        /// </summary>
        event Action<int, float[]> OnSegmentReady;

        /// <summary>
        /// The sampling frequency of the audio
        /// </summary>
        int Frequency { get; }

        /// <summary>
        /// The number of channels in the audio
        /// </summary>
        int ChannelCount { get; }

        /// <summary>
        /// The number of segments (a segment is a sequence of audio samples)
        /// that are emitted from the source every second.
        /// Eg. A 16000 Hz source with one channel at a rate of 10 
        /// will output an array of 1600 samples every 100 milliseconds.
        /// A 44000 Hz source with two channels at a rate of 10 
        /// will output an array of 8800 samples every 100 milliseconds.
        /// </summary>
        int SegmentRate { get; }
    }
}
