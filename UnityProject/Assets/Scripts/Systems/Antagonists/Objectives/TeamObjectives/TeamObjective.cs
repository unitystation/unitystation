using System;
using System.Collections.Generic;
using Items;
using UnityEngine;

namespace Antagonists
{
	public class TeamObjective : Objective
	{
		protected Team team;
		public Team Team => team;

		protected override bool CheckCompletion()
		{
			return false;
		}

		protected override void Setup()
		{
			
		}

		public virtual void OnCanceling()
		{
			
		}

		/// <summary>
		/// Sets the owner of the objective and performs setup if required
		/// </summary>
		public void DoSetup(Team teamToSetup)
		{
			team = teamToSetup;
			ID = Guid.NewGuid().ToString();

			try
			{
				SetupInGame();
			}
			catch (Exception e)
			{
				Logger.LogError($"Failed to set up objectives for {this.name}" + e.ToString());
			}

		}
	}
}