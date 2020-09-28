using System.Collections.Generic;
using UnityEngine;

namespace Disposals
{
	/// <summary>
	/// A virtual container for disposal instances. Contains the disposed contents,
	/// and allows the contents to be dealt with when the disposal instance ends.
	/// </summary>
	public class DisposalVirtualContainer : MonoBehaviour, IServerDespawn, IExaminable
	{
		Matrix Matrix;
		ObjectBehaviour ContainerBehaviour;

		// transform.position seems to be the only reliable method after OnDespawnServer() has been called.
		Vector3 ContainerWorldPosition => transform.position;

		// We store each type of entity separately, because each entity type requires different operations.
		List<ObjectBehaviour> containedItems = new List<ObjectBehaviour>();
		List<ObjectBehaviour> containedObjects = new List<ObjectBehaviour>();
		List<ObjectBehaviour> containedPlayers = new List<ObjectBehaviour>();

		public int ContentsCount => containedItems.Count + containedObjects.Count + containedPlayers.Count;
		public bool HasContents => ContentsCount > 0;

		void Awake()
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

		void EjectContainedItems()
		{
			foreach (ObjectBehaviour item in containedItems)
			{
				EjectItemOrObject(item);
			}
		}

		void EjectContainedObjects()
		{
			foreach (ObjectBehaviour entity in containedObjects)
			{
				EjectItemOrObject(entity);
			}
		}

		void EjectContainedPlayers()
		{
			foreach (ObjectBehaviour player in containedPlayers)
			{
				EjectPlayer(player);
			}
		}

		void EjectItemOrObject(ObjectBehaviour entity)
		{
			entity.parentContainer = null;
			entity.GetComponent<CustomNetTransform>()?.SetPosition(ContainerWorldPosition);
		}

		void EjectPlayer(ObjectBehaviour player)
		{
			player.parentContainer = null;
			player.GetComponent<PlayerSync>()?.SetPosition(ContainerWorldPosition);
			FollowCameraMessage.Send(player.gameObject, player.gameObject);
		}

		void ThrowContainedItems(Vector3 throwVector)
		{
			foreach (ObjectBehaviour item in containedItems)
			{
				ThrowItem(item, throwVector);
			}
		}

		void ThrowContainedObjects(Vector3 throwVector)
		{
			foreach (ObjectBehaviour entity in containedObjects)
			{
				// Objects cannot currently be thrown, so just push them in the direction for now.
				PushObject(entity, throwVector);
			}
		}

		void ThrowContainedPlayers(Vector3 throwVector)
		{
			foreach (ObjectBehaviour player in containedPlayers)
			{
				// Players cannot currently be thrown, so just push them in the direction for now.
				PushPlayer(player, throwVector);
			}
		}

		void ThrowItem(ObjectBehaviour item, Vector3 throwVector)
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

		void PushObject(ObjectBehaviour entity, Vector3 pushVector)
		{
			entity.QueuePush(pushVector.NormalizeTo2Int());
			//entity.TryPush(pushVector.NormalizeTo2Int());
			//GetComponent<CustomNetTransform>().Push(pushVector.NormalizeTo2Int());
		}

		void PushPlayer(ObjectBehaviour player, Vector3 pushVector)
		{
			//PushObject(player, pushVector);
			player.GetComponent<RegisterPlayer>()?.ServerStun();
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
			if (ContainerBehaviour.IsBeingPulled) Debug.Log("I was pulled");


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
			if (!player.TryGetComponent(out ObjectBehaviour playerBehaviour)) return;
			if (!containedPlayers.Contains(playerBehaviour)) return;

			GameObject disposalMachine = ContainerBehaviour.parentContainer?.gameObject;
			if (disposalMachine == null)
			{
				// Must be in the disposal pipes
				SoundManager.PlayNetworkedAtPos("Clang", ContainerWorldPosition);
			}
			else if (disposalMachine.TryGetComponent(out DisposalBin disposalBin))
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
