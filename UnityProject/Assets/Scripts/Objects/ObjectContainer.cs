using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Messages.Server;

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
		void EntityTryEscape(GameObject entity);
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

		[SerializeField]
		[Tooltip("Whether the contents will spawn at roundstart or be spawned manually.")]
		private bool spawnContentsAtRoundstart = true;

		public bool IsEmpty => storedObjects.Count == 0;

		private bool initialContentsSpawned = false;

		private RegisterTile registerTile;
		private ObjectBehaviour objectBehaviour;

		// stored contents and their positional offsets, if applicable
		private readonly Dictionary<GameObject, Vector3> storedObjects = new Dictionary<GameObject, Vector3>();

		#region Lifecycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			objectBehaviour = GetComponent<ObjectBehaviour>();

			registerTile.OnParentChangeComplete.AddListener(() =>
			{
				ReparentStoredObjects(registerTile.NetworkedMatrixNetId);
			});
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (spawnContentsAtRoundstart)
			{
				TrySpawnInitialContents(true);
			}
		}

		public void OnDespawnServer(DespawnInfo info)
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

			if (obj.TryGetComponent<ObjectBehaviour>(out var objBehaviour))
			{
				objBehaviour.parentContainer = objectBehaviour;
				objBehaviour.VisibleState = false;

				if (obj.TryGetComponent<PlayerScript>(out var playerScript))
				{
					playerScript.playerMove.IsTrapped = true;

					// Start tracking container
					if (playerScript.IsGhost == false)
					{
						FollowCameraMessage.Send(obj, gameObject);
					}

					CheckPlayerCrawlState(objBehaviour);
				}
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
			foreach (var entity in registerTile.Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, true))
			{
				// Don't add the container to itself...
				if (entity.gameObject == gameObject) continue;

				// Can't store secured objects.
				if (entity.IsPushable == false) continue;

				StoreObject(entity.gameObject, entity.transform.position - transform.position);
			}
		}

		public IEnumerable<GameObject> GetStoredObjects()
		{
			if (initialContentsSpawned == false)
			{
				TrySpawnInitialContents();
			}

			foreach (var obj in storedObjects.Keys)
			{
				// May have despawned while in storage.
				if (obj == null) continue;

				yield return obj;
			}
		}

		public void RemoveObject(GameObject obj)
		{
			storedObjects.Remove(obj);
		}

		/// <summary>
		/// Takes the given object out of storage, dropping it in the tile of the container, inheriting the inertia of the container.
		/// If the object belongs to a player, then sends a <see cref="FollowCameraMessage"/>.
		/// </summary>
		public void RetrieveObject(GameObject obj, Vector3? worldPosition = null)
		{
			if (obj == null || storedObjects.TryGetValue(obj, out var offset) == false) return;
			storedObjects.Remove(obj);

			if (obj.TryGetComponent<ObjectBehaviour>(out var objBehaviour))
			{
				objBehaviour.parentContainer = null;

				if (obj.TryGetComponent<CustomNetTransform>(out var cnt))
				{
					//avoids blinking of premapped items when opening first time in another place:
					Vector3 pos = worldPosition.GetValueOrDefault(registerTile.WorldPositionServer) + offset;
					cnt.AppearAtPositionServer(pos);
					if (objectBehaviour.Pushable.IsMovingServer)
					{
						cnt.InertiaDrop(pos, objectBehaviour.Pushable.SpeedServer, objectBehaviour.InheritedImpulse.To2Int());
					}
				}
				else if (obj.TryGetComponent<PlayerScript>(out var playerScript))
				{
					playerScript.PlayerSync.AppearAtPositionServer(registerTile.WorldPositionServer);
					playerScript.playerMove.IsTrapped = false;
					if (objectBehaviour.Pushable.IsMovingServer)
					{
						objBehaviour.TryPush(objectBehaviour.InheritedImpulse.To2Int(), objectBehaviour.Pushable.SpeedServer);
					}

					// Stop tracking closet
					FollowCameraMessage.Send(obj, obj);
					CheckPlayerCrawlState(objBehaviour);
				}
			}
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
				kvp.Key.GetComponent<ObjectBehaviour>().parentContainer = objectBehaviour;

				if (kvp.Key.TryGetComponent<PlayerScript>(out var playerScript))
				{
					// update player camera target
					if (playerScript.IsGhost == false)
					{
						FollowCameraMessage.Send(kvp.Key, gameObject);
					}
				}
			}
		}

		private void CheckPlayerCrawlState(ObjectBehaviour playerBehaviour)
		{
			var regPlayer = playerBehaviour.GetComponent<RegisterPlayer>();
			regPlayer.HandleGetupAnimation(!regPlayer.IsLayingDown);
		}

		/// <summary>
		/// Invoked when the parent net ID of this object's RegisterTile changes. Updates the parent net ID of the player / items
		/// in the container, passing the update on to their RegisterTile behaviors.
		/// </summary>
		/// <param name="parentNetId">new parent net ID</param>
		private void ReparentStoredObjects(uint parentNetId)
		{
			foreach (GameObject obj in GetStoredObjects())
			{
				obj.RegisterTile().ServerSetNetworkedMatrixNetID(parentNetId);
			}
		}
	}
}
