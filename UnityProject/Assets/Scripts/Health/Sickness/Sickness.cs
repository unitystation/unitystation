﻿using System;
using System.Collections;
using Core.Chat;
using HealthV2;
using ScriptableObjects.RP;
using System.Collections.Generic;
using System.Linq;
using Chemistry;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

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

		[FormerlySerializedAs("possibleCures")]public List<Reaction> PossibleCures = new List<Reaction>();
		public List<PlayerHealthData> ImmuneRaces = new List<PlayerHealthData>();
		public Reagent CureForSickness = null;
		public List<Reagent> CureHints = new List<Chemistry.Reagent>();

		[SerializeField, Tooltip("basic Symptomp feedback")] protected EmoteSO emoteFeedback;

		[SerializeField, Range(10f,60f)] private float cooldownTime = 10f;
		public bool IsOnCooldown = false;

		public LayerMask PlayerMask;

		private int cureIndex = 0;
		public int CureIndex => cureIndex;

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

		private void Awake()
		{
			PlayerMask = LayerMask.NameToLayer("Players");
			SetCure();
		}

		public void SetCure()
		{
			if (PossibleCures.Count == 0) return;
			cureIndex = Random.Range(0, PossibleCures.Count - 1);
			CureForSickness = PossibleCures[cureIndex].ingredients.PickRandom().Key;
			FillCureHints();
		}

		public void SetCure(Reagent cure)
		{
			if (cure == null) return;
			CureForSickness = cure;
			FillCureHints();
		}

		private void FillCureHints()
		{
			CureHints.AddRange(PossibleCures[cureIndex].ingredients.Keys);
		}

		public virtual void SicknessBehavior(LivingHealthMasterBase health)
		{
			if(IsOnCooldown) return;
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
			var result = Physics2D.OverlapCircleAll(gameObject.TileLocalPosition(), ContagiousRadius, PlayerMask);
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
			return health.CirculatorySystem.BloodPool.reagentKeys.Contains(CureForSickness) || health.CirculatorySystem.NutrimentToConsume.ContainsKey(CureForSickness);
		}

		protected virtual IEnumerator Cooldown()
		{
			IsOnCooldown = true;
			yield return WaitFor.Seconds(cooldownTime);
			IsOnCooldown = false;
		}
	}
}