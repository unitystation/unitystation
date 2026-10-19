using System.Collections.Generic;
using UnityEngine;
using US13.Core.Addressables.Types;

namespace US13.Managers.Music
{
	/// <summary>
	/// A server-host configurable collection of music grouped by category.
	/// Server operators can author one of these assets (or several, swapped at runtime)
	/// to control lobby, admin, and jukebox music libraries without code changes.
	/// </summary>
	[CreateAssetMenu(fileName = "MusicLibrary", menuName = "ScriptableObjects/Audio/MusicLibrary", order = 3)]
	public class MusicLibrarySO : ScriptableObject
	{
		[Header("Lobby / Ambient Playlist")]
		[Tooltip("Tracks available to the random lobby / ambient playlist.")]
		public List<AddressableAudioSource> LobbyTracks = new List<AddressableAudioSource>();

		[Header("Admin / Global Library")]
		[Tooltip("Tracks admins can trigger globally for all players.")]
		public List<MusicTrackSO> AdminTracks = new List<MusicTrackSO>();

		[Header("Default Triggers")]
		[Tooltip("Optional registered triggers. Gameplay code can also fire triggers by TriggerId at runtime.")]
		public List<MusicTriggerSO> Triggers = new List<MusicTriggerSO>();

		/// <summary>
		/// Finds a registered trigger by its id, or null if not present.
		/// </summary>
		public MusicTriggerSO GetTrigger(string triggerId)
		{
			if (string.IsNullOrEmpty(triggerId) || Triggers == null) return null;
			foreach (var trigger in Triggers)
			{
				if (trigger != null && trigger.TriggerId == triggerId) return trigger;
			}
			return null;
		}
	}
}
