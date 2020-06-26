using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Antagonists
{
	/// <summary>
	/// Represents an antag that has been spawned into the game. Used so that Antagonist SO can remain
	/// immutable and server merely as the definition of a particular kind of antag and this
	/// can server as an actual INSTANCE of that antag in a given round.
	/// </summary>
	public class SpawnedAntag
	{

		/// <summary>
		/// Antagonist this player spawned as.
		/// </summary>
		public readonly Antagonist Antagonist;

		/// <summary>
		/// Player controlling this antag.
		/// </summary>
		public readonly Mind Owner;

		/// <summary>
		/// The objectives this antag has been given
		/// </summary>
		public IEnumerable<Objective> Objectives;

		private SpawnedAntag(Antagonist antagonist, Mind owner, IEnumerable<Objective> objectives)
		{
			Antagonist = antagonist;
			Owner = owner;
			Objectives = objectives;
		}

		/// <summary>
		/// Create a spawned antag of the indicated antag type for the indicated mind with the given objectives.
		/// </summary>
		/// <param name="antagonist"></param>
		/// <param name="owner"></param>
		/// <param name="objectives"></param>
		/// <returns></returns>
		public static SpawnedAntag Create(Antagonist antagonist, Mind owner, IEnumerable<Objective> objectives)
		{
			return new SpawnedAntag(antagonist, owner, objectives);
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
			StringBuilder objSB = new StringBuilder($"</i><size=26>You are a <b>{Antagonist.AntagName}</b>!</size>\n", 200);
			var objectiveList = Objectives.ToList();
			objSB.AppendLine("Your objectives are:");
			for (int i = 0; i < objectiveList.Count; i++)
			{
				objSB.AppendLine($"{i+1}. {objectiveList[i].Description}");
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
			var objectiveList = Objectives.ToList();
			objSB.AppendLine("Their objectives were:");
			for (int i = 0; i < objectiveList.Count; i++)
			{
				objSB.Append($"{i+1}. {objectiveList[i].Description}: ");
				objSB.AppendLine(objectiveList[i].IsComplete() ? "<color=green><b>Completed</b></color>" : "<color=red><b>Failed</b></color>");
			}
			return objSB.ToString();
		}

		public string GetObjectiveStatusNonRich()
		{
			var message = $"{Owner.body.playerName}\n";
			var objectiveList = Objectives.ToList();
			message += "Their objectives were:\n";
			for (int i = 0; i < objectiveList.Count; i++)
			{
				message += $"{i + 1}. {objectiveList[i].Description}: ";
				message += objectiveList[i].IsComplete() ? "Completed\n" : "Failed\n";
			}
			return message;
		}
	}
}