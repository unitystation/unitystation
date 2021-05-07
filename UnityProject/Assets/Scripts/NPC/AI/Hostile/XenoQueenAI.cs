using System;
using System.Collections;
using UnityEngine;

namespace Systems.MobAIs
{
	public class XenoQueenAI: GenericHostileAI
	{
		[SerializeField][Tooltip("If true, this Queen won't be counted when spawned for the queen cap.")]
		private bool ignoreForQueenCount = false;
		[SerializeField]
		[Tooltip("Max amount of active facehuggers that can be in the server at once")]
		private int huggerCap = 0;

		[SerializeField]
		[Range(0, 100)]
		[Tooltip("Chances of laying an egg. This gets rolled every time the Queen has no cd")]
		private int fertility = 30;

		[SerializeField]
		[Tooltip("Time in seconds between each roll for laying eggs")]
		private float eggCooldown = 400;

		[SerializeField]
		[Tooltip("Alien egg reference so we can spawn")]
		private GameObject alienEgg = null;

		private static int currentHuggerAmt;
		private static int currentQueensAmt;
		private static bool resetHandlerAdded = false;

		public static int CurrentQueensAmt => currentQueensAmt;

		protected override void OnSpawnMob()
		{
			base.OnSpawnMob();
			AddResetHandler();

			if (ignoreForQueenCount == false)
			{
				currentQueensAmt++;
			}
		}

		protected override void OnAIStart()
		{
			base.OnAIStart();

			if (fertility != 0)
			{
				FertilityLoop();
			}
		}

		private bool HuggerCapReached()
		{
			if (huggerCap < 0)
			{
				return false;
			}

			return currentHuggerAmt >= huggerCap;
		}

		private void FertilityLoop()
		{
			if (HuggerCapReached())
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

		public static void AddFacehuggerToCount()
		{
			currentHuggerAmt ++;
		}

		public static void RemoveFacehuggerFromCount()
		{
			currentHuggerAmt --;
		}

		private void LayEgg()
		{
			Spawn.ServerPrefab(alienEgg, gameObject.transform.position);
		}

		private static void ResetStaticCounters()
		{
			currentHuggerAmt = 0;
			currentQueensAmt = 0;
		}

		private static void AddResetHandler()
		{
			if (resetHandlerAdded)
			{
				return;
			}

			EventManager.AddHandler(Event.RoundStarted, ResetStaticCounters);
			resetHandlerAdded = true;
		}

		protected override void HandleDeathOrUnconscious()
		{
			base.HandleDeathOrUnconscious();
			if (ignoreForQueenCount == false)
			{
				currentQueensAmt--;
			}
		}
	}
}
