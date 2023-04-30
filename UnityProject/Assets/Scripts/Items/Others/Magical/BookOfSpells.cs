using System.Collections;
using System.Linq;
using UnityEngine;
using Systems.Spells;
using ScriptableObjects.Systems.Spells;
using ScriptableObjects.Items.SpellBook;
using InGameEvents;
using AddressableReferences;
using Objects;

namespace Items.Magical
{
	public class BookOfSpells : MonoBehaviour, IExaminable, IServerInventoryMove, ICheckedInteractable<HandActivate>
	{
		[SerializeField] private AddressableAudioSource learningSound = null;

		[SerializeField] private AddressableAudioSource summonItemSound = null;

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

			PlayerInfo player = GetLastReader();

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

		private void LearnSpell(PlayerInfo player, SpellBookSpell spellEntry)
		{
			points -= spellEntry.Cost;

			SoundManager.PlayNetworkedAtPos(learningSound, player.Script.WorldPos, sourceObj: player.GameObject);
			Chat.AddChatMsgToChatServer(player, spellEntry.Incantation, ChatChannel.Local, Loudness.SCREAMING);

			Spell spellInstance = player.Mind.GetSpellInstance(spellEntry.Spell);

			if (spellInstance != null)
			{
				spellInstance.UpgradeTier();
			}
			else
			{
				Spell spell = spellEntry.Spell.AddToPlayer(player.Script.Mind);
				player.Mind.AddSpell(spell);
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
				SoundManager.PlayNetworkedAtPos(summonItemSound, playerScript.WorldPos, sourceObj: playerScript.gameObject);

				var objectContainer = spawnResult.GameObject.GetComponent<ObjectContainer>();

				foreach (GameObject artifactPrefab in artifactEntry.Artifacts)
				{
					spawnResult = Spawn.ServerPrefab(artifactPrefab);
					if (spawnResult.Successful)
					{
						objectContainer.StoreObject(spawnResult.GameObject);
					}
				}
			}
		}

		public void CastRitual(SpellBookRitual ritualEntry)
		{
			if (ritualEntry.Cost > Points) return;

			PlayerInfo player = GetLastReader();

			if (ritualEntry.InvocationMessage != default)
			{
				Chat.AddChatMsgToChatServer(player, ritualEntry.InvocationMessage, ChatChannel.Local, Loudness.LOUD);
			}

			if (ritualEntry.CastSound != default)
			{
				SoundManager.PlayNetworkedAtPos(ritualEntry.CastSound, player.Script.WorldPos, sourceObj: player.GameObject);
			}

			InGameEventsManager.Instance.TriggerSpecificEvent(ritualEntry.EventIndex, ritualEntry.EventType,
				adminName: $"[Wizard] {player.Username}, {player.Name}", announceEvent: false);

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
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			// Return true to stop NetTab interaction.
			return IsRegistered || (isForWizardsOnly && !IsWizard(interaction.Performer.Player()));
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer,
					$"The {gameObject.ExpensiveName()} does not recognise you as its owner and refuses to open!");
		}

		#endregion Interaction

		private bool IsWizard(PlayerInfo player)
		{
			return player.Mind.IsOfAntag<Antagonists.Wizard>();
		}

		public Spell GetReaderSpellInstance(SpellData spell)
		{
			return GetLastReader().Script.Mind.GetSpellInstance(spell);
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
				if (GetLastReader().Script.Mind.Spells.Any(s => s.SpellData == entry.Spell)) return true;
			}

			return false;
		}
		/// <summary>
		/// Gets the latest player to interact with tab.
		/// </summary>
		public PlayerInfo GetLastReader()
		{
			return netTab.LastInteractedPlayer().Player();
		}
	}
}
