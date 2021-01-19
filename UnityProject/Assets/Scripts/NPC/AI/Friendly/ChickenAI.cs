using System.Collections.Generic;
using Items;
using Items.Others;
using UnityEngine;
using AddressableReferences;

namespace Systems.MobAIs
{
	/// <summary>
	/// Generic AI for Chickens. Keep them happy and they will lay eggs!
	/// </summary>
	public class ChickenAI: GenericFriendlyAI, ICheckedInteractable<HandApply>
	{

		[SerializeField] private AddressableAudioSource EatFoodA = null;

		[SerializeField, Tooltip("Check this if this chicken is a grown up chicken in age of laying eggs")]
		private bool grownChicken = true;

		[SerializeField, Tooltip("If this chicken's mood is at least this level, she will lay eggs")]
		private int happyChicken = 50;

		[SerializeField, Tooltip("The maximum amount of eggs this chicken can lay in her lifetime")]
		private int maxEggsAmount = 4;

		[SerializeField, Tooltip("Egg reference so we can spawn it")]
		private GameObject egg = null;

		[SerializeField, Tooltip("The little chick will grow to be one of these chickens")]
		private List<GameObject> possibleGrownForms = null;

		private int currentLaidEggs;
		private MobMood mood;

		protected override void Awake()
		{
			base.Awake();
			mood = GetComponent<MobMood>();
		}

		protected override void OnSpawnMob()
		{
			base.OnSpawnMob();
			currentLaidEggs = 0;
			BeginExploring();
		}

		protected override void DoRandomAction()
		{
			// Check if we're a happy chiken/chick or we alreday laid all possible egs
			if (mood.Level < happyChicken || currentLaidEggs == maxEggsAmount)
			{
				return;
			}

			// roll current mood level
			if (!DMMath.Prob(mood.Level/2))
			{
				return;
			}

			if (grownChicken)
			{
				// Lay egg
				var eggGo = Spawn.ServerPrefab(
					egg,
					gameObject.RegisterTile().WorldPosition,
					scatterRadius: 1f);

				if (eggGo.Successful)

				{
					// chances of fertilized egg are equal to mommy's happiness
					eggGo.GameObject.GetComponent<ChickenEgg>().SetFertilizedChance(mood.Level);
					currentLaidEggs++;
				}
			}
			else
			{
				// grow to become a cute chicken
				Spawn.ServerPrefab(possibleGrownForms.PickRandom(), gameObject.RegisterTile().WorldPosition);
				Despawn.ServerSingle(gameObject);
			}
		}

		// Manually feeding
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side) &&
			       !health.IsDead &&
			       !health.IsCrit &&
			       !health.IsSoftCrit &&
			       !health.IsCardiacArrest &&
			       interaction.Intent == Intent.Help &&
			       mobExplore.IsInFoodPreferences(interaction.HandObject.GetComponent<ItemAttributesV2>());
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			Inventory.ServerConsume(interaction.HandSlot, 1);
			mood.OnFoodEaten();
			SoundManager.PlayNetworkedAtPos(
				EatFoodA,
				gameObject.RegisterTile().WorldPosition,
				1f,
				sourceObj: gameObject);

			Chat.AddActionMsgToChat(
				interaction.Performer,
				$"You feed {mobNameCap} with {interaction.HandObject.ExpensiveName()}",
				$"{interaction.Performer.ExpensiveName()}" +
				$" feeds some {interaction.HandObject.ExpensiveName()} to {mobNameCap}");
		}
	}
}