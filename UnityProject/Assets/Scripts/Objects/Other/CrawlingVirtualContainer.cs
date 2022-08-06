using System;
using System.Linq;
using AddressableReferences;
using Objects.Atmospherics;
using Systems.Pipes;
using UnityEngine;

namespace Objects.Other
{
	public class CrawlingVirtualContainer : MonoBehaviour, IExaminable, IEscapable
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

		public void Setup(PipeData newPipeData)
		{
			pipeData = newPipeData;
		}

		/// <summary>
		/// Ejects contents at the virtual container's position with no spin.
		/// </summary>
		public void EjectContents()
		{
			objectContainer.RetrieveObjects();
			gasContainer.ReleaseContentsInstantly();
			gasContainer.IsSealed = false;

			_ = Despawn.ServerSingle(gameObject);
		}

		public void EntityTryEscape(GameObject entity, Action ifCompleted, MoveAction moveAction)
		{
			SoundManager.PlayNetworkedAtPos(ClangSound, ContainerWorldPosition);

			if (pipeData == null)
			{
				EjectContents();
				return;
			}

			var connections = pipeData.ConnectedPipes;

			if (connections.Count == 0)
			{
				EjectContents();
				return;
			}

			var localPosContainer = objectContainer.registerTile.LocalPositionServer;
			var direction = PlayerAction.GetMoveDirection(moveAction).To3Int();

			var pipeToMove = PipeFunctions.GetPipeFromDirection(pipeData,
				localPosContainer + direction, PipeFunctions.VectorIntToPipeDirection(direction),
				objectContainer.registerTile.Matrix);

			if(pipeToMove == null) return;

			objectContainer.registerTile.
				ObjectPhysics.Component
				.AppearAtWorldPositionServer(pipeToMove.MatrixPos.ToWorld(objectContainer.registerTile.Matrix),
				false, false);
		}

		public string Examine(Vector3 worldPos = default)
		{
			int contentsCount = objectContainer.GetStoredObjects().Count();
			return $"There {(contentsCount == 1 ? "is one entity" : $"are {contentsCount} entities")} inside.";
		}
	}
}