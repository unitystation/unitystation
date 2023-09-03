using System;
using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using NaughtyAttributes;
using ScriptableObjects;
using ScriptableObjects.Items.SpellBook;
using Systems.GhostRoles;
using Systems.Spells;

namespace Items.Magical
{
	/// <summary>
	/// Allows a wizard to spawn an apprentice wizard via a ghost role.
	/// </summary>
	public class ContractOfApprenticeship : MonoBehaviour
	{
		[SerializeField, Required] private GhostRoleData ghostRole = default;
		[SerializeField, ReorderableList] private MagicSchool[] schools = default;

		public event Action OnGhostRoleTimeout;
		public event Action OnApprenticeSpawned;

		public MagicSchool SelectedSchool { get; private set; }
		public PlayerInfo BoundTo { get; private set; }
		public PlayerInfo Apprentice { get; private set; }
		public bool WasUsed => Apprentice != null;

		private uint createdRoleKey;

		private HasNetworkTabItem netTab;

		private void Awake()
		{
			netTab = GetComponent<HasNetworkTabItem>();
		}

		public void SelectSchool(int schoolIndex)
		{
			SelectedSchool = schools[schoolIndex];

			CreateGhostRole();
		}

		public void CreateGhostRole()
		{
			if (GhostRoleManager.Instance.serverAvailableRoles.ContainsKey(createdRoleKey))
			{
				Loggy.LogWarning("A wizard apprentice ghost role already exists.", Category.Spells);
				return;
			}
			else if (WasUsed)
			{
				Loggy.LogWarning("This contract has already been used. Cannot spawn another apprentice.", Category.Spells);
				return;
			}

			BoundTo = netTab.LastInteractedPlayer().Player();

			createdRoleKey = GhostRoleManager.Instance.ServerCreateRole(ghostRole);
			GhostRoleServer role = GhostRoleManager.Instance.serverAvailableRoles[createdRoleKey];

			role.OnPlayerAdded += SpawnApprentice;
			role.OnTimerExpired += OnGhostRoleTimeout;
		}

		public void CancelApprenticeship()
		{
			GhostRoleManager.Instance.ServerRemoveRole(createdRoleKey);
		}

		private void SpawnApprentice(PlayerInfo player)
		{
			player.Script.PlayerNetworkActions.ServerRespawnPlayerAntag(player, "Wizard Apprentice");

			Apprentice = player;
			OnApprenticeSpawned?.Invoke();

			foreach (SpellBookEntry entry in SelectedSchool.spellEntries)
			{
				if (entry is SpellBookSpell spellEntry)
				{
					Spell spell = spellEntry.Spell.AddToPlayer(player.Mind);
					player.Mind.AddSpell(spell);
				}
				else if (entry is SpellBookArtifact spellArtifact)
				{
					foreach (GameObject prefab in spellArtifact.Artifacts)
					{
						GameObject item = Spawn.ServerPrefab(prefab, player.Script.WorldPos).GameObject;
						player.Script.DynamicItemStorage.GetBestHandOrSlotFor(item);
					}
				}
			}
		}

		[Serializable]
		public class MagicSchool
		{
			[SerializeField]
			public string Name = default;
			[SerializeField, ReorderableList]
			public SpellBookEntry[] spellEntries = default;
		}
	}
}
