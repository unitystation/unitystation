using System.Collections.Generic;
using AddressableReferences;
using Messages.Server;
using UnityEngine;

namespace Objects.Disposals
{
	/// <summary>
	/// A virtual container for disposal instances. Contains the disposed contents,
	/// and allows the contents to be dealt with when the disposal instance ends.
	/// </summary>
	public class DisposalVirtualContainer : MonoBehaviour, IServerDespawn, IExaminable
	{
		[Tooltip("The sound made when someone is trying to move in pipes.")]
		[SerializeField]
		private AddressableAudioSource ClangSound;

		private Matrix Matrix;
		private ObjectBehaviour ContainerBehaviour;

		// transform.position seems to be the only reliable method after OnDespawnServer() has been called.
		private Vector3 ContainerWorldPosition => transform.position;

		// We store each type of entity separately, because each entity type requires different operations.
		private readonly List<ObjectBehaviour> containedItems = new List<ObjectBehaviour>();
		private readonly List<ObjectBehaviour> containedObjects = new List<ObjectBehaviour>();
		private readonly List<ObjectBehaviour> containedPlayers = new List<ObjectBehaviour>();

		public int ContentsCount => containedItems.Count + containedObjects.Count + containedPlayers.Count;
		public bool HasContents => ContentsCount > 0;

		private void Awake()
		{
			Matrix = gameObject.RegisterTile().Matrix;
			ContainerBehaviour = GetComponent<ObjectBehaviour>();
		}

		#region AddContents

		public void AddItems(IEnumerable<ObjectBehaviour> items)
		{
			foreach (ObjectBehaviour item in items)
			{
				AddItem(item);
			}
		}

		public void AddObjects(IEnumerable<ObjectBehaviour> objects)
		{
			foreach (ObjectBehaviour entity in objects)
			{
				AddObject(entity);
			}
		}

		public void AddPlayers(IEnumerable<ObjectBehaviour> players)
		{
			foreach (ObjectBehaviour player in players)
			{
				AddPlayer(player);
			}
		}

		public void AddItem(ObjectBehaviour item)
		{
			containedItems.Add(item);
			SetContainerAndMakeInvisible(item);
		}

		public void AddObject(ObjectBehaviour entity)
		{
			containedObjects.Add(entity);
			SetContainerAndMakeInvisible(entity);
		}

		public void AddPlayer(ObjectBehaviour player)
		{
			containedPlayers.Add(player);
			SetContainerAndMakeInvisible(player);
			FollowCameraMessage.Send(player.gameObject, gameObject);
		}

		void SetContainerAndMakeInvisible(ObjectBehaviour entity)
		{
			entity.parentContainer = ContainerBehaviour;
			entity.VisibleState = false;
		}

		#endregion AddContents

		#region RemoveContents

		public void RemoveItem(ObjectBehaviour item)
		{
			if (containedItems.Remove(item))
			{
				EjectItemOrObject(item);
			}
		}

		public void RemoveObject(ObjectBehaviour entity)
		{
			if (containedObjects.Remove(entity))
			{
				EjectItemOrObject(entity);
			}
		}

		public void RemovePlayer(ObjectBehaviour player)
		{
			if (containedPlayers.Remove(player))
			{
				EjectPlayer(player);
			}
		}

		#endregion RemoveContents

		#region EjectContents

		private void EjectContainedItems()
		{
			foreach (ObjectBehaviour item in containedItems)
			{
				EjectItemOrObject(item);
			}
		}

		private void EjectContainedObjects()
		{
			foreach (ObjectBehaviour entity in containedObjects)
			{
				EjectItemOrObject(entity);
			}
		}

		private void EjectContainedPlayers()
		{
			foreach (ObjectBehaviour player in containedPlayers)
			{
				EjectPlayer(player);
			}
		}

		private void EjectItemOrObject(ObjectBehaviour entity)
		{
			if (entity == null) return;

			entity.parentContainer = null;
			if (entity.TryGetComponent<CustomNetTransform>(out var netTransform))
			{
				netTransform.SetPosition(ContainerWorldPosition);
			}
		}

		private void EjectPlayer(ObjectBehaviour player)
		{
			if (player == null) return;

			player.parentContainer = null;
			if (player.TryGetComponent<PlayerSync>(out var playerSync))
			{
				playerSync.SetPosition(ContainerWorldPosition);
				FollowCameraMessage.Send(player.gameObject, player.gameObject);
			}
		}

		private void ThrowContainedItems(Vector3 throwVector)
		{
			foreach (ObjectBehaviour item in containedItems)
			{
				ThrowItem(item, throwVector);
			}
		}

		private void ThrowContainedObjects(Vector3 throwVector)
		{
			foreach (ObjectBehaviour entity in containedObjects)
			{
				// Objects cannot currently be thrown, so just push them in the direction for now.
				PushObject(entity, throwVector);
			}
		}

		private void ThrowContainedPlayers(Vector3 throwVector)
		{
			foreach (ObjectBehaviour player in containedPlayers)
			{
				// Players cannot currently be thrown, so just push them in the direction for now.
				PushPlayer(player, throwVector);
			}
		}

		private void ThrowItem(ObjectBehaviour item, Vector3 throwVector)
		{
			Vector3 vector = item.transform.rotation * throwVector;
			ThrowInfo throwInfo = new ThrowInfo
			{
				ThrownBy = Matrix.transform.parent.gameObject,
				Aim = BodyPartType.Chest,
				OriginWorldPos = ContainerWorldPosition,
				WorldTrajectory = vector,
				SpinMode = SpinMode.Clockwise // TODO: randomise this
			};

			CustomNetTransform itemTransform = item.GetComponent<CustomNetTransform>();
			if (itemTransform == null) return;
			itemTransform.Throw(throwInfo);
		}

		private void PushObject(ObjectBehaviour entity, Vector3 pushVector)
		{
			entity.QueuePush(pushVector.NormalizeTo2Int());
			//entity.TryPush(pushVector.NormalizeTo2Int());
			//GetComponent<CustomNetTransform>().Push(pushVector.NormalizeTo2Int());
		}

		private void PushPlayer(ObjectBehaviour player, Vector3 pushVector)
		{
			//PushObject(player, pushVector);
			player.GetComponent<RegisterPlayer>().ServerStun();
			player.QueuePush(pushVector.NormalizeTo2Int());
		}

		/// <summary>
		/// Ejects contents at the virtual container's position with no spin.
		/// </summary>
		public void EjectContents()
		{
			EjectContainedItems();
			EjectContainedObjects();
			EjectContainedPlayers();

			containedItems.Clear();
			containedObjects.Clear();
			containedPlayers.Clear();
		}

		/// <summary>
		/// Ejects contents at the virtual container's position, then throws each entity with the given throw vector.
		/// </summary>
		/// <param name="throwVector">The direction and distance to throw the contents with</param>
		public void EjectContentsAndThrow(Vector3 throwVector)
		{
			EjectContainedItems();
			EjectContainedObjects();
			EjectContainedPlayers();
			ThrowContainedItems(throwVector);
			ThrowContainedObjects(throwVector);
			ThrowContainedPlayers(throwVector);

			containedItems.Clear();
			containedObjects.Clear();
			containedPlayers.Clear();
		}

		#endregion EjectContents

		public void PlayerTryEscaping(GameObject player)
		{
			if (player.TryGetComponent<ObjectBehaviour>(out var playerBehaviour) == false) return;
			if (containedPlayers.Contains(playerBehaviour) == false) return;
			if (ContainerBehaviour.parentContainer == null) return;

			GameObject disposalMachine = ContainerBehaviour.parentContainer.gameObject;
			if (disposalMachine == null)
			{
				// Must be in the disposal pipes
				SoundManager.PlayNetworkedAtPos(ClangSound, ContainerWorldPosition);
			}
			else if (disposalMachine.TryGetComponent<DisposalBin>(out var disposalBin))
			{
				// In a disposal bin
				disposalBin.PlayerTryClimbingOut(player);
			}
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			EjectContents();
		}

		public string Examine(Vector3 worldPos = default)
		{
			int contentsCount = ContentsCount;
			return $"There {(contentsCount == 1 ? "is one entity" : $"are {contentsCount} entities")} inside.";
		}
	}
}
