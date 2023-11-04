using Antagonists;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StationObjectives
{
	public class StationObjective : TeamObjective
	{
		/// <summary>
		/// The description read out at the end of the round.
		/// </summary>
		[SerializeField]
		protected string roundEndReport;

		public void DoSetupStationObjective()
		{
			Setup();
			GameManager.Instance.CentComm.MakeCommandReport(Description, false);
		}

		/// <summary>
		/// Override to make modifications of the end report
		/// </summary>
		public virtual string GetRoundEndReport()
		{
			return roundEndReport;
		}

		public virtual bool CheckStationObjectiveCompletion()
		{
			return Complete;
		}

		protected override void Setup()
		{
			// Required for implementing
		}

		protected override bool CheckCompletion()
		{
			return CheckStationObjectiveCompletion();
		}
	}
}
