using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Messages.Server;
using Mirror;
using Objects;

namespace Objects
{
	/// <summary>
	/// Allows a mob to attempt to come out of an object with an <see cref="ObjectContainer"/>.
	/// </summary>
	public interface IEscapable
	{
		/// <summary>
		/// Invoked when a mob attempts to escape the object.
		/// </summary>
		/// <remarks>The mob could be a player, bot, animal etc.</remarks>
		/// <param name="entity">The <c>GameObject</c> of the mob attempting to escape.</param>
		/// <param name="ifCompleted">An <c>Action</c> to carry out if escape is successful. </param>
		/// <param name="moveAction">The move direction for this escape try</param>
		void EntityTryEscape(GameObject entity, [CanBeNull] Action ifCompleted, MoveAction moveAction);
	}

	/// <summary>
	/// Allows an object to contain other objects. For example, closets.
	/// </summary>
	public class ObjectContainer : MonoBehaviour, IServerLifecycle
	{
		[Header("Initial contents")]
		[SerializeField]
		[Tooltip("Contents that will be spawned inside when the container spawns.")]
		private SpawnableList initialContents = default;

		[SerializeField] [Tooltip("Whether the contents will spawn at roundstart or be spawned manually.")]
		private bool spawnContentsAtRoundstart = true;

		public bool IsEmpty => storedObjects.Count == 0;

		private bool initialContentsSpawned = false;

		public RegisterTile registerTile;
		private UniversalObjectPhysics ObjectPhysics;

		public List<IEscapable> IEscapables;

		/// <summary>
		/// Experimental. Top owner object
		/// </summary>
		public UniversalObjectPhysics TopContainer {
			get {
				if (ObjectPhysics.ContainedInObjectContainer != null)
				{
					return ObjectPhysics.ContainedInObjectContainer.TopContainer;
				}

				if (ObjectPhysics.IsVisible == false)
				{
					var pu = GetComponent<Pickupable>();
					if (pu != null && pu.ItemSlot != null)
					{
						//we are in an itemstorage, so report our root item storage object.
						var UOP = pu.ItemSlot.GetRootStorageOrPlayer().GetComponent<UniversalObjectPhysics>();
						if (UOP != null)
						{
							//our container has a pushpull, so use its parent
							return UOP;
						}
					}
				}
				return ObjectPhysics;
			}
		}


		// stored contents and their positional offsets, if applicable
		private readonly Dictionary<GameObject, Vector3> storedObjects = new Dictionary<GameObject, Vector3>();

		public Dictionary<GameObject, Vector3> StoredObjects => storedObjects;

		public int StoredObjectsCount => storedObjects.Count;

		#region Lifecycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			ObjectPhysics = GetComponent<UniversalObjectPhysics>();

			IEscapables = GetComponents<IEscapable>().ToList();
			registerTile.OnParentChangeComplete.AddListener(() =>
			{
				ReparentStoredObjects(registerTile.NetworkedMatrixNetId);
			});
		}

		public virtual void OnSpawnServer(SpawnInfo info)
		{
			if (spawnContentsAtRoundstart)
			{
				TrySpawnInitialContents(true);
			}
		}

		public virtual void OnDespawnServer(DespawnInfo info)
		{
			RetrieveObjects();
		}

		/// <summary>Spawns the initial contents when needed, not always on game start so that there's less game objects
		public void TrySpawnInitialContents(bool hideContents = false)
		{
			if (initialContentsSpawned) return;
			initialContentsSpawned = true;

			// Null check after setting true so we only do the null check once not every opening
			if (initialContents == null) return;

			// populate initial contents
			var result = initialContents.SpawnAt(SpawnDestination.At(gameObject));

			// Only hide if on spawn as the closet will be closed // TODO: when would we onl wantto spawn objects at hthe point andasdf
			if (hideContents == false) return;

			StoreObjects(result.GameObjects);
		}

		#endregion

		/// <summary>
		/// Stores the given object. Remembers the offset, if provided.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="offset"></param>
		public void StoreObject(GameObject obj, Vector3 offset = new Vector3())
		{
			storedObjects.Add(obj, offset);

			if (obj.TryGetComponent<UniversalObjectPhysics>(out var objectPhysics))
			{
				objectPhysics.StoreTo(this);
			}
		}

		/// <summary>
		/// Stores the given <c>IEnumerable</c> collection of GameObjects.
		/// </summary>
		/// <remarks>If you want to remember positional offsets, consider <see cref="StoreObject(GameObject, Vector3)"/></remarks>
		/// <param name="gameObjects"></param>
		public void StoreObjects(IEnumerable<GameObject> gameObjects)
		{
			foreach (var gameObject in gameObjects)
			{
				StoreObject(gameObject);
			}
		}

		public void GatherObjects()
		{
			foreach (var entity in registerTile.Matrix.Get<UniversalObjectPhysics>(registerTile.LocalPositionServer, true))
			{
				// Don't add the container to itself...
				if (entity.gameObject == gameObject) continue;

				// Can't store secured objects (exclude this check on mobs as e.g. magboots set pushable false)
				if (entity.IsNotPushable) continue;

				//No Nested ObjectContainer shenanigans
				if (entity.GetComponent<ObjectContainer>()) continue;

				StoreObject(entity.gameObject, entity.transform.position - transform.position);
			}
		}

		public IEnumerable<GameObject> GetStoredObjects(bool onlyInstantiated = false)
		{
			if (initialContentsSpawned == false && onlyInstantiated == false)
			{
				TrySpawnInitialContents(true);
			}

			foreach (var obj in storedObjects.Keys)
			{
				// May have despawned while in storage.
				if (obj == null) continue;

				yield return obj;
			}
		}

		//Only use for items that are being destroyed TODO Probably should cleanup values for nice pooling
		public void RemoveObject(GameObject obj)
		{
			storedObjects.Remove(obj);
		}

		/// <summary>
		/// Takes the given object out of storage, dropping it in the tile of the container, inheriting the inertia of the container.
		/// If the object belongs to a player, then sends a <see cref="FollowCameraMessage"/>.
		/// </summary>
		public void RetrieveObject(GameObject obj, Vector3? worldPosition = null, Action onDrop = null)
		{
			if (obj == null || storedObjects.TryGetValue(obj, out var offset) == false) return;
			storedObjects.Remove(obj);

			if (obj.TryGetComponent<UniversalObjectPhysics>(out var uop))
			{
				if (worldPosition == null)
				{
					uop.DropAtAndInheritMomentum(ObjectPhysics);

				}
				else
				{
					uop.AppearAtWorldPositionServer(worldPosition.Value);
				}
				uop.StoreTo(null);
			}

			onDrop?.Invoke();
		}

		public void RetrieveObjects(Vector3? worldPosition = null)
		{
			foreach (var entity in GetStoredObjects().ToArray())
			{
				RetrieveObject(entity, worldPosition);
			}

			storedObjects.Clear();
		}

		public void TransferObjectsTo(ObjectContainer container)
		{
			container.ReceiveObjects(storedObjects);
			storedObjects.Clear();
		}

		public void ReceiveObjects(Dictionary<GameObject, Vector3> objects)
		{
			foreach (var kvp in objects)
			{
				if (kvp.Key == null) continue;

				storedObjects[kvp.Key] = kvp.Value;
				kvp.Key.GetComponent<UniversalObjectPhysics>().StoreTo( this );
			}
		}

		public void RetrieveObject(Vector3 worldPosition)
		{
			foreach (var entity in GetStoredObjects().ToArray())
			{
				RetrieveObject(entity, worldPosition);
				return; //So inefficient xD
			}
		}


		/// <summary>
		/// Invoked when the parent net ID of this object's RegisterTile changes. Updates the parent net ID of the player / items
		/// in the container, passing the update on to their RegisterTile behaviors.
		/// </summary>
		/// <param name="parentNetId">new parent net ID</param>
		private void ReparentStoredObjects(uint parentNetId)
		{
			foreach (GameObject obj in GetStoredObjects(true))
			{
				obj.RegisterTile().ServerSetNetworkedMatrixNetID(parentNetId);
			}
		}

		/// <summary>
		/// Checks for Other ObjectContainers in the vicinity of the tile this ObjectContainer is in.
		/// </summary>
		/// <returns>Returns true if at least one ObjectContainer exists on the same tile.</returns>
		public bool IsAnotherContainerNear()
		{
			foreach (var entity in registerTile.Matrix.Get<UniversalObjectPhysics>(registerTile.LocalPositionServer, true))
			{
				if (entity.GetComponent<ObjectContainer>() && entity != ObjectPhysics)
				{
					return true;
				}
			}

			return false;
		}
	}
}