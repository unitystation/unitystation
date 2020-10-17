using System;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.MobAIs
{
	[RequireComponent(typeof(MobAI))]

	public class MobMood : MonoBehaviour, IServerSpawn, IServerDespawn
	{
		#region inspector exposed variables
		[SerializeField, Tooltip("Set the initial mood level this creature will have once spawned")]
		private int initialMoodLevel = 0;

		[SerializeField, Tooltip("Minimum Mood level for this creature. Negative level is considered \"unhappiness\"")]
		private int minMoodLevel = -100;

		[SerializeField, Tooltip("Mood level will never go beyond this level")]
		private int maxMoodLevel = 100;

		[SerializeField, Tooltip("If true, damage will affect this creature's mood")]
		private bool isAffectedByDamage = true;

		[SerializeField, Tooltip("Amount of mood level that will be added or subtracted when hit. " +
		                         "Consider positive numbers as happiness and negative as unhappiness"),
		 ShowIf(nameof(isAffectedByDamage))]
		private int moodOnHit = -10;

		[SerializeField, Tooltip("If true, petting will improve this creature's mood level")]
		private bool isAffectedByPetting = false;

		[SerializeField, Tooltip("How much does petting affect mood. Use positive or negative numbers"),
		 ShowIf(nameof(isAffectedByPetting))]
		private int moodOnPetted = 10;

		[SerializeField, Tooltip("Petting will only affect mood when it is not in cooldown. " +
		                         "A negative number here means not cooldown at all."), ShowIf(nameof(isAffectedByPetting))]
		private float pettingCooldown = -1;

		[SerializeField, Tooltip("If true, having or not having food will have an effect on this creature's mood")]
		private bool isAffectedByFood = false;

		[SerializeField, Tooltip("What's the level amount you want to modify on each meal"),
		 ShowIf(nameof(isAffectedByFood))]
		private int moodOnFood = 10;

		[SerializeField, ShowIf(nameof(isAffectedByFood))]
		private float timeBetweenFoodCheck = 300;
		#endregion

		private int level;
		public int Level => level;

		private MobAI mobAi;
		private MobExplore mobExplore;
		private LivingHealthBehaviour lbh;
		private DateTime lastPetted;

		private void Awake()
		{
			mobAi = GetComponent<MobAI>();
			lbh = GetComponent<LivingHealthBehaviour>();
			mobExplore = GetComponent<MobExplore>();
		}

		public void OnPetted()
		{
			if (!isAffectedByPetting)
			{
				return;
			}

			if (pettingCooldown < 0 || lastPetted.AddSeconds(pettingCooldown) > GameManager.Instance.stationTime)
			{
				UpdateMoodLevel(moodOnPetted);
			}

			lastPetted = GameManager.Instance.stationTime;
		}

		public void OnDamageReceived(GameObject obj)
		{
			if (!isAffectedByDamage)
			{
				return;
			}

			UpdateMoodLevel(moodOnHit);
		}

		public void OnFoodEaten()
		{
			if (!isAffectedByFood)
			{
				return;
			}

			UpdateMoodLevel(moodOnFood);
		}

		private void ServerPeriodicUpdate()
		{
			// Hungry tick
			UpdateMoodLevel(moodOnFood * -1);
		}

		/// <summary>
		/// Public method to modify this creature's mood level.
		/// Use positive numbers for happiness and negative numbers to make them sad/angry.
		/// </summary>
		/// <param name="amount">How much joy/suffering you want this creature to feel, you monster</param>
		public void UpdateMoodLevel(int amount)
		{
			// maybe do a happy/unhappy face animation to represent this change?
			level = Mathf.Clamp(level + amount, minMoodLevel, maxMoodLevel);
		}

		public void SetMoodLevel(int amount)
		{
			level = Mathf.Clamp(amount, minMoodLevel, maxMoodLevel);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			level = initialMoodLevel;

			if (CustomNetworkManager.Instance == null)
			{
				return;
			}

			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Add(ServerPeriodicUpdate, timeBetweenFoodCheck);
			}

			lbh.applyDamageEvent += OnDamageReceived;
			mobAi.PettedEvent += OnPetted;
			mobExplore.FoodEatenEvent += OnFoodEaten;
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ServerPeriodicUpdate);
			}

			mobAi.health.applyDamageEvent -= OnDamageReceived;
			mobAi.PettedEvent -= OnPetted;
			mobExplore.FoodEatenEvent -= OnFoodEaten;
		}
	}
}
