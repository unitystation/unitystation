using HealthV2;
using System.Collections.Generic;
using UnityEngine;

namespace Health.Sickness
{
	public class Sickness : MonoBehaviour
	{
		/// <summary>
		/// Name of the sickness
		/// </summary>
		[SerializeField]
		private string sicknessName = "<Unnamed>";

		/// <summary>
		/// Indicates if the sickness is contagious or not.
		/// </summary>
		[SerializeField]
		private bool contagious = true;

		/// <summary>
		/// List of all the stages of a particular sickness
		/// </summary>
		[SerializeField]
		private List<SicknessStage> sicknessStages = null;

		[SerializeField] private List<Chemistry.Reagent> possibleCures = new List<Chemistry.Reagent>();
		private Chemistry.Reagent cureForSickness = null;

		public Sickness()
		{
			sicknessStages = new List<SicknessStage>();
		}

		/// <summary>
		/// Name of the sickness
		/// </summary>
		public string SicknessName
		{
			get
			{
				if (string.IsNullOrEmpty(sicknessName))
					return "<Unnamed>";

				return sicknessName;
			}
		}

		/// <summary>
		/// Indicates if the sickness is contagious or not.
		/// </summary>
		public bool Contagious
		{
			get
			{
				return contagious;
			}
		}

		/// <summary>
		/// List of all the stages of a particular sickness
		/// </summary>
		public List<SicknessStage> SicknessStages
		{
			get
			{
				return sicknessStages;
			}
		}

		public void SetCure()
		{
			if (possibleCures.Count != 0) cureForSickness = possibleCures.PickRandom();
		}

		public void SetCure(Chemistry.Reagent cure)
		{
			cureForSickness = cure;
		}

		public virtual void SicknessBehavior(LivingHealthMasterBase health) { }

		public virtual bool CheckForCureInHealth(LivingHealthMasterBase health)
		{
			if (health.CirculatorySystem.BloodPool.reagentKeys.Contains(cureForSickness) == false) return false;
			return true;
		}
	}
}