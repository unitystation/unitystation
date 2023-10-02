using System;
using Logs;

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
			// Required for implementing
		}

		/// <summary>
		/// Called when objective is canceled
		/// </summary>
		public virtual void OnCanceling()
		{
			// Will be called when objective is canceled
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
				Loggy.LogError($"Failed to set up objectives for {this.name}" + e.ToString());
			}

		}
	}
}