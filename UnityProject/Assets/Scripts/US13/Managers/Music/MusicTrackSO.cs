using System;
using UnityEngine;
using US13.Core.Addressables.Types;

namespace US13.Managers.Music
{
	/// <summary>
	/// Defines a single music track that the MusicManager can play.
	/// Server hosts can author these as assets and register them on a MusicLibrarySO
	/// to configure per-server music without code changes.
	/// </summary>
	[CreateAssetMenu(fileName = "MusicTrack", menuName = "ScriptableObjects/Audio/MusicTrack", order = 1)]
	public class MusicTrackSO : ScriptableObject
	{
		[Header("Audio")]
		[Tooltip("The addressable audio clip for this track.")]
		public AddressableAudioSource Clip;

		[Header("Playback")]
		[Tooltip("How this track is played: Local (this client), Global (all clients), or Diagetic (world position).")]
		public MusicPlaybackMode PlaybackMode = MusicPlaybackMode.Local;

		[Tooltip("Higher-score tracks override lower-score tracks of the same or lower precedence. " +
		         "Global tracks should generally score higher than Local tracks.")]
		[Range(0, 100)]
		public int Score = 50;

		[Header("Fading (seconds)")]
		[Tooltip("Fade-in duration when this track starts.")]
		[Range(0f, 10f)]
		public float FadeInDuration = 1f;

		[Tooltip("Fade-out duration when this track is stopped or overridden.")]
		[Range(0f, 10f)]
		public float FadeOutDuration = 1f;

		[Header("Metadata")]
		[Tooltip("Display name shown in UI. Falls back to the clip name if empty.")]
		public string DisplayName = "";

		[Tooltip("Author/artist shown in UI. Falls back to the clip name suffix.")]
		public string Artist = "";

		/// <summary>
		/// Human-readable track name, preferring the authored DisplayName over the clip name.
		/// </summary>
		public string TrackName
		{
			get
			{
				if (!string.IsNullOrEmpty(DisplayName)) return DisplayName;
				if (Clip != null && Clip.AudioSource != null && Clip.AudioSource.clip != null)
				{
					var clipName = Clip.AudioSource.clip.name;
					return clipName.Contains("_") ? clipName.Split('_')[0] : clipName;
				}
				return name;
			}
		}

		/// <summary>
		/// Human-readable artist, preferring the authored Artist over the clip-name suffix.
		/// </summary>
		public string TrackArtist
		{
			get
			{
				if (!string.IsNullOrEmpty(Artist)) return Artist;
				if (Clip != null && Clip.AudioSource != null && Clip.AudioSource.clip != null)
				{
					var clipName = Clip.AudioSource.clip.name;
					return clipName.Contains("_") ? clipName.Split('_')[1] : "Unknown";
				}
				return "Unknown";
			}
		}
	}
}
