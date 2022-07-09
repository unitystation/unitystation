using System.Collections;
using Core.Chat;
using HealthV2;
using ScriptableObjects.RP;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Health.Sickness
{
	public class Sickness : MonoBehaviour
	{
		/// <summary>
		/// Name of the sickness
		/// </summary>
		[SerializeField]
		private string sicknessName = "<Unnamed>";

		[Tooltip(" Indicates if the sickness is contagious or not.")]
		public bool Contagious = true;
		[Range(0,12f)] public float ContagiousRadius = 6f;
		[SerializeField, Range(0,100f)] private float infectOtherChance = 50f;

		[Tooltip("The number of levels a sickness has.")]
		public int NumberOfStages = 1;

		[Range(0f, 9320), Tooltip("Set it to less than 2 ticks to disable automatic progression.")]
		public int TicksToPogressStages = 50;
		private int currentTicksSinceLastProgression = 0;

		private int currentStage = 1;
		public int CurrentStage => currentStage;

		[FormerlySerializedAs("possibleCures")]public List<Chemistry.Reagent> PossibleCures = new List<Chemistry.Reagent>();
		public List<PlayerHealthData> ImmuneRaces = new List<PlayerHealthData>();
		public Chemistry.Reagent CureForSickness = null;
		public List<Chemistry.Reagent> CureHints = new List<Chemistry.Reagent>();

		[SerializeField, Tooltip("basic Symptomp feedback")] protected EmoteSO emoteFeedback;

		[SerializeField, Range(10f,60f)] private float cooldownTime = 10f;
		protected bool isOnCooldown = false;

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

		public void SetCure()
		{
			if (PossibleCures.Count != 0) CureForSickness = PossibleCures.PickRandom();
			FillCureHints();
		}

		public void SetCure(Chemistry.Reagent cure)
		{
			if (cure == null) return;
			CureForSickness = cure;
			FillCureHints();
		}

		private void FillCureHints()
		{
			if (CureForSickness != null && CureForSickness.RelatedReactions.Length > 0)
			{
				CureHints.AddRange(CureForSickness.RelatedReactions[Random.Range(0, CureForSickness.RelatedReactions.Length)].catalysts.Keys);
			}
		}

		public virtual void SicknessBehavior(LivingHealthMasterBase health)
		{
			if(isOnCooldown) return;
			SymptompFeedback(health);
			currentTicksSinceLastProgression += 1;
			if(currentTicksSinceLastProgression >= TicksToPogressStages && NumberOfStages > currentStage)
			{
				currentTicksSinceLastProgression = 0;
				currentStage += 1;
			}
			if(Contagious) TrySpreading();
			if(cooldownTime > 2f) health.StartCoroutine(Cooldown());
		}

		/// <summary>
		/// Attempts to spread the virus to nearby mobs.
		/// </summary>
		public virtual void TrySpreading()
		{
			if(DMMath.Prob(infectOtherChance) == false) return;
			var result = Physics2D.OverlapCircleAll(gameObject.TileLocalPosition(), ContagiousRadius);
			foreach (var obj in result)
			{
				if (obj.TryGetComponent<LivingHealthMasterBase>(out var healthBase) == false) continue;
				healthBase.AddSickness(this);
				return; //Balance note : Only infect one person to avoid mass infection problems that leads to chaos.
			}
		}

		public virtual void SymptompFeedback(LivingHealthMasterBase health)
		{
			EmoteActionManager.DoEmote(emoteFeedback, health.gameObject);
		}

		public virtual bool CheckForCureInHealth(LivingHealthMasterBase health)
		{
			if (health.CirculatorySystem.BloodPool.reagentKeys.Contains(CureForSickness) == false) return false;
			return true;
		}

		protected virtual IEnumerator Cooldown()
		{
			isOnCooldown = true;
			yield return WaitFor.Seconds(cooldownTime);
			isOnCooldown = false;
		}
	}
}