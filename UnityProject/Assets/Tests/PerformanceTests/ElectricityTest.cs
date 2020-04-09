using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    class ElectricityTest : PlayModePerformanceTest
	{
		protected override string Scene => "Lobby";

		protected override SampleGroupDefinition[] SampleGroupDefinitions => sampleGroupDefinitions;

		private readonly SampleGroupDefinition[] sampleGroupDefinitions =
			new[]
			{
				new SampleGroupDefinition(ElectricalSynchronisation.updateName)
			};

		PlayerSync player;

		[UnityTest, Performance]
        public IEnumerator ElectricityGeneratorTest()
		{
			yield return LoadSceneAndSetActive();
			yield return ClickButton("LoginButton");
			yield return DoActionWaitSceneLoad(ClickButton("StartGameButton"));
			yield return ClickButton("Nanotrasen");
			yield return ClickButton(JobType.CHIEF_ENGINEER);

			yield return Settle();

			player = PlayerManager.LocalPlayer.GetComponent<PlayerSync>();
			yield return Move(10, MoveAction.MoveUp);
			yield return Settle();
			yield return Move(3, MoveAction.MoveUp);

			yield return Settle();
			yield return UpdateBenchmark(300);

			GUI_IngameMenu.Instance.isTest = true;
			GUI_IngameMenu.Instance.OpenMenuPanel();
			yield return ClickButton("ExitButton");
			yield return DoActionWaitSceneUnload(ClickButton("Button1"));
		}

		protected override IEnumerator CustomUpdateBenchmark(int sampleCount)
		{
			int disableGeneratorPoint = sampleCount / 3;
			int enableGeneratorPoint = sampleCount * 2 / 3;
			yield return new WaitWhile(LoopFunction);

			bool LoopFunction()
			{
				sampleCount--;
				if(sampleCount == disableGeneratorPoint || sampleCount == enableGeneratorPoint)
				{
					var generator = GetAtRelative<PowerGenerator>(Orientation.Right);
					InteractionUtils.RequestInteract(HandApply.ByLocalPlayer(generator.gameObject), generator);
				}
				return sampleCount > 0;
			}
		}

		T GetAtRelative<T>(Orientation orientation) where T : MonoBehaviour
		{
			return MatrixManager.GetAt<T>(player.ClientPosition + Vector3Int.FloorToInt(orientation.Vector), true).First();
		}

		IEnumerator Move(int repeat, params MoveAction[] moves)
		{
			for (int i = 0; i < repeat; i++)
			{
				yield return new WaitUntil(() => TryMove(moves));
			}
		}

		IEnumerator Move(params MoveAction[] moves)
		{
			return new WaitUntil(() => TryMove(moves));
		}

		bool TryMove(params MoveAction[] moves)
		{
			int[] intMoves = Array.ConvertAll(moves, value => (int)value);
			return player.DoAction(new PlayerAction() { moveActions = intMoves});
		}
    }
}
