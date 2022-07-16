using Systems.MobAIs;
using Chemistry;
using Chemistry.Components;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NPC.Mood
{
	[RequireComponent(typeof(MobMood))]
	public class MilkableMob: MonoBehaviour, ICheckedInteractable<HandApply>, IServerLifecycle
	{
		#region Inspector
		[BoxGroup("Reagent")]
		[SerializeField]
		[Tooltip("What reagent to use as the milk of this mob.")]
		private Reagent milkReagent = default;

		[BoxGroup("Reagent")]
		[SerializeField]
		[Tooltip("Amount of reagent you get with each interaction.")]
		private float milkAmount = 25;

		[BoxGroup("Reagent")]
		[SerializeField]
		[Tooltip("The update frequency this mob will use for milk production.")]
		private float milkTick = 120;

		[BoxGroup("Reagent")]
		[SerializeField]
		[Tooltip("The amount of milk the mob can produce each update period.")]
		private float milkProducePerTick = 25;

		[BoxGroup("Reagent")]
		[SerializeField]
		[Tooltip("Max amount of milk the mob can produce before it starts losing mood.")]
		private float maxMilk = 100;

		[BoxGroup("Mood")]
		[SerializeField]
		[Tooltip("Amount of mood required to produce milk")]
		private int requiredMood = 40;

		[BoxGroup("Mood")]
		[SerializeField]
		[Tooltip("Amount of mood the mob loses each milkTick when it already produced the max amount or is milked " +
		         "before it has produced the milk")]
		private int moodPenalty = 10;

		[BoxGroup("Effects")]
		[SerializeField]
		[Tooltip("Text action to display when you milk the mob")]
		private string milkMessageYou = "You milk {0}.";

		[BoxGroup("Effects")]
		[SerializeField]
		[Tooltip("Text action to display for others when you milk the mob")]
		private string milkMessageOthers = "{1} milks {0}";

		[BoxGroup("Effects")]
		[SerializeField]
		[Tooltip("Text action to display when the mob is milked while it has no milk.")]
		private string noMilkMessage = "You pull the teat but no milk is coming out!";

		[BoxGroup("Effects")]
		[SerializeField]
		[Tooltip("Optional. Function we run when the mob is milked while it doesn't have enough milk, " +
		         "leave empty for no special consequences.")]
		private UnityEvent<GameObject, GameObject> onMilkedEmpty = default;
		#endregion

		private float currentMilkAmount;
		private MobMood mood;
		private MobAI mobAI;

		private void Milk(GameObject performer, ReagentContainer container)
		{
			if (currentMilkAmount < milkAmount)
			{
				onMilkedEmpty?.Invoke(performer, gameObject);
				mood.UpdateLevel(moodPenalty * -1);

				Chat.AddActionMsgToChat(
					performer,
					string.Format(noMilkMessage, performer.ExpensiveName(), mobAI.mobName.Capitalize()),
					""
				);

				return;
			}

			Chat.AddActionMsgToChat(
				performer,
				string.Format(milkMessageYou, mobAI.mobName.Capitalize()),
				string.Format(milkMessageOthers, mobAI.mobName.Capitalize(), performer.ExpensiveName())
			);

			currentMilkAmount -= milkAmount;
			var milkyMix = new ReagentMix(milkReagent, milkAmount, 40f);

			container.Add(milkyMix);
		}

		private void MilkUpdate()
		{
			if (mood.LevelPercent < requiredMood)
			{
				currentMilkAmount = 0;
				mood.MoodChanged += OnMoodChanged;
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, MilkUpdate);
				return;
			}

			if (currentMilkAmount >= maxMilk)
			{
				mood.UpdateLevel(moodPenalty * -1);
				return;
			}

			currentMilkAmount += milkProducePerTick;
		}

		private void OnMoodChanged()
		{
			if (mood.LevelPercent < requiredMood)
			{
				return;
			}

			UpdateManager.Add(MilkUpdate, milkTick);
			mood.MoodChanged -= OnMoodChanged;
		}

		#region Lifecycle
		private void Awake()
		{
			mobAI = GetComponent<MobAI>();
			mood = GetComponent<MobMood>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (CustomNetworkManager.Instance == null)
			{
				return;
			}

			UpdateManager.Add(MilkUpdate, milkTick);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, MilkUpdate);
		}

		#endregion

		#region interaction
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side) &&
			       interaction.TargetObject == gameObject &&
			       interaction.HandObject != null &&
			       interaction.HandObject.TryGetComponent<ReagentContainer>(out _);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			Milk(interaction.Performer, interaction.HandObject.GetComponent<ReagentContainer>());
		}
		#endregion
	}
}
