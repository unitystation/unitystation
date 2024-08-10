using System;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Messages.Server.SoundMessages
{
	public enum MixerType
	{
		Master,
		Muffled,
		Ambient,
		SoundFX,
		JukeBox,
		Music
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
		/// <summary>
		/// Unity volume goes from 0 to 1.
		/// </summary>
		public float Volume;
		public float Time;
		public float Pan;

		// The Output Mixer to use
		public MixerType MixerType;

		// Pitch of the sound
		public float Pitch;

		// Spatial blend of the audio source (0 for Prefab default,  1 for 2D, 2 for 3D)
		// Note:  2D spatial blend doesn't attenuate with distance
		// Note: 2D = Global
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

		// If the audio source should loop forever
		public bool Loops;

		/// <Summary>
		/// Constructor for the AudioSourceParameters Struct
		/// </Summary>
		public AudioSourceParameters(float pitch = 0, float volume = 0, float time = 0, float pan = 0,
			float spatialBlend = 0, float spread = 0, float minDistance = 0, float maxDistance = 0,
			MixerType mixerType = MixerType.Master, VolumeRolloffType volumeRolloffType = VolumeRolloffType.Linear,
			bool isMute = false, bool loops = false)
		{
			Pitch = pitch;
			Volume = volume;
			Time = time;
			Pan = pan;
			SpatialBlend = spatialBlend;
			Spread = spread;
			MinDistance = minDistance;
			MaxDistance = maxDistance;
			MixerType = mixerType;
			VolumeRolloffType = volumeRolloffType;
			IsMute = isMute;
			Loops = loops;
		}

		public AudioSourceParameters PitchVariation(float variation)
		{
			Pitch = Random.Range(1 - variation, 1 + variation);
			return this;
		}

		public AudioSourceParameters SetVolume(float volume)
		{
			Volume = Mathf.Clamp(volume, 0f, 1f);
			return this;
		}

		/// <summary>
		/// Forces the sound to be played for everyone regadrless of their position.
		/// </summary>
		public AudioSourceParameters MakeSoundGlobal()
		{
			MinDistance = Single.MaxValue;
			SpatialBlend = 1;
			return this;
		}

		/// <summary>
		/// useful for when unity is acting stupid with specific addressable audio prefabs that cannot be localfied.
		/// </summary>
		public AudioSourceParameters MakeSoundLocal(float numberOfTiles = 12)
		{
			MinDistance = numberOfTiles;
			return this;
		}

		public override string ToString()
		{
			string mixerTypeValue = MixerType.ToString();
			string volumeRolloffTypeValue = VolumeRolloffType.ToString();

			return $"{nameof(Volume)}: {Volume}, {nameof(Time)}: {Time}, {nameof(Pan)}: {Pan}, {nameof(MixerType)}: {mixerTypeValue}, {nameof(Pitch)}: {Pitch}, {nameof(SpatialBlend)}: {SpatialBlend}, {nameof(Spread)}: {Spread}, {nameof(MinDistance)}: {MinDistance}, {nameof(MaxDistance)}: {MaxDistance}, {nameof(VolumeRolloffType)}: {volumeRolloffTypeValue}";
		}
	}
}
