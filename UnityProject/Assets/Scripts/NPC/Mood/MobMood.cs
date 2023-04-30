using System;
using Systems.MobAIs;
using NaughtyAttributes;
using UnityEngine;

namespace NPC.Mood
{
	[RequireComponent(typeof(MobAI))]

	public class MobMood : MonoBehaviour, IServerSpawn, IServerDespawn
	{
		#region inspector exposed variables
		[SerializeField, Tooltip("Set the initial mood level this creature will have once spawned")]
		[MinValue(0)]
		private int initialMoodLevel = 50;

		[SerializeField, Tooltip("Mood level will never go beyond this level")]
		private int maxMoodLevel = 100;

		[SerializeField, Tooltip("If true, damage will affect this creature's mood")]
		private bool isAffectedByDamage = true;

		[SerializeField,
		 Tooltip("Amount of mood level that will be added or subtracted when hit. " +
		         "Consider positive numbers as happiness and negative as unhappiness"),
		 ShowIf(nameof(isAffectedByDamage))]
		private int moodOnHit = -10;

		[SerializeField, Tooltip("If true, petting will improve this creature's mood level")]
		private bool isAffectedByPetting = false;

		[SerializeField, Tooltip("How much does petting affect mood. Use positive or negative numbers"),
		 ShowIf(nameof(isAffectedByPetting))]
		private int moodOnPetted = 10;

		[SerializeField,
		 Tooltip("Petting will only affect mood when it is not in cooldown. " +
		         "A negative number here means not cooldown at all."),
		 ShowIf(nameof(isAffectedByPetting))]
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
		public int LevelPercent => Mathf.RoundToInt(level / maxMoodLevel * 100);

		public event Action MoodChanged;

		private MobAI mobAi;
		private MobExplore mobExplore;
		private LivingHealthBehaviour lbh;
		private DateTime lastPetted;

		#region LifeCycle
		private void Awake()
		{
			mobAi = GetComponent<MobAI>();
			lbh = GetComponent<LivingHealthBehaviour>();
			mobExplore = GetComponent<MobExplore>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			level = initialMoodLevel;

			if (CustomNetworkManager.Instance == null)
			{
				return;
			}

			UpdateManager.Add(ServerPeriodicUpdate, timeBetweenFoodCheck);

			lbh.applyDamageEvent += OnDamageReceived;
			mobAi.PettedEvent += OnPetted;
			mobExplore.FoodEatenEvent += OnFoodEaten;
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ServerPeriodicUpdate);

			mobAi.health.applyDamageEvent -= OnDamageReceived;
			mobAi.PettedEvent -= OnPetted;
			mobExplore.FoodEatenEvent -= OnFoodEaten;
		}
		#endregion

		#region Events
		private void OnPetted()
		{
			if (!isAffectedByPetting)
			{
				return;
			}

			if (pettingCooldown < 0 || lastPetted.AddSeconds(pettingCooldown) > GameManager.Instance.RoundTime)
			{
				UpdateLevel(moodOnPetted);
			}

			lastPetted = GameManager.Instance.RoundTime;
		}

		private void OnDamageReceived(GameObject obj)
		{
			if (!isAffectedByDamage)
			{
				return;
			}

			UpdateLevel(moodOnHit);
		}

		public void OnFoodEaten()
		{
			if (!isAffectedByFood)
			{
				return;
			}

			UpdateLevel(moodOnFood);
		}
		#endregion

		private void ServerPeriodicUpdate()
		{
			// Hungry tick
			UpdateLevel(moodOnFood * -1);
		}

		/// <summary>
		/// Public method to modify this creature's mood level.
		/// Use positive numbers for happiness and negative numbers to make them sad/angry.
		/// </summary>
		/// <param name="amount">How much joy/suffering you want this creature to feel, you monster</param>
		public void UpdateLevel(int amount)
		{
			SetLevel(level + amount);
		}

		/// <summary>
		/// Public method to directly set this creature's mood to a particular level.
		/// Use positive numbers for happiness and negative numbers to make them sad/angry
		/// </summary>
		/// <param name="amount"></param>
		public void SetLevel(int amount)
		{
			// maybe do a happy/unhappy face animation to represent this change?
			level = Mathf.Clamp(amount, 0, maxMoodLevel);
			MoodChanged?.Invoke();
		}
	}
}
