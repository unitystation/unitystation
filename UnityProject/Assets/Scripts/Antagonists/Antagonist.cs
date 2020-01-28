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
		/// <summary>
		/// The name of the antagonist type
		/// </summary>
		[SerializeField]
		protected string antagName = "New Antag";
		/// <summary>
		/// The name of the antagonist type
		/// </summary>
		public string AntagName => antagName;

		/// <summary>
		/// How many objectives this antag needs
		/// </summary>
		public int NumberOfObjectives = 2;

		[SerializeField]
		protected bool canUseSharedObjectives;
		/// <summary>
		/// Can this antag get objectives from the shared objective pool?
		/// </summary>
		public bool CanUseSharedObjectives => canUseSharedObjectives;

		[SerializeField]
		protected bool needsEscapeObjective;
		/// <summary>
		/// Does this antag need some kind of escape objective?
		/// </summary>
		public bool NeedsEscapeObjective => needsEscapeObjective;

		[SerializeField]
		protected List<Objective> coreObjectives = new List<Objective>();
		/// <summary>
		/// The core objectives only this type of antagonist can get
		/// </summary>
		public IEnumerable<Objective> CoreObjectives => coreObjectives;

		/// <summary>
		/// Server only. Spawn the joined viewer as the indicated antag, includes creating their player object
		/// and transferring them to it.
		/// </summary>
		/// <param name="spawnRequest">player's requested spawn</param>
		/// <returns>gameobject of the spawned antag that he player is now in control of</returns>
		public abstract GameObject ServerSpawn(PlayerSpawnRequest spawnRequest);

	}
}