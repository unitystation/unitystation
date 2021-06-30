using System.Collections;
using Messages.Server;
using UnityEngine;
using WebSocketSharp;

namespace Systems.MobAIs
{
	/// <summary>
	/// AI brain for parrots
	/// They say random stuff, repeat what someone says
	/// and maybe steal from your active hand
	/// </summary>
	public class ParrotAI : GenericFriendlyAI
	{

		private string lastHeardMsg;

		protected override void Awake()
		{
			base.Awake();
			ResetBehaviours();
		}

		public override void LocalChatReceived(ChatEvent chatEvent)
		{
			// check who said the message
			var originator = chatEvent.originator;
			if (originator == gameObject)
			{
				// parrot should ignore its own speech
				return;

			}

			// parrot should listen only speech and ignore different action/examine/combat messages
			var channels = chatEvent.channels;
			if (!Chat.NonSpeechChannels.HasFlag(channels))
			{
				lastHeardMsg = chatEvent.message;
			}
		}

		public override void OnPetted(GameObject performer)
		{
			ParrotSounds();
		}

		// Steals shit from your active hand
		public override void ExplorePeople(PlayerScript player)
		{
			if (player.IsGhost) return;
			var inventory = player.GetComponent<DynamicItemStorage>();
			var thingInHand = inventory.GetActiveHandSlot();

			if (thingInHand != null && thingInHand.Item != null)
			{
				GameObject stolenThing = thingInHand.Item.gameObject;
				Inventory.ServerDespawn(thingInHand);
				StartCoroutine(FleeAndDrop(player.gameObject, stolenThing));
			}
		}

		private IEnumerator FleeAndDrop(GameObject dude, GameObject stolenThing)
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
				text,
				gameObject,
				MobName);
			ShowChatBubbleMessage.SendToNearby(gameObject, text);
		}
		private void SayRandomThing()
		{
			// 50% chances of saying something
			if (Random.value > .5f)
			{
				ParrotSounds();
				return;
			}

			if (Random.value < 0.3f && (!lastHeardMsg.IsNullOrEmpty()))
			{
				Speak(lastHeardMsg);
			}
			else
			{
				Speak(polyPhrases.PickRandom());
			}
		}

		private void ParrotSounds()
		{
			//TODO add parrot sounds!
			string[]  _sounds = {"squawks", "screeches"};
			Chat.AddActionMsgToChat(
				gameObject,
				$"{MobName} {_sounds.PickRandom()}!",
				$"{MobName} {_sounds.PickRandom()}!");
		}

		protected override void DoRandomAction()
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
}