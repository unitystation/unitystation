using System.Collections.Generic;
using UnityEngine;
using System.Text;

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
		private JobType antagJobType;

		public JobType AntagJobType => antagJobType;

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

		/// <summary>
		/// Server only. Spawn the joined viewer as the indicated antag, includes creating their player object
		/// and transferring them to it.
		/// </summary>
		/// <param name="spawnRequest">player's requested spawn</param>
		/// <returns>gameobject of the spawned antag that he player is now in control of</returns>
		public abstract GameObject ServerSpawn(PlayerSpawnRequest spawnRequest);

	}
}