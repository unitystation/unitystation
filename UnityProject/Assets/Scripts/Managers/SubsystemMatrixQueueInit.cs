using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Logs;
using Shared.Managers;

namespace Managers
{
	public class SubsystemMatrixQueueInit : SingletonManager<SubsystemMatrixQueueInit>
	{
		private List<Matrix> Matrixs = new List<Matrix>();

		//TODO List for non-matrix specific systems

		public static bool InitializedAll = false;

		public override void Awake()
		{
			base.Awake();
			EventManager.AddHandler(Event.RoundEnded, ClearSubsystems);
		}

		private static void ClearSubsystems()
		{
			Instance.Matrixs.Clear();
			InitializedAll = false;
		}

		public static void Queue(Matrix Matrix)
		{
			if (InitializedAll)
			{
				//mid-round scene only
				Matrix.StartCoroutine(Matrix.MatrixInitialization());
				return;
			}

			if (Instance.Matrixs.Contains(Matrix))
			{
				Loggy.LogWarning($"{Matrix.gameObject} has the same exact behavior queued. Skipping");
				return;
			}

			Instance.Matrixs.Add(Matrix);
		}

		public static IEnumerator InitAllSystems()
		{
			int ElapseMilliseconds = 0;
			int Matrixint = 0;
			var watch2 = new Stopwatch();
			watch2.Start();
			Chat.AddGameWideSystemMsgToChat($"<color=#008b8b>Initialising {Instance.Matrixs.Count} Matrices..</color>");
			foreach (var behaviour in Instance.Matrixs)
			{
				yield return behaviour.MatrixInitialization();
				ElapseMilliseconds += watch2.Elapsed.Milliseconds;
				Matrixint++;
				yield return null;
			}
			watch2.Stop();
			Chat.AddGameWideSystemMsgToChat(
				$"<color=green>Matrix Subsystems loaded! Only took {ElapseMilliseconds}ms</color>");
			InitializedAll = true;
		}
	}
}