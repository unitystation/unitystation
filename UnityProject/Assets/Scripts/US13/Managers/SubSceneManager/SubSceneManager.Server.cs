using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Logs;
using Mirror;
using UnityEngine.SceneManagement;
using US13.Messages.Server;
using US13.Tilemaps.Behaviours.Layers;

//Server
namespace US13.Managers.SubSceneManager
{
	public partial class SubSceneManager
	{
		/// <summary>
		/// Starts a collection of scenes that this connection is allowed to see
		/// </summary>
		public void AddNewObserverScenePermissions(NetworkConnectionToClient conn)
		{
			if (NetworkServer.observerSceneList.ContainsKey(conn))
			{
				RemoveSceneObserver(conn);
			}

			NetworkServer.observerSceneList.Add(conn, new List<Scene> {SceneManager.GetActiveScene()});
		}

		public void RemoveSceneObserver(NetworkConnectionToClient conn)
		{
			NetworkServer.observerSceneList.Remove(conn);
		}

		/// <summary>
		/// No scene / proximity visibility checking. Just adding it to everything
		/// </summary>
		/// <param name="connToAdd"></param>
		void AddObserverToAllObjects(NetworkConnectionToClient connToAdd, Scene sceneContext)
		{
			SyncPlayerData(connToAdd, sceneContext).Forget(e => Logs.Loggy.Error(e.ToString()));
			AddObservableSceneToConnection(connToAdd, sceneContext);
		}

		/// Sync init data with specific scenes
		/// staggered over multiple frames
		public async UniTask SyncPlayerData(NetworkConnectionToClient connToAdd, Scene sceneContext)
		{
			var cancellationToken = LoadingToken;
			cancellationToken.ThrowIfCancellationRequested();

			var client = connToAdd.owned.Count == 0 ? null : connToAdd.owned.ElementAt(0).gameObject;

			Loggy.Info().Format("SyncPlayerData. This server sending a bunch of sync data to new " +
			                    "client {0} for scene {1}", Category.Connections, client, sceneContext.name);

			//Add connection as observer to the scene objects:
			await AddObserversForClient(connToAdd, sceneContext);
		}

		/// <summary>
		/// Add a connection as an observer to everything in a scene
		/// </summary>
		async UniTask AddObserversForClient(NetworkConnectionToClient connToAdd, Scene sceneContext)
		{
			var cancellationToken = LoadingToken;
			cancellationToken.ThrowIfCancellationRequested();
			//Activate the matrices on the client first
			foreach (var matrixInfo in MatrixManager.MatrixManager.Instance.ActiveMatrices.Values)
			{
				if (matrixInfo.Matrix.gameObject.scene == sceneContext)
				{
					matrixInfo.Matrix.GetComponentInParent<NetworkedMatrix>().MatrixSync.netIdentity.AddPlayerObserver(connToAdd);
				}
			}

			await UniTask.NextFrame(cancellationToken: cancellationToken);

			var Stopwatch = new Stopwatch();

			var objCount = 0;
			var netIds = NetworkServer.spawned.Values.ToList();
			Stopwatch.Start();
			foreach (var n in netIds)
			{
				if (n == null) continue;

				if (n.gameObject.scene != sceneContext)
					continue;

				if (connToAdd.identity == null) //connection dc midway, we're out
					return;

				n.AddPlayerObserver(connToAdd);

				if (Stopwatch.ElapsedMilliseconds >= 10)
				{
					Stopwatch.Reset();
					await UniTask.NextFrame(cancellationToken: cancellationToken);
					Stopwatch.Start();
				}
			}

			await UniTask.NextFrame(cancellationToken: cancellationToken);
			FinishedAddedObserverMessage.Send(connToAdd , sceneContext.name);
		}
	}
}
