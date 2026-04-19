using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.Systems.Antagonists.Objectives.TeamObjectives;

namespace US13.Systems.Antagonists
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Team")]
	public class TeamData : ScriptableObject
	{
		[SerializeField]
		private string teamName = "DefaultName";
		public string TeamName => teamName;

		[SerializeField]
		private bool canBeAddedNewMembers = true;
		public bool CanBeAddedNewMembers => canBeAddedNewMembers;
		[SerializeField]
		private bool canBeAddedNewObjectives = true;
		public bool CanBeAddedNewObjectives => canBeAddedNewObjectives;
		[SerializeField]
		private bool needToBeShownAtRoundEnd = true;
		public bool NeedToBeShownAtRoundEnd => needToBeShownAtRoundEnd;
		[SerializeField]
		private bool alwaysSpawnOnRoundStart = true;
		public bool AlwaysSpawnOnRoundStart => alwaysSpawnOnRoundStart;

		[SerializeField]
		private bool isStationTeam = true;
		public bool IsStationTeam => isStationTeam;

		[Tooltip("The core objectives only this type of team can get")]
		[SerializeField]
		protected List<Objective> coreObjectives = new List<Objective>();
		/// <summary>
		/// The core objectives only this type of team can get
		/// </summary>
		public IEnumerable<Objective> CoreObjectives => coreObjectives;

		public List<Objective> GenerateObjectives(Team createdTeam)
		{
			if (isStationTeam == true)
			{
				return new List<Objective>();
			}

			foreach (var objective in CoreObjectives)
			{
				if (objective is TeamObjective teamObjective == false) continue;
				if(teamObjective.attributes.Count == 0) teamObjective.DoSetup(createdTeam);
			}
			return CoreObjectives.ToList();
		}
	}
}