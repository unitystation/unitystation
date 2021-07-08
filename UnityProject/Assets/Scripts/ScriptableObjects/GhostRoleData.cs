using System.Collections;
using UnityEngine;
using NaughtyAttributes;
using Antagonists;

namespace ScriptableObjects
{
	/// <summary>
	/// Contains all the data necessary for both the server and client to create, receive notifications for and request a ghost role.
	/// </summary>
	[CreateAssetMenu(fileName = "MyGhostRole", menuName = "ScriptableObjects/Systems/GhostRoles/GhostRole")]
	public class GhostRoleData : ScriptableObject
	{
		[SerializeField] private new string name = default;
		[SerializeField] private string description = default;
		[SerializeField] private SpriteDataSO sprite = default;

		[Tooltip("If custom, then whatever creates the ghost role must decide the respawning logic.")]
		[SerializeField] private GhostRoleSpawnType respawnType = default;
		[SerializeField, ShowIf(nameof(IsOccupation))] private Occupation targetOccupation = default;
		[SerializeField, ShowIf(nameof(IsAntagonist))] private Antagonist targetAntagonist = default;

		[SerializeField, Range(1, 16)] private int minPlayers = 1;
		[SerializeField, Range(1, 16)] private int maxPlayers = 1;
		[InfoBox("Set to -1 for no timeout.", EInfoBoxType.Normal)]
		[SerializeField, Range(-1, 120)] private float timeout = 30;

		[Tooltip("What player counts the player should see on this ghost role entry - min/max possible players, current players.")]
		[SerializeField] private GhostRolePlayerCountType playerCountType = GhostRolePlayerCountType.ShowCurrentAndMaxCounts;

		public string Name => name;
		public string Description => description;
		public SpriteDataSO Sprite => sprite;

		public GhostRoleSpawnType RespawnType => respawnType;
		public bool IsOccupation => respawnType == GhostRoleSpawnType.SpawnOccupation;
		public bool IsAntagonist => respawnType == GhostRoleSpawnType.SpawnAntagonist;
		public Occupation TargetOccupation => targetOccupation;
		public Antagonist TargetAntagonist => targetAntagonist;

		public int MinPlayers => minPlayers;
		public int MaxPlayers => maxPlayers;
		public float Timeout => timeout;

		public GhostRolePlayerCountType PlayerCountType => playerCountType;

		public override string ToString()
		{
			return name;
		}
	}

	public enum GhostRoleSpawnType
	{
		SpawnOccupation,
		SpawnAntagonist,
		Custom,
	}

	public enum GhostRolePlayerCountType
	{
		ShowNoCounts,
		ShowMaxCount,
		ShowCurrentAndMaxCounts,
		ShowMinCurrentAndMaxCounts,
	}
}
