using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using Audio.Containers;
using Audio.Managers;
using Messages.Server.SoundMessages;
using UnityEngine;

/// <summary>
/// This will trigger ambience for players entering or leaving
/// its trigger area. Drag and drop the prefab to start setting up
/// an ambient controlled zone
/// </summary>
public class AmbientSoundArea : MonoBehaviour
{

	public static event Action RefreshAmbientSoundAreas;

	[SerializeField] private AudioClipsArray enteringSoundTrack = null;
	[SerializeField] private AudioClipsArray leavingSoundTrack = null;

	private AddressableAudioSource playing;

	public static void TriggerRefresh()
	{
		RefreshAmbientSoundAreas?.Invoke();
	}

	public void OnTriggerEnter2D(Collider2D coll)
	{
		ValidatePlayer(coll.gameObject, true);
	}

	public void OnTriggerExit2D(Collider2D coll)
	{
		ValidatePlayer(coll.gameObject, false);
	}


	public void OnEnable()
	{
		RefreshAmbientSoundAreas += Refresh;
	}
	public void OnDisable()
	{
		RefreshAmbientSoundAreas -= Refresh;
	}


	public void Refresh()
	{
		var Colliders = this.GetComponents<Collider2D>();
		foreach (var Collider in Colliders)
		{
			Collider.enabled = false;
		}

		foreach (var Collider in Colliders)
		{
			Collider.enabled = true;
		}
	}


	private void ValidatePlayer(GameObject player, bool isEntering)
	{
		if (player == null) return;
		if (player != PlayerManager.LocalPlayerObject) return;

		// Dont change sound when sent to hidden pos, e.g in locker
		// TODO entering sound still plays when exiting locker, but this at least stops space sound
		if (player.TryGetComponent<MovementSynchronisation>(out var playerSync))
		{
			if (playerSync.registerTile.LocalPosition == TransformState.HiddenPos)
			{
				return;
			}
		}

		if (isEntering)
		{
			PlayAudio(enteringSoundTrack.AddressableAudioSource.GetRandom());
		}
		else
		{
			PlayAudio(leavingSoundTrack.AddressableAudioSource.GetRandom());
		}
	}

	private void PlayAudio(AddressableAudioSource clipToPlay)
	{
		if (clipToPlay == null) return;

		SoundAmbientManager.StopAudio(playing);
		playing = clipToPlay;
		SoundAmbientManager.PlayAudio(clipToPlay);
	}
}
