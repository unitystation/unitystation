using Audio.Containers;
using Audio.Managers;
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
	private AudioClip currentTrack = null;

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

		if (isEntering)
		{
			PlayAudio(enteringSoundTrack.GetRandomClip());
		}
		else
		{
			PlayAudio(leavingSoundTrack.GetRandomClip());
		}
	}

	private void PlayAudio(AudioClip clipToPlay)
	{
		if (clipToPlay == null) return;

		SoundAmbientManager.StopAudio(currentTrack);
		currentTrack = clipToPlay;
		SoundAmbientManager.PlayAudio(currentTrack, isLooped: true);
	}
}
