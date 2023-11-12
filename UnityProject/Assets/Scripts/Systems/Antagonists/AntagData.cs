using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ScriptableObjects;
using Logs;

namespace Antagonists
{
	/// <summary>
	/// Stores all antagonists and objectives. Use the public methods to get since they must be instantiated
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagData")]
	public class AntagData : SingletonScriptableObject<AntagData>
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
		public List<Objective> SharedObjectivesPublic => new List<Objective>(SharedObjectives);

		/// <summary>
		/// All possible teams.
		/// </summary>
		[SerializeField]
		private List<TeamData> teamDatas = new List<TeamData>();
		public List<TeamData> TeamDatas => teamDatas;
		/// <summary>
		/// Possible escape objectives. Antags should only get one of these.
		/// </summary>
		[SerializeField]
		private List<Objective> EscapeObjectives = new List<Objective>();

		/// <summary>
		/// Gimmick objectives, these objectives will always succeed.
		/// </summary>
		[SerializeField]
		private List<Objective> GimmickObjectives = new List<Objective>();

		[SerializeField]
		private List<TeamObjective> TeamObjectives = new List<TeamObjective>();

		/// <summary>
		/// Putting all objectives to one list
		/// </summary>
		private readonly List<Objective> allObjectives = new List<Objective>();

		/// <summary>
		/// All antags, teams, station objectives in one list
		/// </summary>
		public List<Objective> AllObjectives
		{
			get
			{
				if (allObjectives.Count == 0)
				{
					foreach (var x in Antags)
					{
						allObjectives.AddRange(x.CoreObjectives);
					}
					allObjectives.AddRange(SharedObjectives);
					allObjectives.AddRange(EscapeObjectives);
					allObjectives.AddRange(GimmickObjectives);
					allObjectives.AddRange(TeamObjectives);
					allObjectives.AddRange(StationObjectives.StationObjectiveData.Instance.ObjectivesPublic);
				}

				return new (allObjectives);
			}
		}

		/// <summary>
		/// Returns a new instance of a random antag type.
		/// </summary>
		public Antagonist GetRandomAntag()
		{
			var antag = Antags[Random.Range(0, Antags.Count)];
			if (antag == null)
			{
				Loggy.LogError("No antags available in AntagData! Ensure you added the ScriptableObjects to it.", Category.Antags);
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
		public List<Objective> GenerateObjectives(Mind Mind, Antagonist antag)
		{
			int amount = antag.NumberOfObjectives;
			// Get all antag core and shared objectives which are possible for this player
			List<Objective> objPool = antag.CoreObjectives.Where(obj => obj.IsPossible(Mind) && antag.BlackListedObjectives.Contains(obj) == false).ToList();
			if (antag.CanUseSharedObjectives)
			{
				objPool = objPool.Concat(SharedObjectives).Where(obj => obj.IsPossible(Mind) && antag.BlackListedObjectives.Contains(obj) == false).ToList();
			}

			if (objPool.Count == 0)
			{
				amount = 0;
				Loggy.LogWarning($"No objectives available, only assigning escape type objective", Category.Antags);
			}

			List<Objective> generatedObjs = new List<Objective>();
			Objective newObjective;
			for (int i = 0; i < amount; i++)
			{
				// Select objective and perform setup e.g. assign owner and targets
				newObjective = PickRandomObjective(ref objPool);
				newObjective.DoSetup(Mind);
				generatedObjs.Add(newObjective);

				// Trim any objectives which aren't possible
				// Should be done everytime an objective is assigned and setup,
				// otherwise all targets could be taken already!
				objPool = objPool.Where(obj => obj.IsPossible(Mind)).ToList();

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
				var allowedEscapes = EscapeObjectives.Where(obj => obj.IsPossible(Mind)).ToList();
				//TODO since checkUnique is false we dont need to remove the chosen object from EscapeObjectives
				//TODO but we would if we ever want to allow for unique escape objectives
				newObjective = PickRandomObjective(ref allowedEscapes, false);
				newObjective.DoSetup(Mind);
				generatedObjs.Add(newObjective);
			}

			if (antag.ChanceForGimmickObjective != 0 && DMMath.Prob(antag.ChanceForGimmickObjective))
			{
				// Add one gimmick objective
				var allowedGimmicks = GimmickObjectives.Where(obj => obj.IsPossible(Mind)).ToList();
				//TODO since checkUnique is false we dont need to remove the chosen object from EscapeObjectives
				//TODO but we would if we ever want to allow for unique gimmick objectives
				newObjective = PickRandomObjective(ref allowedGimmicks, false);
				newObjective.DoSetup(Mind);
				generatedObjs.Add(newObjective);
			}

			foreach (var alwaysStartWithObjective in antag.AlwaysStartWithObjectives)
			{
				var objectiveNew = Instantiate(alwaysStartWithObjective);
				objectiveNew.DoSetup(Mind);
				generatedObjs.Add(objectiveNew);
			}

			return generatedObjs;
		}


		/// <summary>
		/// Returns all possible objectives for provided antag type
		/// </summary>
		/// <param name="antag">The antag type</param>
		public List<Objective> GetAllPosibleObjectives(Antagonist antag)
		{
			List<Objective> objPool = antag.CoreObjectives.Where(obj => antag.BlackListedObjectives.Contains(obj) == false).ToList();
			if (antag.CanUseSharedObjectives)
			{
				objPool = objPool.Concat(SharedObjectives).Where(obj => antag.BlackListedObjectives.Contains(obj) == false).ToList();
			}
			objPool = objPool.Concat(EscapeObjectives).Where(obj => antag.BlackListedObjectives.Contains(obj) == false).ToList();
			if (antag.ChanceForGimmickObjective != 0 && DMMath.Prob(antag.ChanceForGimmickObjective))
			{
				objPool = objPool.Concat(GimmickObjectives).Where(obj => antag.BlackListedObjectives.Contains(obj) == false).ToList();
			}
			return objPool;
		}

		/// <summary>
		/// Returns all possible objectives for provided antag type
		/// </summary>
		/// <param name="antag">The antag type</param>
		public List<Objective> GetAllBasicObjectives()
		{
			// Get all antag core and shared objectives which are possible for this player
			List<Objective> objPool = EscapeObjectives;
			objPool = objPool.Concat(SharedObjectives).ToList();

			return objPool;
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

		public Antagonist FromIndexAntag(short index)
		{
			if (index < 0 || index > Antags.Count - 1)
			{
				Loggy.LogErrorFormat("AntagData: no Antagonist found at index {0}", Category.Antags, index);
				return null;
			}

			return Antags[index];
		}

		public short GetIndexAntag(Antagonist antag)
		{
			return (short)Antags.IndexOf(antag);
		}

		public Objective FromIndexObj(short index)
		{
			if (index < 0 || index > AllObjectives.Count - 1)
			{
				Loggy.LogErrorFormat("AntagData: no Objective found at index {0}", Category.Antags, index);
				return null;
			}

			return AllObjectives[index];
		}

		public short GetIndexObj(Objective obj)
		{
			return (short)AllObjectives.IndexOf(obj);
		}

		public short GetTeamIndex(TeamData data)
		{
			return (short)teamDatas.IndexOf(data);
		}

		public TeamData GetFromIndex(short index)
		{
			if (index < 0 || index > teamDatas.Count - 1)
			{
				Loggy.LogErrorFormat("TeamList: no TeamData found at index {0}", Category.Antags, index);
				return null;
			}

			return teamDatas[index];
		}
	}
}
