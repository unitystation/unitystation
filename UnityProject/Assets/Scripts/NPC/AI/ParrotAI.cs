using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Text.RegularExpressions;

/// <summary>
/// AI brain for parrots
/// They say random stuff, repeat what someone says
/// and maybe steal from your active hand
/// </summary>
public class ParrotAI : MobAI
{
	private string parrotName;
	private string capParrotName;
	private float timeForNextRandomAction;
	private float timeWaiting;

	private string lastHeardMsg = "";

	protected override void Awake()
	{
		base.Awake();
		parrotName = mobName.ToLower();
		capParrotName = char.ToUpper(parrotName[0]) + parrotName.Substring(1);
		BeginExploring(MobExplore.Target.players);
	}

	protected override void UpdateMe()
	{
		if (health.IsDead || health.IsCrit || health.IsCardiacArrest) return;

		base.UpdateMe();
		MonitorExtras();
	}

	public override void LocalChatReceived(ChatEvent chatEvent)
	{
		var speaker = PlayerList.Instance.Get(chatEvent.speaker);
		if (speaker.Script == null || speaker.Script.playerNetworkActions == null)
		{
			return;
		}

		lastHeardMsg = chatEvent.message;
	}

	void MonitorExtras()
	{
		if (IsPerformingTask) return;

		timeWaiting += Time.deltaTime;
		if (timeWaiting > timeForNextRandomAction)
		{
			timeWaiting = 0f;
			timeForNextRandomAction = Random.Range(1f, 30f);

			DoRandomAction();
		}
	}

	public override void OnPetted(GameObject performer)
	{
		ParrotSounds();
	}

	protected override void OnAttackReceived(GameObject damagedBy)
	{
		StartFleeing(damagedBy, 5f);
	}

	// Steals shit from your active hand
	public override void ExplorePeople(PlayerScript player)
	{
		if (player.IsGhost) return;
		var inventory = player.GetComponent<ItemStorage>();
		var thingInHand = inventory.GetActiveHandSlot();

		if (thingInHand != null && thingInHand.Item != null)
		{
			GameObject stolenThing = thingInHand.Item.gameObject;
			Inventory.ServerDespawn(thingInHand);
			StartCoroutine(FleeAndDrop(player.gameObject, stolenThing));
		}
	}

	IEnumerator FleeAndDrop(GameObject dude, GameObject stolenThing)
	{
		StartFleeing(dude, 3f);
		yield return WaitFor.Seconds(3f);
		Spawn.ServerPrefab(stolenThing, gameObject.WorldPosServer());
		StartFleeing(dude, 5f);
		yield break;
	}

	private void Speak(string text)
	{
		//TODO use the actual chat api when it allows it!
		Chat.AddLocalMsgToChat(
			$"<b>{capParrotName} says</b>, \"{text}\"",
			gameObject.transform.position,
			gameObject);
		ChatBubbleManager.ShowAChatBubble(gameObject.transform, text);
	}
	private void SayRandomThing()
	{
		// 50% chances of saying something
		if (Random.value > .5f)
		{
			ParrotSounds();
			return;
		}

		if (Random.value < 0.3f && (lastHeardMsg != ""))
		{
			Speak(lastHeardMsg);
		}
		else
		{
			Speak(polyPhrases[Random.Range(0, polyPhrases.Length)]);
		}
	}

	private void ParrotSounds()
	{
		//TODO add parrot sounds!
		string[]  _sounds = {"squawks", "screeches"};
		Chat.AddActionMsgToChat(
			gameObject,
			$"{capParrotName} {_sounds[Random.Range(0,2)]}!",
			$"{capParrotName} {_sounds[Random.Range(0,2)]}!");
	}

	private void DoRandomAction()
	{
		int randAction = Random.Range(1, 3);
		switch (randAction)
		{
			case 1:
				BeginExploring(MobExplore.Target.players, 3f);
				break;
			case 2:
				SayRandomThing();
				break;
		}
	}

	private readonly string[] polyPhrases =
	{
		"Who wired the SMES!",
		"Has anyone seen the Nuke disc?",
		"AA at HOPs",
		"Nukie spotted!",
		"Captain is a condom!",
		"Help ling absorbing me in maint!",
		"The AI is rogue!",
		"Someone is messing with the generator!",
		"Alright, planting bomb",
		"OH, NO! We are out of plasma!",
		"Someone is breaking into the CE's office!",
		"Xenos spotted in engineering",
		"Why is this APC blue?",
		"Plasma flood in engineering!",
		"Why am I glowing?",
		"Who is outside engineering in a red hardsuit?",
		"FIRE! EVERYTHING IS ON FIRE!",
		"Does anyone else want to kill the Captain really badly for some reason?",
		"He is breaking in from maint!",
		"Someone in gravity room",
		"There's a pizza box with a weird robot spider in it",
		"Shitsec!",
		"Sec to engineering!",
		"Engineering to security!"
	};
}