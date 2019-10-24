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
		/// Gets a number of random objectives
		/// </summary>
		/// <param name="amount">How many objectives to generate</param>
		/// <param name="unique">Should these objectives be unique</param>
		/// <param name="antag">The antag type</param>
		public List<Objective> GetRandomObjectives(int amount = 1, bool unique = false, Antagonist antag = null)
		{
			List<Objective> objPool = SharedObjectives.Concat(antag.PossibleObjectives).ToList();
			List<Objective> chosenObjs = new List<Objective>();
			if (unique && objPool.Count < amount)
			{
				amount = objPool.Count;
				Logger.LogWarning($"Not enough unique objectives available, only getting {amount}", Category.Antags);
			}
			for (int i = 0; i < amount; i++)
			{
				int randIndex = Random.Range(0, objPool.Count);
				chosenObjs.Add(Instantiate(objPool[randIndex]));
				if (unique)
				{
					objPool.RemoveAt(randIndex);
				}
			}
			return chosenObjs;
		}

		/// <summary>
		/// Returns an escape objective
		/// </summary>
		public Objective GetEscapeObjective()
		{
			int randIndex = Random.Range(0, EscapeObjectives.Count);
			return Instantiate(EscapeObjectives[randIndex]);
		}
	}
}