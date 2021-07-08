using System.Collections.Generic;
using Items.Others;
using NPC.Mood;
using UnityEngine;

namespace Systems.MobAIs
{
	/// <summary>
	/// Generic AI for Chickens. Keep them happy and they will lay eggs!
	/// </summary>
	public class ChickenAI: GenericFriendlyAI
	{
		[SerializeField, Tooltip("Check this if this chicken is a grown up chicken in age of laying eggs")]
		private bool grownChicken = true;

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
		}

		protected override void DoRandomAction()
		{
			// Check if we alreday laid all possible egs
			if (currentLaidEggs >= maxEggsAmount)
			{
				return;
			}

			// roll current mood level
			if (DMMath.Prob(mood.LevelPercent) == false)
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

				if (!eggGo.Successful)
				{
					return;
				}

				// chances of fertilized egg are equal to mommy's happiness
				eggGo.GameObject.GetComponent<ChickenEgg>().SetFertilizedChance(mood.LevelPercent);
				currentLaidEggs++;
			}
			else
			{
				// grow to become a cute chicken
				Spawn.ServerPrefab(possibleGrownForms.PickRandom(), gameObject.RegisterTile().WorldPosition);
				_ = Despawn.ServerSingle(gameObject);
			}
		}
	}
}
