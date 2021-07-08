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
	[SerializeField] private AudioClipsArray enteringSoundTrack = null;
	[SerializeField] private AudioClipsArray leavingSoundTrack = null;
	private string guid = "";

	private AddressableAudioSource playing;

	public void OnTriggerEnter2D(Collider2D coll)
	{
		ValidatePlayer(coll.gameObject, true);
	}

	public void OnTriggerExit2D(Collider2D coll)
	{
		ValidatePlayer(coll.gameObject, false);
	}

	private void ValidatePlayer(GameObject player, bool isEntering)
	{
		if (player == null) return;
		if (player != PlayerManager.LocalPlayer) return;

		// Dont change sound when sent to hidden pos, e.g in locker
		// TODO entering sound still plays when exiting locker, but this at least stops space sound
		if (player.TryGetComponent<PlayerSync>(out var playerSync))
		{
			if (playerSync.TrustedPosition == TransformState.HiddenPos)
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
