using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace Antagonists
{
	/// <summary>
	/// Contains all information about an antagonist including objectives
	/// </summary>
	public class Antagonist : ScriptableObject
	{
		/// <summary>
		/// The player controlling this antag
		/// </summary>
		public Mind Owner { get; set; }

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
		public List<Objective> CoreObjectives => coreObjectives;

		/// <summary>
		/// The objectives this antag currently has
		/// </summary>
		protected List<Objective> CurrentObjectives = new List<Objective>();

		/// <summary>
		///	Give this antag an objective
		/// </summary>
		public void GiveObjective(Objective objective)
		{
			CurrentObjectives.Add(objective);
		}

		/// <summary>
		/// Give this antag multiple objectives at once
		/// </summary>
		public void GiveObjectives(List<Objective> objectives)
		{
			CurrentObjectives.AddRange(objectives);
		}

		/// <summary>
		/// Returns a string with just the objectives for logging
		/// </summary>
		public string GetObjectivesForLog()
		{
			StringBuilder objSB = new StringBuilder(200);
			for (int i = 0; i < CurrentObjectives.Count; i++)
			{
				objSB.AppendLine($"{i+1}. {CurrentObjectives[i].Description}");
			}
			return objSB.ToString();
		}

		/// <summary>
		/// Returns a string with the current objectives for this antag which will be shown to the player
		/// </summary>
		public string GetObjectivesForPlayer()
		{
			StringBuilder objSB = new StringBuilder($"</i><size=26>You are a <b>{antagName}</b>!</size>\n", 200);
			objSB.AppendLine("Your objectives are:");
			for (int i = 0; i < CurrentObjectives.Count; i++)
			{
				objSB.AppendLine($"{i+1}. {CurrentObjectives[i].Description}");
			}
			// Adding back italic tag so rich text doesn't break
			objSB.AppendLine("<i>");
			return objSB.ToString();
		}

		/// <summary>
		/// Returns a string with the status of all objectives for this antag
		/// </summary>
		public string GetObjectiveStatus()
		{
			StringBuilder objSB = new StringBuilder($"<b>{Owner.body.playerName}</b>\n", 200);
			objSB.AppendLine("Their objectives were:");
			for (int i = 0; i < CurrentObjectives.Count; i++)
			{
				objSB.Append($"{i+1}. {CurrentObjectives[i].Description}: ");
				objSB.AppendLine(CurrentObjectives[i].IsComplete() ? "<color=green><b>Completed</b></color>" : "<color=red><b>Failed</b></color>");
			}
			return objSB.ToString();
		}
	}
}