using Core.Chat;
using HealthV2;
using ScriptableObjects.RP;
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

		[Tooltip("The number of levels a sickness has.")]
		public int NumberOfStages = 1;

		private int currentStage = 1;
		public int CurrentStage => currentStage;

		public List<Chemistry.Reagent> PossibleCures = new List<Chemistry.Reagent>();
		public List<RaceHealthData> ImmuneRaces = new List<RaceHealthData>();
		private Chemistry.Reagent cureForSickness = null;
		private List<Chemistry.Reagent> cureHints = new List<Chemistry.Reagent>();

		[SerializeField, Tooltip("basic Symptomp feedback")] protected EmoteSO emoteFeedback;

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
		public bool Contagious => contagious;

		public void SetCure()
		{
			if (PossibleCures.Count != 0) cureForSickness = PossibleCures.PickRandom();
			FillCureHints();
		}

		public void SetCure(Chemistry.Reagent cure)
		{
			cureForSickness = cure;
			FillCureHints();
		}

		private void FillCureHints()
		{
			if (cureForSickness != null && cureForSickness.RelatedReactions.Length > 0)
			{
				cureHints.AddRange(cureForSickness.RelatedReactions[Random.Range(0, cureForSickness.RelatedReactions.Length)].catalysts.Keys);
			}
		}

		public virtual void SicknessBehavior(LivingHealthMasterBase health)
		{
			SymptompFeedback(health);
		}

		public virtual void SymptompFeedback(LivingHealthMasterBase health)
		{
			EmoteActionManager.DoEmote(emoteFeedback, health.gameObject);
		}

		public virtual bool CheckForCureInHealth(LivingHealthMasterBase health)
		{
			if (health.CirculatorySystem.BloodPool.reagentKeys.Contains(cureForSickness) == false) return false;
			return true;
		}
	}
}