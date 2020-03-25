using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Magical dog AI brain for corgis!
/// Used for all corgis, remember to set the name of the
/// dog in inspector.
/// *All logic should be server side.
/// </summary>
public class CorgiAI : MobAI
{
	private string dogName;
	private string capDogName;

	//Set this inspector. The corgi will only respond to
	//voice commands from these job types:
	public List<JobType> allowedToGiveCommands = new List<JobType>();

	//TODO: later we can make it so capt or hop can tell the dog to
	//respond to commands from others based on their names

	private float timeForNextRandomAction;
	private float timeWaiting;

	protected override void Awake()
	{
		base.Awake();
		dogName = mobName.ToLower();
		capDogName = char.ToUpper(dogName[0]) + dogName.Substring(1);
	}

	private void SingleBark(GameObject barked = null)
	{
		SoundManager.PlayNetworkedAtPos("Bark", 
										gameObject.transform.position, 
										Random.Range(.8F, 1.3F));

		if (barked != null)
		{
			Chat.AddActionMsgToChat(barked, $"{capDogName} barks at you!", 
									$"{capDogName} barks at {barked.ExpensiveName()}");
		}
		else
		{
			Chat.AddActionMsgToChat(gameObject, $"{capDogName} barks!", $"{capDogName} barks!");
		}		
	}

	IEnumerator RandomBarks()
	{
		int barkAmt = Random.Range(1, 4);
		while (barkAmt > 0) 
		{
			SingleBark();
			yield return WaitFor.Seconds(Random.Range(0.4f, 1f));
			barkAmt--;
		}

		yield break;
	}

	protected override void AIStartServer()
	{
		followingStopped.AddListener(OnFollowingStopped);
		exploringStopped.AddListener(OnExploreStopped);
		fleeingStopped.AddListener(OnFleeStopped);
	}

	public override void LocalChatReceived(ChatEvent chatEvent)
	{
		ProcessLocalChat(chatEvent);
		base.LocalChatReceived(chatEvent);
	}

	void ProcessLocalChat(ChatEvent chatEvent)
	{
		var speaker = PlayerList.Instance.Get(chatEvent.speaker);

		if (speaker.Script == null) return;
		if (speaker.Script.playerNetworkActions == null) return;

		if (speaker.Job == JobType.CAPTAIN || speaker.Job == JobType.HOP)
		{
			StartCoroutine(PerformVoiceCommand(chatEvent.message.ToLower(), speaker));
		}
	}

	IEnumerator PerformVoiceCommand(string msg, ConnectedPlayer speaker)
	{
		//We want these ones to happen right away:
		if (msg.Contains($"{dogName} run") || msg.Contains($"{dogName} get out of here"))
		{
			StartFleeing(speaker.GameObject.transform, 10f);
			yield break;
		}

		if (msg.Contains($"{dogName} stay") || msg.Contains($"{dogName} sit")
		                                    || msg.Contains($"{dogName} stop"))
		{
			ResetBehaviours();
			yield break;
		}

		//Slight delay for the others:
		yield return WaitFor.Seconds(0.5f);

		if (msg.Contains($"{dogName} come") || msg.Contains($"{dogName} follow")
		                                    || msg.Contains($"come {dogName}"))
		{
			if (Random.value > 0.8f)
			{
				yield return StartCoroutine(ChaseTail(1));
			}
			else
			{
				SingleBark();
			}

			FollowTarget(speaker.GameObject.transform);
			yield break;
		}

		if (msg.Contains($"{dogName} find food") || msg.Contains($"{dogName} explore"))
		{
			if (Random.value > 0.8f)
			{
				yield return StartCoroutine(ChaseTail(2));
			}
			else
			{
				SingleBark();
			}

			BeginExploring();
			yield break;
		}
	}

	IEnumerator ChaseTail(int times)
	{
		var timesSpun = 0;
		Chat.AddActionMsgToChat(gameObject, $"{capDogName} start chasing its own tail!", $"{capDogName} start chasing its own tail!");
;

		while (timesSpun <= times)
		{
			for (int spriteDir = 1; spriteDir < 5; spriteDir++)
			{
				dirSprites.DoManualChange(spriteDir);
				yield return WaitFor.Seconds(0.3f);
			}

			timesSpun++;
		}

		StartCoroutine(RandomBarks());

		yield return WaitFor.EndOfFrame;
	}

	//TODO: Do extra stuff on these events, like barking when being told to sit:
	void OnFleeStopped()
	{
		StartCoroutine(RandomBarks());
	}

	void OnExploreStopped()
	{
		StartCoroutine(RandomBarks());
	}

	void OnFollowingStopped()
	{
		StartCoroutine(RandomBarks());
	}

	public override void OnPetted(GameObject performer)
	{
		int randAction = Random.Range(1,6);

		switch (randAction)
		{
			case 1:
				StartCoroutine(ChaseTail(Random.Range(1,3)));
				break;
			case 2:
				RandomBarks();
				break;
			case 3:
				Chat.AddActionMsgToChat(gameObject, $"{capDogName} wags its tail!", $"{capDogName} wags its tail!");
				break;
			case 4:
				Chat.AddActionMsgToChat(performer, $"{capDogName} licks your hand!", 
										$"{capDogName} licks {performer.ExpensiveName()}'s hand!");
				break;
			case 5:
				Chat.AddActionMsgToChat(performer, $"{capDogName} gives you its paw!",
										$"{capDogName} gives his paw to {performer.ExpensiveName()}");
				break;
		}
	}

	protected override void OnAttackReceived(GameObject damagedBy)
	{
		SingleBark();
		StartFleeing(damagedBy.transform);
	}

	//Updates only on the server
	protected override void UpdateMe()
	{
		if (health.IsDead || health.IsCrit || health.IsCardiacArrest) return;

		base.UpdateMe();
		MonitorExtras();
	}

	void MonitorExtras()
	{
		//TODO monitor hunger when it is added

		if (IsPerformingTask) return;

		timeWaiting += Time.deltaTime;
		if (timeWaiting > timeForNextRandomAction)
		{
			timeWaiting = 0f;
			timeForNextRandomAction = Random.Range(15f, 30f);

			DoRandomAction(Random.Range(1, 3));
		}
	}

	void DoRandomAction(int randAction)
	{
		switch (randAction)
		{
			case 1:
				StartCoroutine(ChaseTail(Random.Range(1, 5)));
				break;
			case 2:
				NudgeInDirection(GetNudgeDirFromInt(Random.Range(0, 8)));
				break;
			//case 3 is nothing
		}
	}
}