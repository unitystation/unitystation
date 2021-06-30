using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StationObjectives
{
	public abstract class StationObjective : ScriptableObject
	{
		/// <summary>
		/// The description of the objective which is shown to players.
		/// </summary>
		public string description;

		/// <summary>
		/// The description read out at the end of the round.
		/// </summary>
		[SerializeField]
		protected string roundEndReport;

		/// <summary>
		/// Set true when the objective has been completed
		/// </summary>
		protected bool Complete;

		/// <summary>
		/// Perform initial setup of the objective if needed
		/// </summary>
		public abstract void Setup();

		/// <summary>
		/// Override to make modifications of the end report
		/// </summary>
		public virtual string GetRoundEndReport()
		{
			return roundEndReport;
		}

		public virtual bool CheckCompletion()
		{
			return Complete;
		}
	}
}
