using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A sound spawn is an instance of a currently playing sound.
/// It might have parameters different than the original "sound template" AudioSource.
/// </summary>
public class SoundSpawn: MonoBehaviour
{
	public AudioSource AudioSource = null;
	public RegisterTile RegisterTile = null;

	//We need to handle this manually to prevent multiple requests grabbing sound pool items in the same frame
	public bool IsPlaying = false;
	private bool monitor = false;

	public string assetAddress = "";

	/// <summary>
	/// The Unique Token of the SoundSpawn.  Keep it to retrieve the playing sound.
	/// </summary>
	/// <remarks>
	/// This token is needed to stop or change the AudioSourceParameters of a sound currently playing
	/// </remarks>
	public string Token;

	private void Awake()
	{
		AudioSource = GetComponent<AudioSource>();
		RegisterTile = GetComponent<RegisterTile>();
	}

	public void PlayOneShot()
	{
		if (AudioSource == null) return;
		AudioSource.PlayOneShot(AudioSource.clip);
		WaitForPlayToFinish();
	}


	[NaughtyAttributes.Button("PlayNormally")]
	public void PlayNormally()
	{
		if (AudioSource == null) return;
		AudioSource.Play();
		WaitForPlayToFinish();
	}

	void WaitForPlayToFinish()
	{
		monitor = true;
	}

	private void OnEnable()
	{
		UpdateManager.Add(UpdateMe, 0.2f);
	}

	private void OnDisable()
	{
		if (SoundManager.Instance == null) return;
		SoundManager.Instance.SoundSpawns.Remove(Token);
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
	}

	void UpdateMe()
	{
		if (!monitor || AudioSource == null) return;

		if (!AudioSource.isPlaying)
		{
			IsPlaying = false;
			monitor = false;

			if (Token != string.Empty)
			{
				SoundManager.Instance.SoundSpawns.Remove(Token);
			}

			if (SoundManager.Instance.NonplayingSounds.ContainsKey(assetAddress) == false)
			{
				SoundManager.Instance.NonplayingSounds[assetAddress] = new List<SoundSpawn>();
			}
			SoundManager.Instance.NonplayingSounds[assetAddress].Add(this);
		}
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