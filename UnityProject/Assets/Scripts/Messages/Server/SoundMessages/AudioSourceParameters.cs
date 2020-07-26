using Newtonsoft.Json;

namespace Assets.Scripts.Messages.Server.SoundMessages
{
	public enum MixerType
	{
		Default,
		Muffled
	}

	/// <summary>
	/// Structure to provide any AudioSource special parameters when playing a sound with the PlaySoundMessage
	/// </summary>
	public class AudioSourceParameters
	{
		public float Volume { get; set; } = -1; // -1 means to use the "ambient sound volume".  Any positive value will override the default sound volume.
		public float Time { get; set; } = 0;
		public float Pan { get; set; } = 0;

		// The Output Mixer to use
		public MixerType MixerType { get; set; } = MixerType.Default;

		// Pitch of the sound
		public float Pitch { get; set; } = -1;

		// Spatial blend of the audio source (0 for 2D, 1 for 3D)
		// Note:  2D spatial blend doesn't attenuate with distance
		public float SpatialBlend { get; set; } = 0;

		// Minimum distance in which the sound is at maximum volume
		public float MinDistance { get; set; } = 1;

		// (Logarithmic rolloff) MaxDistance is the distance a sound stops attenuating at.
		public float MaxDistance { get; set; } = 10;

		public override string ToString()
		{
			return $"{nameof(Volume)}: {Volume}, {nameof(Time)}: {Time}, {nameof(Pan)}: {Pan}, {nameof(MixerType)}: {MixerType}, {nameof(Pitch)}: {Pitch}, {nameof(SpatialBlend)}: {SpatialBlend}, {nameof(MinDistance)}: {MinDistance}, {nameof(MaxDistance)}: {MaxDistance}";
		}
	}
}
