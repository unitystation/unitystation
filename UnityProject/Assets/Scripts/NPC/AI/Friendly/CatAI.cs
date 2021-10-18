using System.Collections;
using UnityEngine;
using AddressableReferences;
using Messages.Server.SoundMessages;


namespace Systems.MobAIs
{
	/// <summary>
	/// AI brain for mice
	/// used to get hunted by Runtime and squeak
	/// </summary>
	public class CatAI : GenericFriendlyAI
	{
		[SerializeField] private AddressableAudioSource PurrSFX = null;

		[SerializeField] private AddressableAudioSource MeowSFX = null;

		[SerializeField] private AddressableAudioSource CatHissSFX = null;

		private bool isLayingDown = false;
		private ConeOfSight coneOfSight;
		private LayerMask mobMask;
		private MobMeleeAttack mobAttack;

		protected override void Awake()
		{
			mobMask = LayerMask.GetMask( "NPC");
			coneOfSight = GetComponent<ConeOfSight>();
			mobAttack = GetComponent<MobMeleeAttack>();
			base.Awake();
			ResetBehaviours();
		}

		protected override void ResetBehaviours()
		{
			base.ResetBehaviours();
			if (isLayingDown) StopLayingDown();
		}

		public override void OnPetted(GameObject performer)
		{
			base.OnPetted(performer);
			int randAction = Random.Range(1,5);
			switch (randAction)
			{
				case 1:
					Purr(performer);
					break;
				case 2:
					Meow(performer);
					break;
				case 3:
					StartCoroutine(ChaseTail(Random.Range(1,5)));
					break;
				case 4:
					StartFleeing(performer, 5f);
					break;
				// case 5:
				// 	StartCoroutine(LayDown(Random.Range(10,15)));//TODO
				// 	break;
			}
		}

		protected override void OnAttackReceived(GameObject damagedBy)
		{
			Hiss(damagedBy);
			StartFleeing(damagedBy, 10F);
		}

		protected override void OnFollowStopped()
		{
			DoRandomAction();
		}

		private MouseAI AnyMiceNearby()
		{
			var hits = coneOfSight.GetObjectsInSight(mobMask, LayerTypeSelection.Walls,
				directional.CurrentDirection.Vector,
				10f);

			foreach (var coll in hits)
			{
				if (coll == null) continue;

				if (coll != gameObject && coll.GetComponent<MouseAI>() != null
				                                  && !coll.GetComponent<LivingHealthBehaviour>().IsDead)
				{
					return coll.GetComponent<MouseAI>();
				}
			}
			return null;
		}

		private void HuntMouse(MouseAI mouse)
		{
			//Random chance of going nuts and destroying whatever is in the way
			mobAttack.onlyActOnTarget = Random.value != 0.1f;

			Hiss(mouse.gameObject);
			mobAttack.StartFollowing(mouse.gameObject);
		}

		private void Purr(GameObject purred = null)
		{
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(.8f, 1.2f));
			SoundManager.PlayNetworkedAtPos(PurrSFX, gameObject.WorldPosServer(), audioSourceParameters);

			if (purred != null)
			{
				Chat.AddActionMsgToChat(
					purred,
					$"{MobName} purrs at you!",
					$"{MobName} purrs at {purred.ExpensiveName()}");
			}
			else
			{
				Chat.AddActionMsgToChat(gameObject, $"{MobName} purrs!", $"{MobName} purrs!");
			}
		}

		private void Meow(GameObject meowed = null)
		{
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(.8f, 1.2f));
			SoundManager.PlayNetworkedAtPos(MeowSFX, gameObject.WorldPosServer(), audioSourceParameters);

			if (meowed != null)
			{
				Chat.AddActionMsgToChat(
					meowed,
					$"{MobName} meows at you!",
					$"{MobName} meows at {meowed.ExpensiveName()}");
			}
			else
			{
				Chat.AddActionMsgToChat(gameObject, $"{MobName} meows!", $"{MobName} meows!");
			}
		}

		private void Hiss(GameObject hissed = null)
		{
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(.9f, 1f));
			SoundManager.PlayNetworkedAtPos(CatHissSFX, gameObject.WorldPosServer(), audioSourceParameters);

			if (hissed != null)
			{
				Chat.AddActionMsgToChat(
					hissed,
					$"{MobName} hisses at you!",
					$"{MobName} hisses at {hissed.ExpensiveName()}");
			}
			else
			{
				Chat.AddActionMsgToChat(gameObject, $"{MobName} hisses!", $"{MobName} hisses!");
			}
		}

		private void LickPaws()
		{
			Chat.AddActionMsgToChat(
				gameObject,
				$"{MobName} starts licking its paws!",
				$"{MobName} starts licking its paws!");
		}

		// Public method so it can be called from CorgiAI
		public void RunFromDog(Transform dog)
		{
			Hiss(dog.gameObject);
			StartFleeing(dog.gameObject, 10f);
		}

		IEnumerator LayDown(int cycles)
		{
			isLayingDown = true;
			//TODO animate layingdown and wagging tail

			StopLayingDown();
			yield break;
		}

		private void StopLayingDown()
		{
			isLayingDown = false;
		}

		protected override void DoRandomAction()
		{
			// Before doing anything, try to hunt mouse!
			var possibleMouse = AnyMiceNearby();
			if (possibleMouse != null)
			{

				HuntMouse(possibleMouse);
				return;
			}

			int randAction = Random.Range(1,6);

			switch (randAction)
			{
				case 1:
					Purr();
					break;
				case 2:
					Meow();
					break;
				case 3:
					BeginExploring(MobExplore.Target.food, 3f);
					break;
				case 4:
					LickPaws();
					break;
				case 5:
					StartCoroutine(ChaseTail(Random.Range(1,4)));
					break;
				// case 6:
				//	 StartCoroutine(LayDown(1));//TODO
				//	 break;
			}
		}
	}
}
