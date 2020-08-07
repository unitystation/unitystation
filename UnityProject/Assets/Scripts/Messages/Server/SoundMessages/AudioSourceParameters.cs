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
		public float? Volume { get; set; } = null;
		public float? Time { get; set; } = null;
		public float? Pan { get; set; } = null;

		// The Output Mixer to use
		public MixerType MixerType { get; set; } = MixerType.Unspecified;

		// Pitch of the sound
		public float? Pitch { get; set; } = null;

		// Spatial blend of the audio source (0 for 2D, 1 for 3D)
		// Note:  2D spatial blend doesn't attenuate with distance
		public float? SpatialBlend { get; set; } = null;

		// Minimum distance in which the sound is at maximum volume
		public float? MinDistance { get; set; } = null;

		// MaxDistance is the distance a sound stops attenuating at.
		public float? MaxDistance { get; set; } = null;

		// The type of curve to attenuate the sound in 3D audio.
		public VolumeRolloffType VolumeRolloffType { get; set; } = VolumeRolloffType.Unspecified;

		public override string ToString()
		{
			string volumeValue = Volume.HasValue ? Volume.Value.ToString() : "Null";
			string timeValue = Time.HasValue ? Time.Value.ToString() : "Null";
			string panValue = Pan.HasValue ? Pan.Value.ToString() : "Null";
			string mixerTypeValue = MixerType.ToString();
			string pitchValue = Pitch.HasValue ? Pitch.Value.ToString() : "Null";
			string spatialBlendValue = SpatialBlend.HasValue ? SpatialBlend.Value.ToString() : "Null";
			string minDistanceValue = MinDistance.HasValue ? MinDistance.Value.ToString() : "Null";
			string maxDistanceValue = MaxDistance.HasValue ? MaxDistance.Value.ToString() : "Null";
			string volumeRolloffTypeValue = VolumeRolloffType.ToString();

			return $"{nameof(Volume)}: {volumeValue}, {nameof(Time)}: {timeValue}, {nameof(Pan)}: {panValue}, {nameof(MixerType)}: {mixerTypeValue}, {nameof(Pitch)}: {pitchValue}, {nameof(SpatialBlend)}: {spatialBlendValue}, {nameof(MinDistance)}: {minDistanceValue}, {nameof(MaxDistance)}: {maxDistanceValue}, {nameof(VolumeRolloffType)}: {volumeRolloffTypeValue}";
		}
	}
}
