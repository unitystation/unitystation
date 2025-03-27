using System.Collections.Generic;
using Initialisation;
using UnityEngine;

namespace Core.Sound
{
	/// <summary>
	/// A sound spawn is an instance of a currently playing sound.
	/// It might have parameters different than the original "sound template" AudioSource.
	/// </summary>
	public class SoundSpawn : MonoBehaviour
	{
		public AudioSource AudioSource = null;
		public RegisterTile RegisterTile = null;

		//We need to handle this manually to prevent multiple requests grabbing sound pool items in the same frame
		public bool IsPlaying = false;
		private bool monitor = false;

		public bool Paused = false;

		public string assetAddress = "";

		/// <summary>
		/// The Unique Token of the SoundSpawn.  Keep it to retrieve the playing sound.
		/// </summary>
		/// <remarks>
		/// This token is needed to stop or change the AudioSourceParameters of a sound currently playing
		/// </remarks>
		public string Token;

		private float pauseLocation = 0;

		private void Awake()
		{
			AudioSource = GetComponent<AudioSource>();
			RegisterTile = GetComponent<RegisterTile>();
		}

		public void PlayOneShot()
		{
			LoadManager.DoInMainThread(PlayOneShotMainThread);
		}

		public void PlayOneShotMainThread()
		{
			if (AudioSource == null) return;
			gameObject.SetActive(true);
			AudioSource.PlayOneShot(AudioSource.clip);
			WaitForPlayToFinish();
		}

		[NaughtyAttributes.Button("PlayNormally")]
		public void PlayNormally()
		{
			LoadManager.DoInMainThread(PlayNormallyMainThread);
		}

		public void PlayNormallyMainThread()
		{
			if (AudioSource == null) return;
			gameObject.SetActive(true);
			AudioSource.Play();
			WaitForPlayToFinish();
		}

		void WaitForPlayToFinish()
		{
			monitor = true;
		}

		private void OnEnable()
		{
			UpdateManager.Add(UpdateMe, 0.2f, CallbackType:CallbackType.SOUND_UPDATE);
		}

		private void OnDisable()
		{
			if (SoundManager.Instance == null) return;
			if (Token != string.Empty)
			{
				SoundManager.Instance.SoundSpawns.Remove(Token);
			}
			UpdateManager.Remove(CallbackType.SOUND_UPDATE, UpdateMe);
		}

		void UpdateMe()
		{
			if (Paused) return;
			if (monitor == false || AudioSource == null) return;

			if (CustomNetworkManager.IsHeadless == false && AudioSource.loop)
			{
				if (ClientTooFarFromLoopingSoundSource()) return; // return so we don't pool the sound prematurally for looping sounds
			}

			if (AudioSource.isPlaying == false)
			{
				Pool();
			}
		}

		private bool ClientTooFarFromLoopingSoundSource()
		{
			if (PlayerManager.LocalPlayerScript == null || RegisterTile == null) return false; // player hasn't spawned yet or world hasn't fully loaded.
			if (PlayerManager.LocalPlayerScript.playerMove == null) return false; // player hasn't fully initialized yet.
			if (Vector3.Distance(transform.position,
				    PlayerManager.LocalPlayerScript.playerMove.OfficialPosition) > AudioSource.maxDistance)
			{
				if (AudioSource.isPlaying)
				{
					pauseLocation = AudioSource.time;
					AudioSource.Stop();
				}
				return true;
			}
			else
			{
				if (AudioSource.isPlaying == false)
				{
					AudioSource.Play();
					AudioSource.time = pauseLocation;
				}
			}
			return false;
		}

		public void Pool()
		{
			IsPlaying = false;
			monitor = false;
			AudioSource.Stop();
			if (Token != string.Empty)
			{
				SoundManager.Instance.SoundSpawns.Remove(Token);
			}

			if (SoundManager.Instance.NonplayingSounds.ContainsKey(assetAddress) == false)
			{
				SoundManager.Instance.NonplayingSounds[assetAddress] = new List<SoundSpawn>();
			}
			SoundManager.Instance.NonplayingSounds[assetAddress].Add(this);
			gameObject.SetActive(false);
		}

		public void SetAudioSource(AudioSource sourceToCopy)
		{
			AudioSource.clip = sourceToCopy.clip;
			AudioSource.loop = sourceToCopy.loop;
			AudioSource.pitch = sourceToCopy.pitch;
			AudioSource.mute = sourceToCopy.mute;
			AudioSource.spatialize = sourceToCopy.spatialize;
			AudioSource.spread = sourceToCopy.spread;
			AudioSource.volume = sourceToCopy.volume;
			AudioSource.bypassEffects = sourceToCopy.bypassEffects;
			AudioSource.dopplerLevel = sourceToCopy.dopplerLevel;
			AudioSource.maxDistance = sourceToCopy.maxDistance;
			AudioSource.minDistance = sourceToCopy.minDistance;
			AudioSource.panStereo = sourceToCopy.panStereo;
			AudioSource.rolloffMode = sourceToCopy.rolloffMode;
			AudioSource.spatialBlend = sourceToCopy.spatialBlend;
			AudioSource.bypassListenerEffects = sourceToCopy.bypassListenerEffects;
			AudioSource.bypassReverbZones = sourceToCopy.bypassReverbZones;
			AudioSource.reverbZoneMix = sourceToCopy.reverbZoneMix;
			AudioSource.spatializePostEffects = sourceToCopy.spatializePostEffects;
			AudioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, sourceToCopy.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
			AudioSource.SetCustomCurve(AudioSourceCurveType.Spread, sourceToCopy.GetCustomCurve(AudioSourceCurveType.Spread));
			AudioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, sourceToCopy.GetCustomCurve(AudioSourceCurveType.SpatialBlend));
			AudioSource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, sourceToCopy.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix));
		}
	}
}