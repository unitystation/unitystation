using System;
using System.Collections.Generic;
using UnityEngine;

namespace US13.Managers.Music
{
	/// <summary>
	/// A named trigger that can fire one or more music tracks with an optional random chance.
	/// Triggers are authored as assets and organised in the repository so server hosts can
	/// add their own (e.g. a 1% chance when a microwave turns on).
	/// </summary>
	[CreateAssetMenu(fileName = "MusicTrigger", menuName = "ScriptableObjects/Audio/MusicTrigger", order = 2)]
	public class MusicTriggerSO : ScriptableObject
	{
		[Header("Trigger")]
		[Tooltip("Stable identifier used by gameplay code to request this trigger (e.g. \"round_start\", \"spawn_chef\").")]
		public string TriggerId = "";

		[Header("Chance")]
		[Tooltip("Percent chance (0-100) that the trigger actually plays music when fired. " +
		         "100 means always play; 1 means a 1-in-100 chance.")]
		[Range(0f, 100f)]
		public float ChancePercent = 100f;

		[Header("Cooldown")]
		[Tooltip("Minimum seconds before this trigger can fire again. Prevents spam.")]
		[Range(0f, 600f)]
		public float CooldownSeconds = 10f;

		[Header("Tracks")]
		[Tooltip("Candidate tracks for this trigger. One is chosen at random when the trigger fires.")]
		public List<MusicTrackSO> Tracks = new List<MusicTrackSO>();

		[NonSerialized] private float lastFiredTime = -99999f;

		/// <summary>
		/// True if the cooldown has elapsed since the last fire.
		/// </summary>
		public bool CooldownElapsed => Time.time - lastFiredTime >= CooldownSeconds;

		/// <summary>
		/// Rolls the chance and, on success, marks the trigger as fired and returns a random track.
		/// Returns null if the roll fails, the cooldown is active, or there are no tracks.
		/// </summary>
		public MusicTrackSO TryFire()
		{
			if (Tracks == null || Tracks.Count == 0) return null;
			if (!CooldownElapsed) return null;

			var roll = UnityEngine.Random.Range(0f, 100f);
			if (roll > ChancePercent) return null;

			lastFiredTime = Time.time;
			var index = UnityEngine.Random.Range(0, Tracks.Count);
			return Tracks[index];
		}
	}
}
