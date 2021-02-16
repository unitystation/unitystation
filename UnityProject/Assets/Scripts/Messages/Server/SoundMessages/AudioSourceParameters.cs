using Newtonsoft.Json;

namespace Assets.Scripts.Messages.Server.SoundMessages
{
	public enum MixerType
	{
		Master,
		Muffled
	}

	public enum VolumeRolloffType
	{
		Linear,
		Logarithmic,
		EaseInAndOut
	}

	/// <summary>
	/// Structure to provide any AudioSource special parameters when playing a sound with the PlaySoundMessage
	/// All parameters are 0, false, and undefined by default.
	/// </summary>
	public struct AudioSourceParameters
	{
		public float Volume;
		public float Time;
		public float Pan;

		// The Output Mixer to use
		public MixerType MixerType;

		// Pitch of the sound
		public float Pitch;

		// Spatial blend of the audio source (0 for 2D, 1 for 3D)
		// Note:  2D spatial blend doesn't attenuate with distance
		public float SpatialBlend;

		//Sets the spread angle (in degrees) of a 3d stereo or multichannel sound in speaker space. (0 - 360f)
		public float Spread;

		// Minimum distance in which the sound is at maximum volume
		public float MinDistance;

		// MaxDistance is the distance a sound stops attenuating at.
		public float MaxDistance;

		// The type of curve to attenuate the sound in 3D audio.
		public VolumeRolloffType VolumeRolloffType;

		// True if volume is supposed to be 0.
		// We need this because structs always initilize with with all variables equal to 0.
		public bool IsMute;
		
		/// <Summary>
		/// Constructor for the AudioSourceParameters Struct
		/// </Summary>
		public AudioSourceParameters(float volume, float time, float pan, float pitch,
			float spatialBlend, float spread, float minDistance, float maxDistance, MixerType mixerType, VolumeRolloffType volumeRolloffType, bool isMute)
		{
			Volume = volume;
			Time = time;
			Pan = pan;
			Pitch = pitch;
			SpatialBlend = spatialBlend;
			Spread = spread;
			MinDistance = minDistance;
			MaxDistance = maxDistance;
			MixerType = mixerType;
			VolumeRolloffType = volumeRolloffType;
			IsMute = isMute;
		}

		public override string ToString()
		{
			string mixerTypeValue = MixerType.ToString();
			string volumeRolloffTypeValue = VolumeRolloffType.ToString();

			return $"{nameof(Volume)}: {Volume}, {nameof(Time)}: {Time}, {nameof(Pan)}: {Pan}, {nameof(MixerType)}: {mixerTypeValue}, {nameof(Pitch)}: {Pitch}, {nameof(SpatialBlend)}: {SpatialBlend}, {nameof(Spread)}: {Spread}, {nameof(MinDistance)}: {MinDistance}, {nameof(MaxDistance)}: {MaxDistance}, {nameof(VolumeRolloffType)}: {volumeRolloffTypeValue}";
		}
	}
}
