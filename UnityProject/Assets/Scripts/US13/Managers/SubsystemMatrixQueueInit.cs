using System.Threading;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Logs;
using Shared.Managers;
using US13.Core.Chat;
using US13.Tilemaps.Behaviours.Layers;

namespace US13.Managers
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


			if (Instance.Matrixs.Contains(Matrix))
			{
				Loggy.Warning($"{Matrix.gameObject} has the same exact behavior queued. Skipping");
				return;
			}

			Instance.Matrixs.Add(Matrix);

			if (InitializedAll)
			{
				if (Matrix.NetworkedMatrix.IsJsonLoaded == false)
				{
					//mid-round scene only
					Matrix.MatrixInitialization(Matrix.GetCancellationTokenOnDestroy()).Forget(e => Logs.Loggy.Error(e.ToString()));
					return;
				}
				else
				{
					Matrix.NetworkedMatrix.RequestInitialiseMapLoader = true;
					return;
				}
			}


		}

		public static async UniTask InitAllSystems(CancellationToken cancellationToken = default)
		{
			int ElapseMilliseconds = 0;
			int Matrixint = 0;
			var watch2 = new Stopwatch();
			watch2.Start();
			Chat.AddGameWideSystemMsgToChat($"<color=#008b8b>Initialising {Instance.Matrixs.Count} Matrices..</color>");
			var progress = new SubSceneManager.SubsceneLoadTimer();
			foreach (var behaviour in Instance.Matrixs)
			{
				cancellationToken.ThrowIfCancellationRequested();
				progress.Report("Matrices initialized", behaviour.name, Matrixint, Instance.Matrixs.Count);
				await behaviour.MatrixInitialization(cancellationToken);
				ElapseMilliseconds += watch2.Elapsed.Milliseconds;
				Matrixint++;
				progress.Report("Matrices initialized", behaviour.name, Matrixint, Instance.Matrixs.Count);
				await UniTask.NextFrame(cancellationToken: cancellationToken);
			}
			watch2.Stop();
			Chat.AddGameWideSystemMsgToChat(
				$"<color=green>Matrix Subsystems loaded! Only took {ElapseMilliseconds}ms</color>");
			InitializedAll = true;
		}
	}
}
