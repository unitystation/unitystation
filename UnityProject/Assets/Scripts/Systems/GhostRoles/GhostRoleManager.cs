using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Messages.Server;
using Messages.Client;
using ScriptableObjects;
using UI.Systems.Ghost;

namespace Systems.GhostRoles
{
	/// <summary>
	/// Manages all available ghost roles. Central point for creating, updating and removing roles, and delegating players.
	/// </summary>
	public class GhostRoleManager : MonoBehaviour
	{
		[SerializeField]
		private GhostRoleList ghostRoleList = default;
		/// <summary> A list of all ghost roles that can be created.</summary>
		public List<GhostRoleData> GhostRoles => ghostRoleList.GhostRoles;

		public static GhostRoleManager Instance { get; private set; }

		/// <summary> A list of all instantiated and available ghost roles on the server.</summary>
		public readonly Dictionary<uint, GhostRoleServer> serverAvailableRoles = new Dictionary<uint, GhostRoleServer>();
		/// <summary> A list of all instantiated and available ghost roles that this client knows about. </summary>
		public readonly Dictionary<uint, GhostRoleClient> clientAvailableRoles = new Dictionary<uint, GhostRoleClient>();
		private uint currentKeyIndex = 0; // For key generation when adding new roles.

		#region Lifecycle

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(this);
			}
		}

		private void OnEnable()
		{
			SceneManager.activeSceneChanged += OnRoundRestart;
		}

		private void OnDisable()
		{
			SceneManager.activeSceneChanged -= OnRoundRestart;
		}

		private void OnRoundRestart(Scene scene, Scene newScene)
		{
			serverAvailableRoles.Clear();
			clientAvailableRoles.Clear();
		}

		#endregion Lifecycle

		/// <summary>
		/// Create a new ghost role instance on the server from the given ghost role data.
		/// The role availability will be broadcast to all dead players.
		/// </summary>
		/// <param name="roleData">The ghost role data to create the role with. Must be defined in GhostRoleList SO.</param>
		/// <returns>The key with which the new role was generated, for future reference, if successful.</returns>
		public uint ServerCreateRole(GhostRoleData roleData)
		{
			int roleIndex = GhostRoles.FindIndex(r => r == roleData);
			if (roleIndex < 0)
			{
				Logger.LogError(
						$"Ghost role \"{roleData}\" was not found in {nameof(GhostRoleList)} SO! Cannot inform clients about the ghost role.");
				return default;
			}

			GhostRoleServer role = new GhostRoleServer(roleIndex);
			uint key = ServerAddRole(role);
			role.OnTimerExpired += () =>
			{
				serverAvailableRoles.Remove(key);
			};

			GhostRoleUpdateMessage.SendToDead(key);

			return key;
		}

		/// <summary>
		/// Update an existing ghost role via its key, on the server. The changes will be sent to all dead players.
		/// </summary>
		/// <param name="key">The key used to identify the role for modifying. Returned by <see cref="ServerCreateRole(GhostRoleData)"/>"/></param>
		public void ServerUpdateRole(uint key, int minPlayers, int maxPlayers, float timeRemaining)
		{
			serverAvailableRoles[key].UpdateRole(minPlayers, maxPlayers, timeRemaining);
			GhostRoleUpdateMessage.SendToDead(key);
		}

		/// <summary>
		/// Adds or updates a ghost role by its key, on the client. Informs GhostRoleWindow about the role.
		/// </summary>
		/// <param name="key">The key used to identify the role for modifying.</param>
		/// <returns>Returns the GhostRoleClient generated or found by the key.</returns>
		public GhostRoleClient ClientAddOrUpdateRole(
				uint key, int typeIndex, int minPlayers, int maxPlayers, int playerCount, float timeRemaining)
		{
			if (typeIndex > GhostRoles.Count)
			{
				Logger.LogError($"Ghost role index does not exist in {nameof(GhostRoleList)}! Cannot add to local available ghost role list.");
				return default;
			}

			if (clientAvailableRoles.ContainsKey(key) == false)
			{
				GhostRoleClient newRole = new GhostRoleClient(typeIndex, playerCount, timeRemaining);
				clientAvailableRoles.Add(key, newRole);
				newRole.OnTimerExpired += () =>
				{
					clientAvailableRoles.Remove(key);
				};

				UIManager.Display.hudBottomGhost.GetComponent<UI_GhostOptions>().NewGhostRoleAvailable(GhostRoles[typeIndex]);
			}
			
			GhostRoleClient role = clientAvailableRoles[key];
			role.UpdateRole(minPlayers, maxPlayers, timeRemaining, playerCount);

			// Will be exactly -1 if timeout is set as indefinite.
			if (timeRemaining <= 0 && timeRemaining != -1)
			{
				UIManager.GhostRoleWindow.RemoveEntry(key);
				clientAvailableRoles.Remove(key);
				return default;
			}
			else
			{
				UIManager.GhostRoleWindow.AddOrUpdateEntry(key, role);
				return clientAvailableRoles[key];
			}
		}

		/// <summary>
		/// Sends a network message to the server requesting ghost role assignment to the role associated with the given key.
		/// </summary>
		/// <param name="key">The unique key the ghost role instance is associated with.</param>
		public void LocalGhostRequestRole(uint key)
		{
			if (PlayerManager.LocalPlayerScript.IsDeadOrGhost == false) return;

			RequestGhostRoleMessage.Send(key);
		}

		/// <summary>
		/// Requests the given player to be assigned to the role associated with the given key.
		/// If the role was recently created, the player will be moved to a pool. At the end of this period,
		/// random players will be selected for assignment until there are no more players or the maximum player limit is reached.
		/// </summary>
		/// <param name="player">The player to be assigned to the role.</param>
		/// <param name="key">The unique key the ghost role instance is associated with.</param>
		public void ServerGhostRequestRole(ConnectedPlayer player, uint key)
		{
			GhostRoleServer role = serverAvailableRoles[key];
			if (role.QuickPoolInProgress)
			{
				role.QuickPlayerPool.Add(player);
				return;
			}

			ServerTryAddPlayerToRole(player, key);
		}

		/// <summary>
		/// Removes the associated role of the given key from the available roles list. Dead players are informed of the unavailability.
		/// </summary>
		/// <param name="key">The unique key the ghost role instance is associated with.</param>
		public void ServerRemoveRole(uint key)
		{
			if (serverAvailableRoles.ContainsKey(key) == false)
			{
				Logger.LogWarning("Tried to remove ghost role instance that doesn't or no longer exists.");
				return;
			}

			serverAvailableRoles[key].TimeRemaining = -2; // -2 distinguishes from normal timer expiry and an indefinite role
			GhostRoleUpdateMessage.SendToDead(key);
			serverAvailableRoles.Remove(key);
		}

		private bool ServerPlayerIsQueued(ConnectedPlayer player)
		{
			foreach (KeyValuePair<uint, GhostRoleServer> kvp in serverAvailableRoles)
			{
				if (kvp.Value.WaitingPlayers.Contains(player)) return true;
			}

			return false;
		}

		private GhostRoleResponseCode VerifyPlayerCanQueue(ConnectedPlayer player, uint key)
		{
			if (player.Script.IsDeadOrGhost == false)
			{
				return GhostRoleResponseCode.Error;
			}

			if (serverAvailableRoles.ContainsKey(key) == false)
			{
				return GhostRoleResponseCode.RoleNotFound;
			}

			GhostRoleServer role = serverAvailableRoles[key];
			if (role.WaitingPlayers.Contains(player))
			{
				return GhostRoleResponseCode.AlreadyWaiting;
			}

			if (ServerPlayerIsQueued(player))
			{
				return GhostRoleResponseCode.AlreadyQueued;
			}

			if (role.WaitingPlayers.Count >= role.MaxPlayers)
			{
				return GhostRoleResponseCode.QueueFull;
			}

			return GhostRoleResponseCode.Success;
		}

		private uint ServerAddRole(GhostRoleServer item)
		{
			serverAvailableRoles.Add(++currentKeyIndex, item);
			return currentKeyIndex;
		}

		private void ServerTryAddPlayerToRole(ConnectedPlayer player, uint key)
		{
			GhostRoleResponseCode responseCode = VerifyPlayerCanQueue(player, key);

			if (responseCode == GhostRoleResponseCode.Success)
			{
				serverAvailableRoles[key].AddPlayer(player);

				GhostRoleUpdateMessage.SendToDead(key);
			}

			GhostRoleResponseMessage.SendTo(player, key, responseCode);
		}
	}
}
