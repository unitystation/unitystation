using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using AddressableReferences;
using Player;

namespace Antagonists
{
	/// <summary>
	/// Defines an antagonist.
	/// </summary>
	public abstract class Antagonist : ScriptableObject
	{
		[Tooltip("The name of the antagonist type")]
		[SerializeField]
		private string antagName = "New Antag";

		[Tooltip("The antag jobType")]
		[SerializeField]
		private JobType antagJobType = default;

		public JobType AntagJobType => antagJobType;

		[BoxGroup("Spawn Banner")]
		[Tooltip("Should this antag play a sound cue on spawn?")]
		[SerializeField]
		private bool playSound = false;
		public bool PlaySound => playSound;

		[BoxGroup("Spawn Banner")]
		[ShowIf(nameof(playSound))]
		[Tooltip("The sound a player hears when they spawn as this antag.")]
		[SerializeField]
		private AddressableAudioSource spawnSound = null;

		public AddressableAudioSource SpawnSound => spawnSound;

		[BoxGroup("Spawn Banner")]
		[Tooltip("What color should the text in the spawn banner be for this antag.")]
		[SerializeField]
		private Color textColor = Color.red;
		public Color TextColor => textColor;

		[BoxGroup("Spawn Banner")]
		[Tooltip("The color for the background of the banner")]
		[SerializeField]
		private Color backgroundColor = Color.red;
		public Color BackgroundColor => backgroundColor;

		/// <summary>
		/// The name of the antagonist type
		/// </summary>
		public string AntagName => antagName;

		[Tooltip("How many objectives this antag needs")]
		[SerializeField]
		private int numberOfObjectives = 2;
		/// <summary>
		/// How many objectives this antag needs
		/// </summary>
		public int NumberOfObjectives => numberOfObjectives;

		[Tooltip("Can this antag get objectives from the shared objective pool?")]
		[SerializeField]
		private bool canUseSharedObjectives = false;
		/// <summary>
		/// Can this antag get objectives from the shared objective pool?
		/// </summary>
		public bool CanUseSharedObjectives => canUseSharedObjectives;

		[Tooltip("Does this antag need some kind of escape objective?")]
		[SerializeField]
		protected bool needsEscapeObjective;
		/// <summary>
		/// Does this antag need some kind of escape objective?
		/// </summary>
		public bool NeedsEscapeObjective => needsEscapeObjective;

		[Tooltip("The chance to add one gimmick objective in addition to the other objectives")]
		[SerializeField]
		[Range(0,100)]
		protected float chanceForGimmickObjective = 0;
		/// <summary>
		/// The chance to add one gimmick objective in addition to the other objectives
		/// </summary>
		public float ChanceForGimmickObjective => chanceForGimmickObjective;

		[Tooltip("The core objectives only this type of antagonist can get")]
		[SerializeField]
		protected List<Objective> coreObjectives = new List<Objective>();
		/// <summary>
		/// The core objectives only this type of antagonist can get
		/// </summary>
		public IEnumerable<Objective> CoreObjectives => coreObjectives;

		[Tooltip("Occupation that antags should spawn as, can be left null if they are allocated one.")]
		[SerializeField]
		private Occupation antagOccupation = null;
		/// <summary>
		/// Occupation that antags should spawn as, can be left null if they are allocated one.
		/// </summary>
		public Occupation AntagOccupation => antagOccupation;

		[Tooltip("Should this antag show up as an option in the antag preferences?")]
		[SerializeField]
		private bool showInPreferences = true;
		/// <summary>
		/// Should this antag show up as an option in the antag preferences?
		/// </summary>
		public bool ShowInPreferences => showInPreferences;

		[Tooltip("Randomizes this antag's character when it is a ghost role")]
		[SerializeField]
		private bool randomizeCharacterForGhostRole = false;
		/// <summary>
		/// Randomizes this antags character when it is a ghost role
		/// </summary>
		public bool RandomizeCharacterForGhostRole => randomizeCharacterForGhostRole;

		[Tooltip("Changes the occupation of a player if they get this antag as a ghost role, only matters if they don't already have a job type")]
		[SerializeField]
		private Occupation ghostRoleOccupation = null;
		/// <summary>
		/// changes the occupation for a player if they get this antag as a ghost role, only matters if they don't already have an Antag Occupation
		/// </summary>
		public Occupation GhostRoleOccupation => ghostRoleOccupation;

		[field: SerializeField] public List<Objective> BlackListedObjectives { get; set; } = new List<Objective>();
		[field: SerializeField] public List<Objective> AlwaysStartWithObjectives { get; set; } = new List<Objective>();

		/// <summary>
		/// Server only. Spawn the joined viewer as the indicated antag, includes creating their player object
		/// and transferring them to it.
		/// </summary>
		/// <param name="spawnRequest">player's requested spawn</param>
		/// <returns>gameobject of the spawned antag that he player is now in control of</returns>
		public virtual Mind ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally but override the player-requested occupation with the antagonist occupation
			return PlayerSpawn.NewSpawnPlayerV2(spawnRequest.Player, AntagOccupation, spawnRequest.CharacterSettings);
		}

		/// <summary>
		/// Called just after spawning or respawning.
		/// </summary>
		public abstract void AfterSpawn( Mind SpawnMind);

		public void AddObjective(Objective objective)
		{
			if ( BlackListedObjectives.Contains(objective) ) return;
			numberOfObjectives++;
			coreObjectives.Add(objective);
		}

		public void RemoveObjective(Objective objective)
		{
			coreObjectives.Remove(objective);
			numberOfObjectives--;
		}
	}
}
