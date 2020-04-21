using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Antagonists
{
	/// <summary>
	/// Stores all antagonists and objectives. Use the public methods to get since they must be instantiated
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagData")]
	public class AntagData : ScriptableObject
	{
		/// <summary>
		/// All possible antags.
		/// </summary>
		[SerializeField]
		private List<Antagonist> Antags = new List<Antagonist>();
		/// <summary>
		/// Possible objectives which any antag can get.
		/// </summary>
		[SerializeField]
		private List<Objective> SharedObjectives = new List<Objective>();
		/// <summary>
		/// Possible escape objectives. Antags should only get one of these.
		/// </summary>
		[SerializeField]
		private List<Objective> EscapeObjectives = new List<Objective>();

		/// <summary>
		/// Returns a new instance of a random antag type.
		/// </summary>
		public Antagonist GetRandomAntag()
		{
			var antag = Antags[Random.Range(0, Antags.Count)];
			if (antag == null)
			{
				Logger.LogError("No antags available in AntagData! Ensure you added the ScriptableObjects to it.", Category.Antags);
			}
			return Instantiate(antag);
		}

		/// <summary>
		/// Returns an enumerator of all antag names as strings
		/// </summary>
		/// <returns></returns>
		public IReadOnlyCollection<Antagonist> GetAllAntags()
		{
			return Antags;
		}

		/// <summary>
		/// Generates a set of valid objectives for a player based on their antag type.
		/// Will set them up and ensure all targets are unique.
		/// Amount to generate does not include the escape objective.
		/// </summary>
		/// <param name="player">The player receiving these objectives</param>
		/// <param name="antag">The antag type</param>
		/// <param name="amount">How many objectives to generate, not including escape objectives</param>
		public List<Objective> GenerateObjectives(PlayerScript player, Antagonist antag)
		{
			int amount = antag.NumberOfObjectives;
			// Get all antag core and shared objectives which are possible for this player
			List<Objective> objPool = antag.CoreObjectives.Where(obj => obj.IsPossible(player)).ToList();
			if (antag.CanUseSharedObjectives)
			{
				objPool = objPool.Concat(SharedObjectives).Where(obj => obj.IsPossible(player)).ToList();
			}

			if (objPool.Count == 0)
			{
				amount = 0;
				Logger.LogWarning($"No objectives available, only assigning escape type objective", Category.Antags);
			}

			List<Objective> generatedObjs = new List<Objective>();
			Objective newObjective;
			for (int i = 0; i < amount; i++)
			{
				// Select objective and perform setup e.g. assign owner and targets
				newObjective = PickRandomObjective(ref objPool);
				newObjective.DoSetup(player.mind);
				generatedObjs.Add(newObjective);

				// Trim any objectives which aren't possible
				// Should be done everytime an objective is assigned and setup,
				// otherwise all targets could be taken already!
				objPool = objPool.Where(obj => obj.IsPossible(player)).ToList();

				if (objPool.Count == 0)
				{
					// No objectives left to give so stop assigning
					break;
				}
			}

			if (antag.NeedsEscapeObjective)
			{
				// Add one escape type objective if needed
				// Be careful not to remove all escape objectives from AntagData
				newObjective = PickRandomObjective(ref EscapeObjectives, false);
				newObjective.DoSetup(player.mind);
				generatedObjs.Add(newObjective);
			}

			return generatedObjs;
		}

		/// <summary>
		/// Instantiates a random objective from a list and returns it
		/// </summary>
		/// <param name="objectives">The objectives to pick from</param>
		/// <param name="checkUnique">If true, checks if objective is unique and removes it from the pool </param>
		/// <returns>The random objective</returns>
		public static Objective PickRandomObjective(ref List<Objective> objectives, bool checkUnique = true)
		{
			// Must use Instantiate or else the objectives in AntagData will be referenced for each player!
			int randIndex = Random.Range(0, objectives.Count);
			Objective chosenObjective = Instantiate(objectives[randIndex]);
			if (checkUnique && chosenObjective.IsUnique)
			{
				objectives.RemoveAt(randIndex);
			}
			return chosenObjective;
		}
	}
}