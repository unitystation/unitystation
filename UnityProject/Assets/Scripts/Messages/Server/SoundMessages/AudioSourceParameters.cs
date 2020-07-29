using Newtonsoft.Json;

namespace Assets.Scripts.Messages.Server.SoundMessages
{
	public enum MixerType
	{
		Unspecified,
		Master,
		Muffled
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

		// (Logarithmic rolloff) MaxDistance is the distance a sound stops attenuating at.
		public float? MaxDistance { get; set; } = null;

		public override string ToString()
		{
			return $"{nameof(Volume)}: {Volume}, {nameof(Time)}: {Time}, {nameof(Pan)}: {Pan}, {nameof(MixerType)}: {MixerType}, {nameof(Pitch)}: {Pitch}, {nameof(SpatialBlend)}: {SpatialBlend}, {nameof(MinDistance)}: {MinDistance}, {nameof(MaxDistance)}: {MaxDistance}";
		}
	}
}
