﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ScriptableObjects;
using Systems.Character;
using Antagonists;

namespace Systems.GhostRoles
{
	/// <summary>
	/// An instantiated representation of a ghost role that is valid both server and client side.
	/// The base for <see cref="GhostRoleServer"/> and <see cref="GhostRoleClient"/>.
	/// </summary>
	public abstract class GhostRole
	{
		/// <summary>Static data pertaining to this specific ghost role.</summary>
		public readonly GhostRoleData RoleData;
		/// <summary>The index of the GhostRoleData in the GhostRoleList SO.</summary>
		protected int roleListIndex;
		public int RoleListIndex => roleListIndex;

		/// <summary>The key for GhostRoles in GhostRoleManager.</summary>
		public readonly uint RoleKey;

		/// <summary> The minimum amount of players this ghost role instance can support.</summary>
		public int MinPlayers { get; private set; }
		/// <summary> The maximum amount of players this ghost role instance can support.</summary>
		public int MaxPlayers { get; private set; }
		/// <summary> The amount of time remaining for this ghost role instance.
		/// Invokes <see cref="OnTimerExpired"/> at the end of this period.</summary>
		public float TimeRemaining { get; set; }

		public bool RandomiseCharacterSheet { get; set; } = true;

		/// <summary> Invoked when <see cref="TimeRemaining"/> hits zero.</summary>
		public Action OnTimerExpired;

		protected Coroutine timeoutCoroutine;
		protected bool stopCor = false;

		protected GhostRole(int roleDataIndex, uint roleKey)
		{
			roleListIndex = roleDataIndex;
			RoleData = GhostRoleManager.Instance.GhostRoles[roleDataIndex];

			UpdateRole(RoleData.MinPlayers, RoleData.MaxPlayers, RoleData.Timeout);
			RoleKey = roleKey;
		}

		public void UpdateRole(int minPlayers, int maxPlayers, float timeRemaining)
		{
			MinPlayers = minPlayers;
			MaxPlayers = maxPlayers;
			if (timeRemaining.Approx(-1) == true)
			{
				stopCor = true;
			} else if (stopCor == true)
			{
				stopCor = false;
				timeoutCoroutine = GhostRoleManager.Instance.StartCoroutine(TimeoutTimer(timeRemaining));
				return;
			}
			TimeRemaining = timeRemaining;
		}

		public void UpdateRole(int minPlayers, int maxPlayers, float timeRemaining, int newRoleListIndex)
		{
			MinPlayers = minPlayers;
			MaxPlayers = maxPlayers;
			roleListIndex = newRoleListIndex;
			if (timeRemaining.Approx(-1) == true)
			{
				stopCor = true;
			}
			else if (stopCor == true)
			{
				stopCor = false;
				timeoutCoroutine = GhostRoleManager.Instance.StartCoroutine(TimeoutTimer(timeRemaining));
				return;
			}
			TimeRemaining = timeRemaining;
		}

		protected virtual IEnumerator TimeoutTimer(float timeRemaining)
		{
			if (timeRemaining == -1) yield break; // -1 represents indefinite role

			TimeRemaining = timeRemaining;
			while (TimeRemaining > 0)
			{
				TimeRemaining -= Time.deltaTime;

				if (stopCor == true)
					yield break;

				yield return WaitFor.EndOfFrame;
			}

			if (stopCor == true)
			{
				yield break;
			}

			OnTimerExpired?.Invoke();
		}

		public override string ToString()
		{
			return RoleData.Name;
		}
	}

	/// <summary>
	/// An instantiated representation of a ghost role for the server.
	/// Inherits from <see cref="GhostRole"/>.
	/// </summary>
	public class GhostRoleServer : GhostRole
	{
		/// <summary>A list of players currently signed up to this specific ghost role instance.</summary>
		public readonly List<PlayerInfo> WaitingPlayers = new List<PlayerInfo>();

		public bool QuickPoolInProgress { get; private set; }
		/// <summary>The players that quickly request the role upon availability. Players will be randomly selected from this pool.</summary>
		public readonly List<PlayerInfo> QuickPlayerPool = new List<PlayerInfo>();

		/// <summary>Invoked when a player is successfully added to <see cref="WaitingPlayers"/>.</summary>
		public event Action<PlayerInfo> OnPlayerAdded;
		/// <summary>Invoked when the count of <see cref="WaitingPlayers"/> hits <see cref="GhostRole.MinPlayers"/>.</summary>
		public event Action OnMinPlayersReached;
		/// <summary>Invoked when the count of <see cref="WaitingPlayers"/> hits <see cref="GhostRole.MaxPlayers"/>.</summary>
		public event Action OnMaxPlayersReached;

		private int totalPlayers = 0;

		private readonly Team ghostRoleTeam;

		private int playersSpawned = 0;
		public int PlayersSpawned => playersSpawned;

		public GhostRoleServer(int roleDataIndex, uint roleKey) : base(roleDataIndex, roleKey)
		{
			timeoutCoroutine = GhostRoleManager.Instance.StartCoroutine(TimeoutTimer(RoleData.Timeout));
			if (RoleData.Team != null)
				ghostRoleTeam = AntagManager.Instance.CreateTeam(RoleData.Team);

			if (RoleData.RespawnType != GhostRoleSpawnType.Custom)
			{
				EnableDefaultRespawning();
			}

			GhostRoleManager.Instance.StartCoroutine(CreateQuickPlayerPool());
		}

		/// <summary>
		/// Add a player to this ghost role. Invokes <see cref="OnPlayerAdded"/> and, in addition,
		/// possibly <see cref="OnMinPlayersReached"/>, <see cref="OnMaxPlayersReached"/>.
		/// Intended for use with <see cref="GhostRoleManager"/>.
		/// </summary>
		public void AddPlayer(PlayerInfo player)
		{
			WaitingPlayers.Add(player);
			totalPlayers++;

			OnPlayerAdded?.Invoke(player);

			if (totalPlayers == MinPlayers)
			{
				OnMinPlayersReached?.Invoke();
			}
			if (totalPlayers == MaxPlayers)
			{
				OnMaxPlayersReached?.Invoke();
				TimeRemaining = -2; // this ghost role is full; kill it
			}
		}

		private void EnableDefaultRespawning()
		{
			OnPlayerAdded += (PlayerInfo player) =>
			{
				if (totalPlayers < MinPlayers) return;
				SpawnPlayer(player);
				if (ghostRoleTeam != null)
					player.Mind.AntagPublic.CurTeam = ghostRoleTeam;
				WaitingPlayers.Remove(player);
			};

			OnMinPlayersReached += () =>
			{
				foreach (PlayerInfo player in WaitingPlayers)
				{
					SpawnPlayer(player);
					if (ghostRoleTeam != null)
						player.Mind.AntagPublic.CurTeam = ghostRoleTeam;
				}
				WaitingPlayers.Clear();
			};
		}

		private void SpawnPlayer(PlayerInfo player)
		{
			playersSpawned++;
			if (RandomiseCharacterSheet) player.Mind.CurrentCharacterSettings = CharacterSheet.GenerateRandomCharacter();
			if (RoleData.IsAntagonist)
			{
				player.Script.PlayerNetworkActions.ServerRespawnPlayerAntag(player, RoleData.TargetAntagonist.AntagName);
			}
			else
			{
				player.Mind.occupation = RoleData.TargetOccupation;
				player.Script.PlayerNetworkActions.ServerRespawnPlayer();
			}
		}

		protected override IEnumerator TimeoutTimer(float timeRemaining)
		{
			if (timeRemaining == -1) yield break; // -1 represents indefinite role

			TimeRemaining = timeRemaining;
			while (TimeRemaining > 0)
			{
				TimeRemaining -= Time.deltaTime;

				if (stopCor == true)
					yield break;

				yield return WaitFor.EndOfFrame;
			}

			if (TimeRemaining.Approx(-2) == true || stopCor == true) // -2 represents role prematurely ended
			{
				yield break; // Don't invoke OnTimerExpired for premature endings
			}
			OnTimerExpired?.Invoke();
		}

		private IEnumerator CreateQuickPlayerPool()
		{
			QuickPoolInProgress = true;
			yield return WaitFor.Seconds(1);
			QuickPoolInProgress = false;

			for (int i = 0; i < QuickPlayerPool.Count; i++)
			{
				PlayerInfo player = QuickPlayerPool.PickRandom();
				if (player == null) break;

				QuickPlayerPool.Remove(player);
				if (player.Equals(PlayerInfo.Invalid)) continue;

				var kvp = GhostRoleManager.Instance.serverAvailableRoles.FirstOrDefault(role => role.Value == this);
				GhostRoleManager.Instance.ServerGhostRequestRole(player, kvp.Key);
			}

			QuickPlayerPool.Clear();
		}
	}

	/// <summary>
	/// An instantiated representation of a ghost role for the client.
	/// Inherits from <see cref="GhostRole"/>.
	/// </summary>
	public sealed class GhostRoleClient : GhostRole
	{
		/// <summary>
		/// The amount of players this client is known to have for this role.
		/// </summary>
		public int PlayerCount { get; set; }

		public GhostRoleClient(int roleDataIndex, int playerCount, float timeRemaining, uint roleKey) : base(roleDataIndex, roleKey)
		{
			PlayerCount = playerCount;
			timeoutCoroutine = GhostRoleManager.Instance.StartCoroutine(TimeoutTimer(timeRemaining));
		}

		public void UpdateRole(int minPlayers, int maxPlayers, float timeRemaining, int playerCount)
		{
			UpdateRole(minPlayers, maxPlayers, timeRemaining);
			PlayerCount = playerCount;
		}

		public void UpdateRole(int minPlayers, int maxPlayers, float timeRemaining, int playerCount, int newIndex)
		{
			UpdateRole(minPlayers, maxPlayers, timeRemaining);
			PlayerCount = playerCount;
			roleListIndex = newIndex;
		}
	}
}
