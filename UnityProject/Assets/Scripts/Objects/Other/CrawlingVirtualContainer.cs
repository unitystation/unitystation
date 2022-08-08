using System;
using System.Linq;
using AddressableReferences;
using Mirror;
using Objects.Atmospherics;
using Systems.Pipes;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Objects.Other
{
	public class CrawlingVirtualContainer : NetworkBehaviour, IExaminable, IEscapable
	{
		[Tooltip("The sound made when someone is trying to move in pipes.")]
		[SerializeField]
		private AddressableAudioSource ClangSound = default;

		private ObjectContainer objectContainer;
		private GasContainer gasContainer;

		private PipeData pipeData;

		// transform.position seems to be the only reliable method after OnDespawnServer() has been called.
		private Vector3 ContainerWorldPosition => transform.position;

		private void Awake()
		{
			objectContainer = GetComponent<ObjectContainer>();
			gasContainer = GetComponent<GasContainer>();
		}

		public void Setup(PipeData newPipeData, RegisterPlayer newPlayer)
		{
			pipeData = newPipeData;

			DoRpc(newPlayer, true);
		}

		/// <summary>
		/// Ejects contents at the virtual container's position with no spin.
		/// </summary>
		public void EjectContents(GameObject entity, GameObject monoPipe)
		{
			objectContainer.RetrieveObjects();
			gasContainer.ReleaseContentsInstantly();
			gasContainer.IsSealed = false;

			DoRpc(entity.GetComponent<RegisterPlayer>(), false);

			if (monoPipe != null)
			{
				Chat.AddActionMsgToChat(entity, $"You exit out of the {monoPipe.ExpensiveName()}",
					$"{entity.ExpensiveName()} exits out of the {monoPipe.ExpensiveName()}!");
			}
			else
			{
				Chat.AddActionMsgToChat(entity, "You emerge out of the floor",
					$"{entity.ExpensiveName()} emerges out of the floor!");
			}

			_ = Despawn.ServerSingle(gameObject);
		}

		public void EntityTryEscape(GameObject entity, Action ifCompleted, MoveAction moveAction)
		{
			if (DMMath.Prob(25))
			{
				SoundManager.PlayNetworkedAtPos(ClangSound, ContainerWorldPosition);
			}

			//The pipe we're in no longer exists
			if (pipeData == null || pipeData.Destroyed)
			{
				EjectContents(entity, null);
				return;
			}

			var connections = pipeData.ConnectedPipes;

			if (connections.Count == 0)
			{
				EjectContents(entity, pipeData.MonoPipe.OrNull()?.gameObject);
				return;
			}

			var localPosContainer = objectContainer.registerTile.LocalPositionServer;
			var direction = PlayerAction.GetMoveDirection(moveAction).To3Int();

			var pipeToMove = PipeFunctions.GetPipeFromDirection(pipeData,
				localPosContainer + direction, PipeFunctions.VectorIntToPipeDirection(direction),
				objectContainer.registerTile.Matrix);

			if(pipeToMove == null) return;

			//Equalise our gasmix on move
			pipeToMove.GetMixAndVolume.EqualiseWithExternal(gasContainer.GasMix);

			objectContainer.registerTile.
				ObjectPhysics.Component
				.AppearAtWorldPositionServer(pipeToMove.MatrixPos.ToWorld(objectContainer.registerTile.Matrix),
				false, false);

			if(pipeToMove.MonoPipe == null) return;
			if(pipeToMove.MonoPipe.GetComponent<Scrubber>() == null && pipeToMove.MonoPipe.GetComponent<AirVent>() == null) return;

			EjectContents(entity, pipeToMove.MonoPipe.gameObject);
		}

		private void DoRpc(RegisterPlayer player, bool newState)
		{
			if(player.connectionToClient == null) return;

			if(CustomNetworkManager.IsServer && CustomNetworkManager.IsHeadless == false)
			{
				//Target RPC not working on local host?
				DoState(newState);
				return;
			}

			RpcChangeState(player.connectionToClient, newState);
		}

		[TargetRpc]
		private void RpcChangeState(NetworkConnection conn, bool newState)
		{
			DoState(newState);
		}


		private void DoState(bool newMode)
		{
			var matrixInfos = MatrixManager.Instance.ActiveMatricesList;

			foreach (var matrixInfo in matrixInfos)
			{
				var tilemapRenderer = matrixInfo.Matrix.PipeLayer.GetComponent<TilemapRenderer>();
				tilemapRenderer.sortingLayerName = newMode ? "Walls" : "UnderFloor";
				tilemapRenderer.sortingOrder = newMode ? 100 : 1;
			}
		}

		public string Examine(Vector3 worldPos = default)
		{
			int contentsCount = objectContainer.GetStoredObjects().Count();
			return $"There {(contentsCount == 1 ? "is one entity" : $"are {contentsCount} entities")} inside.";
		}
	}
}