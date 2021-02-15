using Newtonsoft.Json;

namespace Assets.Scripts.Messages.Server.SoundMessages
{
	public enum MixerType
	{
		Unspecified,
		Master,
		Muffled
	}

	public enum VolumeRolloffType
	{
		Unspecified,
		Logarithmic,
		Linear,
		EaseInAndOut
	}

	/// <summary>
	/// Structure to provide any AudioSource special parameters when playing a sound with the PlaySoundMessage
	/// </summary>
	public class AudioSourceParameters
	{
		public float Volume = -1f;
		public float Time = -1f;
		public float Pan = -1f;

		// The Output Mixer to use
		public MixerType MixerType = MixerType.Unspecified;

		// Pitch of the sound
		public float Pitch = -1f;

		// Spatial blend of the audio source (0 for 2D, 1 for 3D)
		// Note:  2D spatial blend doesn't attenuate with distance
		public float SpatialBlend = -1f;

		//Sets the spread angle (in degrees) of a 3d stereo or multichannel sound in speaker space. (0 - 360f)
		public float Spread = -1f;

		// Minimum distance in which the sound is at maximum volume
		public float MinDistance = -1f;

		// MaxDistance is the distance a sound stops attenuating at.
		public float MaxDistance = -1f;

		// The type of curve to attenuate the sound in 3D audio.
		public VolumeRolloffType VolumeRolloffType = VolumeRolloffType.Unspecified;

		public override string ToString()
		{
			string volumeValue = Volume.ToString();
			string timeValue = Time.ToString();
			string panValue = Pan.ToString();
			string mixerTypeValue = MixerType.ToString();
			string pitchValue = Pitch.ToString();
			string spatialBlendValue = SpatialBlend.ToString();
			string spreadValue = Spread.ToString();
			string minDistanceValue = MinDistance.ToString();
			string maxDistanceValue = MaxDistance.ToString();
			string volumeRolloffTypeValue = VolumeRolloffType.ToString();

			return $"{nameof(Volume)}: {volumeValue}, {nameof(Time)}: {timeValue}, {nameof(Pan)}: {panValue}, {nameof(MixerType)}: {mixerTypeValue}, {nameof(Pitch)}: {pitchValue}, {nameof(SpatialBlend)}: {spatialBlendValue}, {nameof(Spread)}: {spreadValue}, {nameof(MinDistance)}: {minDistanceValue}, {nameof(MaxDistance)}: {maxDistanceValue}, {nameof(VolumeRolloffType)}: {volumeRolloffTypeValue}";
		}
	}
}
