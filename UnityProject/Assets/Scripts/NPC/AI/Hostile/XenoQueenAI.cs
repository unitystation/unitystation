using System;
using System.Collections;
using UnityEngine;

namespace Systems.MobAIs
{
	public class XenoQueenAI: GenericHostileAI
	{
		[Tooltip("Max amount of active facehuggers that can be in the server at once")][SerializeField]
		private int huggerCap = 0;

		[Tooltip("Chances of laying an egg. This gets rolled every time the Queen has no cd")]
		[SerializeField]
		[Range(0, 100)]
		private int fertility = 30;

		[Tooltip("Time in seconds between each roll for laying eggs")] [SerializeField]
		private float eggCooldown = 400;

		[Tooltip("Alien egg reference so we can spawn")][SerializeField]
		private GameObject alienEgg = null;

		private static int currentHuggerAmt;
		private static bool resetHandlerAdded = false;

		public static int CurrentHuggerAmt
		{
			get => currentHuggerAmt;
			set => currentHuggerAmt = value;
		}


		private bool CapReached()
		{
			if (currentHuggerAmt < 0)
			{
				return false;
			}

			return currentHuggerAmt >= huggerCap;
		}

		private void FertilityLoop()
		{
			if (CapReached())
			{
				StartCoroutine(Cooldown());
				return;
			}

			if (DMMath.Prob(fertility))
			{
				LayEgg();
			}

			StartCoroutine(Cooldown());
		}

		private IEnumerator Cooldown()
		{
			yield return WaitFor.Seconds(eggCooldown);
			FertilityLoop();
		}

		private void LayEgg()
		{
			Spawn.ServerPrefab(alienEgg, gameObject.transform.position);
		}

		private static void ResetHuggerAmount()
		{
			currentHuggerAmt = 0;
		}

		private static void AddResetHandler()
		{
			if (resetHandlerAdded)
			{
				return;
			}

			EventManager.AddHandler(EVENT.RoundStarted, ResetHuggerAmount);
			resetHandlerAdded = true;
		}

		protected override void OnSpawnMob()
		{
			base.OnSpawnMob();
			AddResetHandler();

			if (fertility != 0)
			{
				FertilityLoop();
			}

			BeginSearch();
		}
	}
}