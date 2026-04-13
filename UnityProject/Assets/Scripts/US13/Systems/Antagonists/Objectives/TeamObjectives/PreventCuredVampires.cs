using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.Core.Chat;
using US13.Messages.Server;
using US13.Player;

namespace US13.Systems.Antagonists.Objectives.TeamObjectives
{
	[CreateAssetMenu(menuName = "ScriptableObjects/AntagObjectives/PreventCuredVampires")]
	public class PreventCuredVampires : TeamObjective
	{
		private int MaxNumberOfCures=> Mathf.CeilToInt(historicVampires.Count * 0.10f);
		private HashSet<Mind> historicVampires;

		protected override void Setup()
		{
			historicVampires = new HashSet<Mind>(team.TeamMembers.ConvertAll(p => p.Owner)); //initialise with all original vampires
			description = $"Prevent more than {MaxNumberOfCures} vampires from being cured";
		}

		protected override bool CheckCompletion()
		{
			//Whenever someone is added to this team, they go into historic vampires and aren't removed when they leave
			//Players can leave vampire team by being cured, thus the difference between historic and current vampires is the amount of cures.
			return historicVampires.Count - team.TeamMembers.Count <= MaxNumberOfCures;
		}

		public void AddNewVampire(Mind newVampire)
		{
			team.AddTeamMember(newVampire);
			historicVampires.Add(newVampire);
		}

		public void RemoveVampire(Mind oldVampire)
		{
			team.RemoveTeamMember(oldVampire);
		}
	}

}