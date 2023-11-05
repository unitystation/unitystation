using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Strings;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Antagonists
{
	/// <summary>
	/// Represents an antag that has been spawned into the game. Used so that Antagonist SO can remain
	/// immutable and server merely as the definition of a particular kind of antag and this
	/// can server as an actual INSTANCE of that antag in a given round.
	/// </summary>
	public class SpawnedAntag : MonoBehaviour
	{

		/// <summary>
		/// Antagonist this player spawned as.
		/// </summary>
		private Antagonist curAntagonist;
		public Antagonist Antagonist => curAntagonist;

		private Team curTeam = null;
		public Team CurTeam
		{
			get
			{
				return curTeam;
			}
			set
			{
				curTeam?.RemoveTeamMember(curOwner);
				value?.AddTeamMember(curOwner);
				curTeam = value;
			}
		}
		public bool IsAntagCanSeeObjectivesStatus { get; set; } = false;

		/// <summary>
		/// Player controlling this antag.
		/// </summary>
		private Mind curOwner;
		public Mind Owner
		{
			get
			{
				if (curOwner == null)
					curOwner = gameObject.GetComponent<Mind>();
				return curOwner;
			}
		}

		/// <summary>
		/// The objectives this antag has been given
		/// </summary>
		public IEnumerable<Objective> Objectives = new List<Objective>();

		/// <summary>
		/// Returns a spawned antag of the indicated antag type for the indicated mind with the given objectives.
		/// </summary>
		/// <param name="antagonist"></param>
		/// <param name="owner"></param>
		/// <param name="objectives"></param>
		/// <returns></returns>
		public SpawnedAntag Init(Antagonist antagonist, Mind mind, IEnumerable<Objective> objectives)
		{
			curAntagonist = antagonist;
			curOwner = mind;
			Objectives = objectives;
			return this;
		}

		/// <summary>
		/// Clears antag objectives and other stuff
		/// </summary>
		public void Clear()
		{
			Objectives = new List<Objective>();
			IsAntagCanSeeObjectivesStatus = false;
			curAntagonist = null;
		}

		/// <summary>
		/// Returns a string with just the objectives for logging
		/// </summary>
		public string GetObjectivesForLog()
		{
			StringBuilder objSB = new StringBuilder(200);
			var objectiveList = Objectives.ToList();
			for (int i = 0; i < objectiveList.Count; i++)
			{
				objSB.AppendLine($"{i+1}. {objectiveList[i].Description}");
			}
			return objSB.ToString();
		}

		/// <summary>
		/// Returns a string with the current objectives for this antag which will be shown to the player
		/// </summary>
		public string GetObjectivesForPlayer()
		{
			StringBuilder objSB = new StringBuilder("", 200);
			if (Antagonist != null)
			{
				objSB.Append($"</i><size={ChatTemplates.VeryLargeText}><color=red>You are a <b>{Antagonist.AntagName}</b>!</color></size>\n");
			} else
			{
				objSB.Append($"</i><size={ChatTemplates.VeryLargeText}><color=red>You have objectives!</color></size>\n");
			}
			var objectiveList = Objectives.ToList();
			objSB.AppendLine("Your objectives are:");
			for (int i = 0; i < objectiveList.Count; i++)
			{
				if (IsAntagCanSeeObjectivesStatus == true)
				{
					objSB.AppendLine($"{i + 1}. {objectiveList[i].Description}:");
					objSB.AppendLine(objectiveList[i].IsComplete() ? "<color=green>Completed\n" : "In progress/Failed");
				} else
				{
					objSB.AppendLine($"{i + 1}. {objectiveList[i].Description}");
				}
			}
			if (CurTeam != null)
			{
				objSB.AppendLine($"You are member of {CurTeam.GetTeamName()}.");
				if (CurTeam.TeamObjectives.Count > 0)
				{
					objSB.AppendLine($"And {CurTeam.GetTeamName()} objectives are:");
				}
				for (int i = 0; i < CurTeam.TeamObjectives.Count; i++)
				{
					var obj = CurTeam.TeamObjectives[i];

					objSB.AppendLine($"{i + 1}. {obj.Description}");
				}
			}
			// Adding back italic tag so rich text doesn't break
			objSB.AppendLine("<i>");
			return objSB.ToString();
		}

		public string GetObjectiveSummary()
		{
			StringBuilder objSB = new StringBuilder("\r\n", 200);
			var objectiveList = Objectives.ToList();
			for (int i = 0; i < objectiveList.Count; i++)
			{
				objSB.AppendLine($"{i+1}. {objectiveList[i].Description}");
			}
			return objSB.ToString();
		}

		/// <summary>
		/// Returns a string with the status of all objectives for this antag
		/// </summary>
		public string GetObjectiveStatus()
		{
			StringBuilder objSB = new StringBuilder($"<b>{Owner.Body.playerName}</b>, {Owner.occupation.DisplayName}\n", 200);
			var objectiveList = Objectives.ToList();
			for (int i = 0; i < objectiveList.Count; i++)
			{
				objSB.Append($"{i+1}. {objectiveList[i].Description}: ");
				objSB.AppendLine(objectiveList[i].IsComplete() ? "<color=green><b>Completed</b></color>" : "<color=red><b>Failed</b></color>");
			}
			return objSB.ToString();
		}

		public string GetObjectiveStatusNonRich()
		{
			var message = $"{Owner.OrNull()?.Body.OrNull()?.playerName}, {Owner.OrNull()?.occupation.OrNull()?.DisplayName}\n";
			var objectiveList = Objectives.ToList();
			for (int i = 0; i < objectiveList.Count; i++)
			{
				message += $"{i + 1}. {objectiveList[i].Description}: ";
				message += objectiveList[i].IsComplete() ? "Completed\n" : "Failed\n";
			}
			return message;
		}

		public string GetPlayerName()
		{
			var message = $"{Owner.OrNull()?.Body.OrNull()?.playerName}, {Owner.OrNull()?.occupation.OrNull()?.DisplayName}\n";
			return message;
		}
	}
}
