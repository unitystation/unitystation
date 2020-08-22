using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Health.Sickness
{
	/// <summary>
	/// An occurence of a sickness for a particular afflicted individual
	/// </summary>
	public class SicknessAffliction: MonoBehaviour
	{
		/// <summary>
		/// The afflicted sickness
		/// </summary>
		private Sickness sickness = null;

		/// <summary>
		/// The current stage of the sickness
		/// </summary>
		public uint CurrentStage
		{
			get
			{
				return (uint)stageNextOccurence.Count;
			}
		}

		/// <summary>
		/// The time it was when the played got the affliction.
		/// </summary>
		/// <remarks>
		/// That will tell if a new stage got reached.
		/// </remarks>
		private float contractedTime;

		public float ContractedTime
		{
			get
			{
				return contractedTime;
			}
		}

		/// <summary>
		/// Indicates the next occurence of a symptom in each stages
		/// </summary>
		/// <remarks>
		/// The index indicates which stage/symptom it is.
		/// The value is a Time at which the symptom will trigger.
		/// Value of null means that this stage's symptom is not triggered anymore.
		/// The number of elements indicates at which stage the sickness is at.
		/// </remarks>
		private List<float?> stageNextOccurence = null;

		/// <summary>
		/// Indicate the time of the next occurence of a particular stage symptom
		/// </summary>
		/// <param name="stageIndex">The stage for which to check the time of the next occurence</param>
		/// <returns>Null if the stage should not trigger any symptom anymore, otherwise, the time of the scheduled next occurence</returns>
		public float? GetStageNextOccurence(int stageIndex)
		{
			return stageNextOccurence[stageIndex];
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sicknessToAdd">The sickness to create</param>
		/// <param name="sicknessContractedTime">The time at which the player contracted the sickness.</param>
		public SicknessAffliction(Sickness sicknessToAdd, float sicknessContractedTime)
		{
			contractedTime = sicknessContractedTime;
			sickness = sicknessToAdd;
			stageNextOccurence = new List<float?>();
		}

		/// <summary>
		/// The sickness affecting the player
		/// </summary>
		public Sickness Sickness
		{
			get
			{
				return sickness;
			}
		}

		/// <summary>
		/// Schedule when will occur the next occurence of a stage symptom.
		/// </summary>
		/// <param name="stage">The stage of the sickness to schedule</param>
		/// <param name="nextOccurenceTime">The time at which the symptom will occur</param>
		public void ScheduleStageNextOccurence(int stage, float? nextOccurenceTime)
		{
 			if (stage < CurrentStage)
				stageNextOccurence[stage] = nextOccurenceTime;
			else
				stageNextOccurence.Add(nextOccurenceTime);
		}
	}
}
 