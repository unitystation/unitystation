using System.Collections;
using UnityEngine;

namespace NPC
{
	/// <summary>
	/// AI brain for mice
	/// used to get hunted by Runtime and squeak
	/// </summary>
	public class CatAI : GenericFriendlyAI
	{
		private bool isLayingDown = false;
		private ConeOfSight coneOfSight;
		private LayerMask mobMask;
		private MobMeleeAttack mobAttack;

		protected override void Awake()
		{
			base.Awake();
			ResetBehaviours();
		}

		public override void OnEnable()
		{
			base.OnEnable();
			mobMask = LayerMask.GetMask("Walls", "NPC");
			coneOfSight = GetComponent<ConeOfSight>();
			mobAttack = GetComponent<MobMeleeAttack>();
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
			var hits = coneOfSight.GetObjectsInSight(mobMask,
				dirSprites.CurrentFacingDirection,
				10f,
				20);

			foreach (Collider2D coll in hits)
			{
				if (coll.gameObject != gameObject && coll.gameObject.GetComponent<MouseAI>() != null
				                                  && !coll.gameObject.GetComponent<LivingHealthBehaviour>().IsDead)
				{
					return coll.gameObject.GetComponent<MouseAI>();
				}
			}
			return null;
		}

		private void HuntMouse(MouseAI mouse)
		{
			//Random chance of going nuts and destroying whatever is in the way
			mobAttack.onlyHitTarget = Random.value != 0.1f;

			Hiss(mouse.gameObject);
			mobAttack.StartFollowing(mouse.transform);
		}

		private void Purr(GameObject purred = null)
		{
			SoundManager.PlayNetworkedAtPos("Purr", gameObject.WorldPosServer(), Random.Range(.8f, 1.2f));

			if (purred != null)
			{
				Chat.AddActionMsgToChat(
					purred,
					$"{mobNameCap} purrs at you!",
					$"{mobNameCap} purrs at {purred.ExpensiveName()}");
			}
			else
			{
				Chat.AddActionMsgToChat(gameObject, $"{mobNameCap} purrs!", $"{mobNameCap} purrs!");
			}
		}

		private void Meow(GameObject meowed = null)
		{
			SoundManager.PlayNetworkedAtPos("Meow#", gameObject.WorldPosServer(), Random.Range(.8f, 1.2f));

			if (meowed != null)
			{
				Chat.AddActionMsgToChat(
					meowed,
					$"{mobNameCap} meows at you!",
					$"{mobNameCap} meows at {meowed.ExpensiveName()}");
			}
			else
			{
				Chat.AddActionMsgToChat(gameObject, $"{mobNameCap} meows!", $"{mobNameCap} meows!");
			}
		}

		private void Hiss(GameObject hissed = null)
		{
			SoundManager.PlayNetworkedAtPos("CatHiss", gameObject.WorldPosServer(), Random.Range(.9f, 1f));

			if (hissed != null)
			{
				Chat.AddActionMsgToChat(
					hissed,
					$"{mobNameCap} hisses at you!",
					$"{mobNameCap} hisses at {hissed.ExpensiveName()}");
			}
			else
			{
				Chat.AddActionMsgToChat(gameObject, $"{mobNameCap} hisses!", $"{mobNameCap} hisses!");
			}
		}

		private void LickPaws()
		{
			Chat.AddActionMsgToChat(
				gameObject,
				$"{mobNameCap} starts licking its paws!",
				$"{mobNameCap} starts licking its paws!");
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
