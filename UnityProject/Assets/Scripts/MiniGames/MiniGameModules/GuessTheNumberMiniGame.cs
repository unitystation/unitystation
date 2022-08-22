using System;
using System.Collections;
using Mirror;
using Player.Language;
using Systems.Communications;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MiniGames.MiniGameModules
{
	public class GuessTheNumberMiniGame : MiniGameModule, IChatInfluencer
	{
		private int randomNumber = -1;
		private bool miniGameActive = false;

		private sequenceStage stage;
		private string nameGiven = "";

		private LanguageSO encodedLang;

		private const int SHORT_NAME_LENGTH = 16;


		private enum sequenceStage
		{
			SPEAKING,
			IDENTIFY,
			PASSCODE
		}

		public override void Setup(MiniGameResultTracker tracker, GameObject parent)
		{
			base.Setup(tracker, parent);
			randomNumber = Random.Range(1000, 9999); //Number will only exist on the server because only the server calls Setup()
		}

		public override void StartMiniGame()
		{
			base.StartMiniGame();
			if(miniGameActive) return;
			StartCoroutine(IdentifySequence());
			miniGameActive = true;
		}

		protected override void OnGameDone(bool t)
		{
			Chat.AddLocalMsgToChat("The lock-pad lights powers down.", MiniGameParent);
			miniGameActive = false;
		}

		private IEnumerator IdentifySequence()
		{
			stage = sequenceStage.SPEAKING;
			Chat.AddLocalMsgToChat($"An advance looking lock-pad lights up on the {MiniGameParent.ExpensiveName()} before a static and muffled robotic voice loudly starts making fake alarm noises.", MiniGameParent);
			yield return WaitFor.Seconds(1.5f);
			Chat.AddLocalMsgToChat($"{MiniGameParent.ExpensiveName()} loudly states 'IDENTIFY YOURSELF'", MiniGameParent);
			stage = sequenceStage.IDENTIFY;
			yield return WaitFor.Seconds(20f);
			if (stage != sequenceStage.IDENTIFY) yield break;
			Tracker.OnGameDone.Invoke(false);
		}

		private IEnumerator PasscodeSequence()
		{
			stage = sequenceStage.SPEAKING;
			Chat.AddLocalMsgToChat($"{MiniGameParent.ExpensiveName()} loudly states '{nameGiven} DOES NOT HAVE A REGISTERED FINGER-PRINT ID NOR VOICE. STATE THE FOUR DIGIT PASSCODE.'", MiniGameParent);
			yield return WaitFor.Seconds(20f);
			if (stage != sequenceStage.IDENTIFY) yield break;
			Tracker.OnGameDone.Invoke(false);
		}

		private void IdentityChecks(ChatEvent chatEvent)
		{
			if (chatEvent.language == encodedLang)
			{
				Chat.AddLocalMsgToChat("The lock-pad makes a static voice before opening up.", MiniGameParent);
				Chat.AddExamineMsg(chatEvent.originator, ".. Take what you need, brother ..");
				Tracker.OnGameDone.Invoke(true);
				return;
			}
			if (chatEvent.message.Length > SHORT_NAME_LENGTH)
			{
				Chat.AddLocalMsgToChat($"{Tracker.gameObject.ExpensiveName()} loudly states 'I DO NOT WANT TO HEAR YOUR FULL LEGAL NAME.' before shutting off.", MiniGameParent);
				Tracker.OnGameDone.Invoke(false);
				return;
			}
			nameGiven = chatEvent.message;
			StopCoroutine(IdentifySequence());
			StartCoroutine(PasscodeSequence());
		}

		private void PassCodeCheck(ChatEvent chatEvent)
		{
			if (chatEvent.message.Length > 4)
			{
				Chat.AddLocalMsgToChat($"{MiniGameParent.ExpensiveName()} loudly states 'THE PASSCODE IS FOUR DIGITS ONLY.'", MiniGameParent);
				return;
			}

			if (int.TryParse(chatEvent.message, out var result))
			{
				if (result == randomNumber)
				{
					Tracker.OnGameDone.Invoke(true);
					return;
				}
			}
			Chat.AddLocalMsgToChat($"{MiniGameParent.ExpensiveName()} loudly states 'INCORRECT PASSCODE.'", MiniGameParent);
		}


		private void AnalyzeSpeech(ChatEvent chatEvent)
		{
			switch (stage)
			{
				case sequenceStage.IDENTIFY:
					IdentityChecks(chatEvent);
					break;
				case sequenceStage.PASSCODE:
					PassCodeCheck(chatEvent);
					break;
				case sequenceStage.SPEAKING:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public bool WillInfluenceChat()
		{
			Debug.Log(miniGameActive);
			return miniGameActive;
		}

		public ChatEvent InfluenceChat(ChatEvent chatToManipulate)
		{
			AnalyzeSpeech(chatToManipulate);
			return chatToManipulate;
		}
	}
}