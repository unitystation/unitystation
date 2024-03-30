namespace Adrenak.UniVoice.AudioSourceOutput {
    [System.Obsolete("InbuiltAudioBuffer as been renamed to CircularAudioClip.")]
    public class InbuiltAudioBuffer : CircularAudioClip {
        public InbuiltAudioBuffer(int frequency, int channels, int segDataLen, int segCount = 3, string clipName = null) 
        : base(frequency, channels, segDataLen, segCount, clipName) {
        }
    }
}
