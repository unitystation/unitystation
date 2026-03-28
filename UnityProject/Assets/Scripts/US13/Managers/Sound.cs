using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Messages.Server.SoundMessages;
using Util;

namespace US13.Managers
{
	/// <summary>
	/// Ergonomic facade for playing sounds. Wraps SoundManager with a fluent builder pattern.
	/// Defaults to 3D spatial sound for positional audio.
	///
	/// Usage:
	///   Sound.At(sound, gameObject).PlayNetworked();
	///   Sound.At(sound, gameObject).WithRandomPitch(0.8f, 1.2f).PlayNetworked();
	///   Sound.Global(sound).PlayNetworked();
	///   var token = await Sound.At(sound, gameObject).WithLooping().PlayNetworkedAsync();
	///   Sound.Stop(token);
	///   Sound.Fade(token, 0f, 2f).AndStop().Send();
	/// </summary>
	public static class Sound
	{
		/// <summary>
		/// Start building a positional sound at a GameObject's location.
		/// The sound is 3D spatial by default and attaches to the source object.
		/// </summary>
		public static SoundBuilder At(AddressableAudioSource sound, GameObject source)
		{
			return new SoundBuilder(sound, source.AssumedWorldPosServer(), source, isGlobal: false);
		}

		/// <summary>
		/// Start building a positional sound at a world position.
		/// The sound is 3D spatial by default. Use .WithSource() to attach to an object.
		/// </summary>
		public static SoundBuilder At(AddressableAudioSource sound, Vector3 worldPos)
		{
			return new SoundBuilder(sound, worldPos, sourceObj: null, isGlobal: false);
		}

		/// <summary>
		/// Start building a positional sound from a list (one picked at random) at a GameObject's location.
		/// </summary>
		public static SoundBuilder At(List<AddressableAudioSource> sounds, GameObject source)
		{
			return new SoundBuilder(sounds.PickRandom(), source.AssumedWorldPosServer(), source, isGlobal: false);
		}

		/// <summary>
		/// Start building a positional sound from a list (one picked at random) at a world position.
		/// </summary>
		public static SoundBuilder At(List<AddressableAudioSource> sounds, Vector3 worldPos)
		{
			return new SoundBuilder(sounds.PickRandom(), worldPos, sourceObj: null, isGlobal: false);
		}

		/// <summary>
		/// Start building a global sound heard by everyone regardless of position.
		/// </summary>
		public static SoundBuilder Global(AddressableAudioSource sound)
		{
			return new SoundBuilder(sound, Vector3.zero, sourceObj: null, isGlobal: true);
		}

		/// <summary>
		/// Start building a global sound from a list (one picked at random).
		/// </summary>
		public static SoundBuilder Global(List<AddressableAudioSource> sounds)
		{
			return new SoundBuilder(sounds.PickRandom(), Vector3.zero, sourceObj: null, isGlobal: true);
		}

		/// <summary>
		/// Stop a currently playing sound by its token (networked, tells all clients).
		/// </summary>
		public static void Stop(string token)
		{
			SoundManager.StopNetworked(token);
		}

		/// <summary>
		/// Stop a sound locally and return it to the pool.
		/// </summary>
		public static void StopLocal(string token)
		{
			SoundManager.ClientStop(token, true);
		}

		/// <summary>
		/// Pause a sound locally without returning it to the pool, so it can be resumed later.
		/// </summary>
		public static void PauseLocal(string token)
		{
			SoundManager.ClientStop(token, false);
		}

		/// <summary>
		/// Start building a fade operation on an already-playing sound.
		/// Chain .AndStop() if desired, then call a terminal: .Send(), .SendTo(player), or .Local().
		/// </summary>
		public static FadeBuilder Fade(string token, float targetVolume, float duration)
		{
			return new FadeBuilder(token, targetVolume, duration);
		}

	}

	/// <summary>
	/// Fluent builder for fade operations on already-playing sounds.
	/// Use Sound.Fade() to create, optionally chain .AndStop(), then call a terminal.
	/// </summary>
	public struct FadeBuilder
	{
		private readonly string token;
		private readonly float targetVolume;
		private readonly float duration;
		private bool stopOnComplete;

		internal FadeBuilder(string token, float targetVolume, float duration)
		{
			this.token = token;
			this.targetVolume = targetVolume;
			this.duration = duration;
			stopOnComplete = false;
		}

		/// <summary>
		/// Stop and pool the sound after the fade completes.
		/// </summary>
		public FadeBuilder AndStop()
		{
			stopOnComplete = true;
			return this;
		}

		/// <summary>
		/// Send the fade to all clients (networked).
		/// </summary>
		public void SendToAll()
		{
			FadeSoundMessage.SendToAll(token, targetVolume, duration, stopOnComplete);
		}

		/// <summary>
		/// Send the fade to a specific player only.
		/// </summary>
		public void SendTo(GameObject recipient)
		{
			FadeSoundMessage.Send(recipient, token, targetVolume, duration, stopOnComplete);
		}

		/// <summary>
		/// Run the fade locally (client-side only, no network message).
		/// </summary>
		public void Local()
		{
			SoundManager.ClientFade(token, targetVolume, duration, stopOnComplete);
		}
	}

	/// <summary>
	/// Fluent builder for configuring and playing sounds.
	/// Use Sound.At() or Sound.Global() to create, chain .With*() methods, then call a Play*() terminal.
	/// </summary>
	public struct SoundBuilder
	{
		private readonly AddressableAudioSource sound;
		private readonly Vector3 worldPos;
		private GameObject sourceObj;
		private bool isGlobal;
		private bool polyphonic;

		private float pitch;
		private bool pitchSet;
		private float volume;
		private bool volumeSet;
		private MixerType mixerType;
		private bool mixerSet;
		private float maxDistance;
		private bool maxDistanceSet;
		private float minDistance;
		private bool minDistanceSet;
		private bool loops;
		private bool isMute;
		private float fadeInDuration;

		internal SoundBuilder(AddressableAudioSource sound, Vector3 worldPos, GameObject sourceObj, bool isGlobal)
		{
			this.sound = sound;
			this.worldPos = worldPos;
			this.sourceObj = sourceObj;
			this.isGlobal = isGlobal;
			polyphonic = false;
			pitch = 0;
			pitchSet = false;
			volume = 0;
			volumeSet = false;
			mixerType = MixerType.Master;
			mixerSet = false;
			maxDistance = 0;
			maxDistanceSet = false;
			minDistance = 0;
			minDistanceSet = false;
			loops = false;
			isMute = false;
			fadeInDuration = 0;
		}

		/// <summary>
		/// Set a random pitch between min and max (e.g. 0.8f, 1.2f).
		/// </summary>
		public SoundBuilder WithRandomPitch(float min, float max)
		{
			pitch = Random.Range(min, max);
			pitchSet = true;
			return this;
		}

		/// <summary>
		/// Set pitch variation around 1.0 (e.g. 0.05f means pitch between 0.95 and 1.05).
		/// </summary>
		public SoundBuilder WithPitchVariation(float variation)
		{
			pitch = Random.Range(1f - variation, 1f + variation);
			pitchSet = true;
			return this;
		}

		/// <summary>
		/// Set a fixed pitch value (1.0 = normal, lower = slower, higher = faster).
		/// </summary>
		public SoundBuilder WithPitch(float value)
		{
			pitch = value;
			pitchSet = true;
			return this;
		}

		/// <summary>
		/// Set volume (0-1, clamped).
		/// </summary>
		public SoundBuilder WithVolume(float vol)
		{
			volume = Mathf.Clamp(vol, 0f, 1f);
			volumeSet = true;
			return this;
		}

		/// <summary>
		/// Set the output mixer (SoundFX, Ambient, Music, etc.).
		/// </summary>
		public SoundBuilder WithMixer(MixerType mixer)
		{
			mixerType = mixer;
			mixerSet = true;
			return this;
		}

		/// <summary>
		/// Set maximum distance the sound can be heard.
		/// </summary>
		public SoundBuilder WithMaxDistance(float distance)
		{
			maxDistance = distance;
			maxDistanceSet = true;
			return this;
		}

		/// <summary>
		/// Set minimum distance (sound is at full volume within this range).
		/// </summary>
		public SoundBuilder WithMinDistance(float distance)
		{
			minDistance = distance;
			minDistanceSet = true;
			return this;
		}

		/// <summary>
		/// Make the sound loop until stopped via Sound.Stop(token).
		/// </summary>
		public SoundBuilder WithLooping()
		{
			loops = true;
			return this;
		}

		/// <summary>
		/// Attach the sound to a source object (follows the object).
		/// Useful with Sound.At(sound, vector3) when position and source differ.
		/// </summary>
		public SoundBuilder WithSource(GameObject source)
		{
			sourceObj = source;
			return this;
		}

		/// <summary>
		/// Allow overlapping instances of this sound.
		/// </summary>
		public SoundBuilder AsPolyphonic()
		{
			polyphonic = true;
			return this;
		}

		/// <summary>
		/// Fade in from silence to target volume over the given duration after playing.
		/// Automatically starts the sound muted. The fade is dispatched to match the
		/// terminal method used (networked, targeted, or local).
		/// </summary>
		public SoundBuilder WithFadeIn(float duration)
		{
			fadeInDuration = duration;
			isMute = true;
			return this;
		}

		private AudioSourceParameters BuildParameters()
		{
			var parameters = new AudioSourceParameters();

			if (isGlobal)
			{
				parameters = parameters.MakeSoundGlobal();
			}
			else
			{
				// Default to 3D spatial for positional sounds
				parameters.SpatialBlend = 2;
			}

			if (pitchSet) parameters.Pitch = pitch;
			if (volumeSet) parameters.Volume = volume;
			if (mixerSet) parameters.MixerType = mixerType;
			if (maxDistanceSet) parameters.MaxDistance = maxDistance;
			if (minDistanceSet) parameters.MinDistance = minDistance;
			if (loops) parameters.Loops = true;
			if (isMute) parameters.IsMute = true;

			return parameters;
		}

		private float FadeTargetVolume => volumeSet ? volume : 1f;

		/// <summary>
		/// Play the sound for all clients. Fire-and-forget (no token returned).
		/// Use PlayNetworkedAsync() if you need the token to stop the sound later.
		/// </summary>
		public void PlayNetworked()
		{
			if (fadeInDuration > 0)
			{
				PlayNetworkedAsync().Forget();
				return;
			}

			var parameters = BuildParameters();
			SoundManager.PlayNetworkedAtPos(sound, worldPos, parameters, polyphonic,
				global: isGlobal, sourceObj: sourceObj);
		}

		/// <summary>
		/// Play the sound for all clients and return a token for later stopping.
		/// If WithFadeIn() was called, a fade message is sent to all clients automatically.
		/// </summary>
		public async UniTask<string> PlayNetworkedAsync()
		{
			var parameters = BuildParameters();
			var token = await SoundManager.PlayNetworkedAtPosAsync(sound, worldPos, parameters, polyphonic,
				global: isGlobal, sourceObj: sourceObj);

			if (fadeInDuration > 0)
			{
				FadeSoundMessage.SendToAll(token, FadeTargetVolume, fadeInDuration);
			}

			return token;
		}

		/// <summary>
		/// Play the sound for a specific player only.
		/// Note: WithFadeIn() is not supported here because the underlying API does not return a token.
		/// For targeted fade-in, use PlayNetworkedAsync() + Sound.Fade(token, ...).SendTo(recipient).
		/// </summary>
		public void PlayNetworkedFor(GameObject recipient)
		{
			var parameters = BuildParameters();
			_ = SoundManager.PlayNetworkedForPlayerAtPos(recipient, worldPos, sound, parameters,
				polyphonic, sourceObj: sourceObj);
		}

		/// <summary>
		/// Play the sound locally (client-side only).
		/// If WithFadeIn() was called, the fade runs locally without any network message.
		/// </summary>
		public async UniTaskVoid PlayLocal()
		{
			var parameters = BuildParameters();
			string token = fadeInDuration > 0 ? System.Guid.NewGuid().ToString() : "";

			await SoundManager.PlayAtPosition(sound, worldPos, sourceObj, soundSpawnToken: token,
				audioSourceParameters: parameters, polyphonic: polyphonic, isGlobal: isGlobal);

			if (fadeInDuration > 0)
			{
				SoundManager.ClientFade(token, FadeTargetVolume, fadeInDuration, false);
			}
		}
	}
}
