using System.Collections;
using System.Linq;
using UnityEngine;
using Systems.Spells;
using ScriptableObjects.Systems.Spells;
using ScriptableObjects.Items.SpellBook;
using InGameEvents;
using AddressableReferences;

namespace Items.Magical
{
	public class BookOfSpells : MonoBehaviour, IExaminable, IServerInventoryMove, ICheckedInteractable<HandActivate>
	{
		[SerializeField] private AddressableAudioSource Blind = null;

		[SerializeField] private AddressableAudioSource SummonItemsGeneric = null;

		[Tooltip("If checked, will only be usable by wizards.")]
		[SerializeField]
		private bool isForWizardsOnly = false;

		[Tooltip("Amount of points this spellbook has.")]
		[SerializeField]
		private int points = 10;

		[Tooltip("The SO containing the necessary data for populating the spellbook.")]
		[SerializeField]
		private SpellBookData data = default;

		[Tooltip("The drop-pod to spawn when spawning artifacts.")]
		[SerializeField]
		private GameObject dropPodPrefab = default;

		private HasNetworkTabItem netTab;

		// Registered to the first player to have this in their inventory.
		private PlayerScript registeredPlayerScript;

		public int Points => points;
		public SpellBookData Data => data;
		public bool IsRegistered => registeredPlayerScript != null;

		private void Awake()
		{
			netTab = GetComponent<HasNetworkTabItem>();
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (info.InventoryMoveType != InventoryMoveType.Add) return;
			if (registeredPlayerScript != null) return; // Register to only the first player.

			RegisterPlayer player = info.ToRootPlayer;
			if (player == null) return;
			registeredPlayerScript = player.PlayerScript;
		}

		public void LearnSpell(SpellBookSpell spellEntry)
		{
			if (spellEntry.Cost > Points) return;

			ConnectedPlayer player = GetLastReader();

			int currentSpellTier = GetReaderSpellLevel(spellEntry.Spell);
			if (currentSpellTier < spellEntry.Spell.TierCount)
			{
				LearnSpell(player, spellEntry);
			}
			else if (spellEntry.Spell.TierCount == 1)
			{
				Chat.AddExamineMsgFromServer(player.GameObject, "You already know this spell!");
			}
			else
			{
				Chat.AddExamineMsgFromServer(player.GameObject, "You can't upgrade this spell any further!");
			}
		}

		private void LearnSpell(ConnectedPlayer player, SpellBookSpell spellEntry)
		{
			points -= spellEntry.Cost;

			SoundManager.PlayNetworkedAtPos(Blind, player.Script.WorldPos, sourceObj: player.GameObject);
			Chat.AddChatMsgToChat(player, spellEntry.Incantation, ChatChannel.Local);

			Spell spellInstance = player.Script.mind.GetSpellInstance(spellEntry.Spell);

			if (spellInstance != null)
			{
				spellInstance.UpgradeTier();
			}
			else
			{
				Spell spell = spellEntry.Spell.AddToPlayer(player.Script);
				player.Script.mind.AddSpell(spell);
			}
		}

		public void SpawnArtifacts(SpellBookArtifact artifactEntry)
		{
			if (artifactEntry.Cost > Points) return;

			var playerScript = GetLastReader().Script;
			var spawnResult = Spawn.ServerPrefab(dropPodPrefab, playerScript.WorldPos);
			if (spawnResult.Successful)
			{
				points -= artifactEntry.Cost;
				SoundManager.PlayNetworkedAtPos(SummonItemsGeneric, playerScript.WorldPos, sourceObj: playerScript.gameObject);

				var closetControl = spawnResult.GameObject.GetComponent<ClosetControl>();

				foreach (GameObject artifactPrefab in artifactEntry.Artifacts)
				{
					spawnResult = Spawn.ServerPrefab(artifactPrefab);
					if (spawnResult.Successful)
					{
						ObjectBehaviour artifactBehaviour = spawnResult.GameObject.GetComponent<ObjectBehaviour>();
						closetControl.ServerAddInternalItem(artifactBehaviour);
					}
				}
			}
		}

		public void CastRitual(SpellBookRitual ritualEntry)
		{
			if (ritualEntry.Cost > Points) return;

			ConnectedPlayer player = GetLastReader();

			if (ritualEntry.InvocationMessage != default)
			{
				Chat.AddChatMsgToChat(player, ritualEntry.InvocationMessage, ChatChannel.Local);
			}

			if (ritualEntry.CastSound != default)
			{
				SoundManager.PlayNetworkedAtPos(ritualEntry.CastSound, player.Script.WorldPos, sourceObj: player.GameObject);
			}

			InGameEventsManager.Instance.TriggerSpecificEvent(ritualEntry.EventIndex, ritualEntry.EventType, announceEvent: false);

			points -= ritualEntry.Cost;
		}

		#region Interaction

		public string Examine(Vector3 worldPos = default)
		{
			if (registeredPlayerScript != null)
			{
				return $"There is a small signature on the front cover: {registeredPlayerScript.playerName}.";
			}

			return "It appears to have no owner.";
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			// Return true to stop NetTab interaction.
			return IsRegistered || (isForWizardsOnly && !IsWizard(interaction.Performer.Player()));
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer,
					$"The {gameObject.ExpensiveName()} does not recognise you as its owner and refuses to open!");
		}

		#endregion Interaction

		private bool IsWizard(ConnectedPlayer player)
		{
			return player.Script.mind.IsOfAntag<Antagonists.Wizard>();
		}

		public Spell GetReaderSpellInstance(SpellData spell)
		{
			return GetLastReader().Script.mind.GetSpellInstance(spell);
		}

		public int GetReaderSpellLevel(SpellData spell)
		{
			Spell spellInstance = GetReaderSpellInstance(spell);
			if (spellInstance == null)
			{
				return default;
			}

			return spellInstance.CurrentTier;
		}

		public bool ReaderSpellsConflictWith(SpellBookSpell spell)
		{
			foreach (SpellBookSpell entry in spell.ConflictsWith)
			{
				if (GetLastReader().Script.mind.Spells.Any(s => s.SpellData == entry.Spell)) return true;
			}

			return false;
		}
		/// <summary>
		/// Gets the latest player to interact with tab.
		/// </summary>
		public ConnectedPlayer GetLastReader()
		{
			return netTab.LastInteractedPlayer().Player();
		}
	}
}
