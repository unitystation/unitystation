using UnityEngine;
using System.Collections;
using System.Linq;

/// <summary>
/// AI brain for mice
/// used to get hunted by Runtime and squeak
/// </summary>
public class CatAI : MobAI
{
	public float mouseDmg = 70f;
	private string catName;
	private string capCatName;
	private float timeForNextRandomAction;
	private float timeWaiting;
	private bool isLayingDown = false;
	private ConeOfSight coneOfSight;
	private LayerMask mobMask;
	private MobMeleeAttack mobAttack;

	protected override void Awake()
	{
		base.Awake();
		catName = mobName.ToLower();
		capCatName = char.ToUpper(catName[0]) + catName.Substring(1);
	}

	public override void OnEnable()
	{
		base.OnEnable();
		mobMask = LayerMask.GetMask("Walls", "NPC");
		coneOfSight = GetComponent<ConeOfSight>();
		mobAttack = GetComponent<MobMeleeAttack>();
	}

	protected override void AIStartServer()
	{
		followingStopped.AddListener(OnFollowingStopped);
	}

	protected override void UpdateMe()
	{
		if (health.IsDead || health.IsCrit || health.IsCardiacArrest) return;

		base.UpdateMe();
		MonitorExtras();
	}
	
	void MonitorExtras()
	{
		if (IsPerformingTask || isLayingDown) return;

		timeWaiting += Time.deltaTime;
		if (timeWaiting > timeForNextRandomAction)
		{
			timeWaiting = 0f;
			timeForNextRandomAction = Random.Range(8f,30f);

			DoRandomAction();
		}
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
				StartFleeing(performer.transform, 5f);
				break;
			// case 5:
			// 	StartCoroutine(LayDown(Random.Range(10,15)));//TODO
			// 	break;
		}
	}

	protected override void OnAttackReceived(GameObject damagedBy)
	{
		Hiss(damagedBy);
		FleeFromAttacker(damagedBy, 10F);
	}

	void OnFollowingStopped()
	{
		DoRandomAction();
	}

	MouseAI AnyMiceNearby()
	{
		var hits = coneOfSight.GetObjectsInSight(mobMask, dirSprites.CurrentFacingDirection, 10f, 20);
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
		if (Random.value == 0.0f)
		{
			mobAttack.onlyHitTarget = false;
		}
		else
		{
			mobAttack.onlyHitTarget = true;
		}

		Hiss(mouse.gameObject);
		mobAttack.StartFollowing(mouse.transform);
	}

	IEnumerator ChaseTail(int times)
	{
		var timesSpun = 0;
		Chat.AddActionMsgToChat(
			gameObject,
			$"{capCatName} start chasing its own tail!",
			$"{capCatName} start chasing its own tail!");

		while (timesSpun <= times)
		{
			for (int spriteDir = 1; spriteDir < 5; spriteDir++)
			{
				dirSprites.DoManualChange(spriteDir);
				yield return WaitFor.Seconds(0.3f);
			}

			timesSpun++;
		}

		yield return WaitFor.EndOfFrame;
	}

	private void Purr(GameObject purred = null)
	{
		SoundManager.PlayNetworkedAtPos("Purr", gameObject.WorldPosServer(), Random.Range(.8f, 1.2f));

		if (purred != null)
		{
			Chat.AddActionMsgToChat(
				purred,
				$"{capCatName} purrs at you!", 
				$"{capCatName} purrs at {purred.ExpensiveName()}");
		}
		else
		{
			Chat.AddActionMsgToChat(gameObject, $"{capCatName} purrs!", $"{capCatName} purrs!");
		}
	}

	private void Meow(GameObject meowed = null)
	{
		SoundManager.PlayNetworkedAtPos("Meow#", gameObject.WorldPosServer(), Random.Range(.8f, 1.2f));
		
		if (meowed != null)
		{
			Chat.AddActionMsgToChat(
				meowed,
				$"{capCatName} meows at you!",
				$"{capCatName} meows at {meowed.ExpensiveName()}");
		}
		else
		{
			Chat.AddActionMsgToChat(gameObject, $"{capCatName} meows!", $"{capCatName} meows!");
		}
	}

	private void Hiss(GameObject hissed = null)
	{
		SoundManager.PlayNetworkedAtPos("CatHiss", gameObject.WorldPosServer(), Random.Range(.9f, 1f));
		
		if (hissed != null)
		{
			Chat.AddActionMsgToChat(
				hissed,
				$"{capCatName} hisses at you!", 
				$"{capCatName} hisses at {hissed.ExpensiveName()}");
		}
		else
		{
			Chat.AddActionMsgToChat(gameObject, $"{capCatName} hisses!", $"{capCatName} hisses!");
		}
	}

	private void LickPaws()
	{
		Chat.AddActionMsgToChat(
			gameObject,
			$"{capCatName} start licking its paws!",
			$"{capCatName} start licking its paws!");
	}

	// Public method so it can be called from CorgiAI
	public void RunFromDog(Transform dog)
	{
		Hiss(dog.gameObject);
		StartFleeing(dog, 10f);
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

	private void DoRandomAction()
	{
		// Before doing anything, try to hunt mouse!
		var posibbleMouse = AnyMiceNearby();
		if (posibbleMouse != null)
		{

			HuntMouse(posibbleMouse);
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