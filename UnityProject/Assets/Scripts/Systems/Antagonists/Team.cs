using System.Collections.Generic;
using NaughtyAttributes;
using AddressableReferences;
using Player;
using Systems.GhostRoles;
using System.Text;
using System.Linq;
using System;
using StationObjectives;

namespace Antagonists
{
	/// <summary>
	/// Defines team.
	/// </summary>
	public class Team
	{
		private List<SpawnedAntag> teamMembers = new List<SpawnedAntag>();
		public List<SpawnedAntag> TeamMembers
		{
			get
			{
				var checkedTeamMembers = new List<SpawnedAntag>();
				foreach (var x in teamMembers)
				{
					if (x != null && x.CurTeam == this)
					{
						checkedTeamMembers.Add(x);
					}
				}
				teamMembers = checkedTeamMembers;
				return new(checkedTeamMembers);
			}
		}

		private List<Objective> teamObjectives = new List<Objective>();
		public List<Objective> TeamObjectives
		{
			get
			{
				if (data.IsStationTeam)
				{
					return new List<Objective>(StationObjectiveManager.Instance.ActiveObjective);
				} else
				{
					return new List<Objective>(teamObjectives);
				}
			}
		}

		private TeamData data;
		public TeamData Data => data;

		public string CustomTeamName = "";

		public static Team CreateTeam(TeamData data)
		{
			var newTeam = new Team
			{
				data = data
			};
			return newTeam;
		}

		public static Team CreateTeam(TeamData data, List<SpawnedAntag> newTeamMembers)
		{
			var newTeam = new Team
			{
				data = data,
				teamMembers = newTeamMembers
			};
			return newTeam;
		}

		public static Team CreateTeam(TeamData data, List<SpawnedAntag> newTeamMembers, List<Objective> objectives)
		{
			var newTeam = new Team
			{
				data = data,
				teamMembers = newTeamMembers,
				teamObjectives = objectives
			};
			return newTeam;
		}

		public void RemindEveryone()
		{
			foreach (var player in teamMembers)
			{
				player.Owner.ShowObjectives();
			}
		}

		public void AddTeamMember(Mind playerToAdd)
		{
			if (!teamMembers.Contains(playerToAdd.AntagPublic))
				teamMembers.Add(playerToAdd.AntagPublic);
		}

		public void RemoveTeamMember(Mind playerToAdd)
		{
			if (teamMembers.Contains(playerToAdd.AntagPublic))
				teamMembers.Remove(playerToAdd.AntagPublic);
		}

		public void AddTeamObjective(Objective objectiveToAdd)
		{
			if (data.IsStationTeam && objectiveToAdd is StationObjective stObj)
			{
				StationObjectiveManager.Instance.AddObjective(stObj);
			} else
			{
				teamObjectives.Add(objectiveToAdd);
			}
		}

		public void AddTeamObjectives(List<Objective> objectives)
		{
			teamObjectives.AddRange(objectives);
		}

		public virtual string GetObjectiveStatus()
		{
			var message = new StringBuilder($"\nThe {GetTeamName()} were: ");
			var objectiveList = teamObjectives.ToList();
			bool noPlayerShown = true;
			foreach (var teamPlayer in teamMembers)
			{
				if (teamPlayer.CurTeam != this)
					continue;
				noPlayerShown = false;
				message.AppendLine($"Team member was {teamPlayer.GetPlayerName()}");
				if (teamPlayer.Owner != null && teamPlayer.Owner.Body != null)
				{
					message.AppendLine($"(Current status {(teamPlayer.Owner.Body.IsDeadOrGhost ? "<color=red>Dead</color>" : "<color=green>Alive</color>")})");
					message.AppendLine($"had following objectives: {teamPlayer.GetObjectiveStatus()}");
				} else
				{
					message.AppendLine($"(Current status unknown)");
				}
			}
			if (noPlayerShown == true)
			{
				message.AppendLine($"Current team members status unknown");
			}

			if (objectiveList.Count > 0)
				message.AppendLine($"{GetTeamName()} had following objectives: ");
			for (int i = 0; i < objectiveList.Count; i++)
			{
				message.AppendLine($"{i + 1}. {objectiveList[i].Description}: ");
				message.AppendLine(objectiveList[i].IsComplete() ? "<color=green><b>Completed</b></color>" : "<color=red><b>Failed</b></color>");
			}
			return message.ToString();
		}

		public string GetTeamName()
		{
			if (CustomTeamName.Length > 0)
				return CustomTeamName;
			return $"{data.TeamName} {(AntagManager.Instance.GetTeamsByTeamData(data).Count > 1 ? $"{AntagManager.Instance.GetTeamsByTeamData(data).Count}" : "")}";
		}

		public TeamObjective GetObjectiveByID(string iD)
		{
			foreach (var x in TeamObjectives)
			{
				if (x.ID == iD && x is TeamObjective teamObj)
				{
					return teamObj;
				}
			}
			return null;
		}

		public void RemoveTeamObjective(TeamObjective obj)
		{
			if (data.IsStationTeam && obj is StationObjective stObj)
			{
				StationObjectiveManager.Instance.RemoveStationObjective(stObj);
				return;
			}
			obj.OnCanceling();
			teamObjectives.Remove(obj);
		}
	}
}
