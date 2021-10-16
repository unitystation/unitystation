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

		private readonly HashSet<GameObject> storedObjects = new HashSet<GameObject>();

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

		public void StoreObject(GameObject obj)
		{
			storedObjects.Add(obj);

			if (obj.TryGetComponent<ObjectBehaviour>(out var objBehaviour))
			{
				objBehaviour.parentContainer = objectBehaviour;
				objBehaviour.VisibleState = false;

				if (obj.TryGetComponent<PlayerScript>(out var playerScript))
				{
					playerScript.playerMove.IsTrapped = true;

					// Start tracking closet
					if (playerScript.IsGhost == false)
					{
						FollowCameraMessage.Send(obj, gameObject);
					}

					CheckPlayerCrawlState(objBehaviour);
				}
			}
		}

		public void StoreObjects(IEnumerable<GameObject> gameObjects)
		{
			foreach (var gameObject in gameObjects)
			{
				StoreObject(gameObject);
			}
		}

		public IEnumerable<GameObject> GetStoredObjects()
		{
			if (initialContentsSpawned == false)
			{
				TrySpawnInitialContents();
			}

			foreach (var obj in storedObjects)
			{
				// May have despawned while in storage.
				if (obj == null) continue;

				yield return obj;
			}
		}

		/// <summary>
		/// Takes the given object out of storage, dropping it in the tile of the container, inheriting the inertia of the container.
		/// If the object belongs to a player, then sends a <see cref="FollowCameraMessage"/>.
		/// </summary>
		public void RetrieveObject(GameObject obj)
		{
			storedObjects.Remove(obj);

			if (obj.TryGetComponent<ObjectBehaviour>(out var objBehaviour))
			{
				objBehaviour.parentContainer = null;

				if (obj.TryGetComponent<CustomNetTransform>(out var cnt))
				{
					//avoids blinking of premapped items when opening first time in another place:
					Vector3Int pos = registerTile.WorldPositionServer;
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

		public void RetrieveObjects()
		{
			foreach (var entity in GetStoredObjects().ToArray())
			{
				RetrieveObject(entity);
			}

			storedObjects.Clear();
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
