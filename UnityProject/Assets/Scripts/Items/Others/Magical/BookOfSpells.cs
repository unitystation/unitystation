using System.Collections;
using UnityEngine;
using Systems.Spells;
using ScriptableObjects.Items.SpellBook;

namespace Items.Magical
{
	public class BookOfSpells : MonoBehaviour, IExaminable, IServerInventoryMove, ICheckedInteractable<HandActivate>
	{
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

		private void Awake()
		{
			netTab = GetComponent<HasNetworkTabItem>();
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (info.InventoryMoveType != InventoryMoveType.Add) return;
			if (registeredPlayerScript != null) return; // Register to only the first player.

			var player = info.ToRootPlayer;
			if (player == null) return;
			registeredPlayerScript = player.PlayerScript;
		}

		public void LearnSpell(SpellBookSpell spellEntry)
		{
			if (spellEntry.Cost > Points) return;

			points -= spellEntry.Cost;
			
			var player = netTab.LastInteractedPlayer().Player();

			SoundManager.PlayNetworkedAtPos("Blind", player.Script.WorldPos, sourceObj: player.GameObject);
			Chat.AddChatMsgToChat(player, spellEntry.Incantation, ChatChannel.Local);

			Spell spell = spellEntry.Spell.AddToPlayer(player.Script);
			player.Script.mind.AddSpell(spell);
		}

		public void SpawnArtifacts(SpellBookArtifact artifactEntry)
		{
			if (artifactEntry.Cost > Points) return;

			var playerScript = netTab.LastInteractedPlayer().Player().Script;
			var spawnResult = Spawn.ServerPrefab(dropPodPrefab, playerScript.WorldPos);
			if (spawnResult.Successful)
			{
				points -= artifactEntry.Cost;
				SoundManager.PlayNetworkedAtPos("SummonItemsGeneric", playerScript.WorldPos, sourceObj: playerScript.gameObject);

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
			return isForWizardsOnly && !IsWizard(interaction.Performer);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "You're not a wizard, Harry!");
		}

		#endregion Interaction

		private Mind GetMind(GameObject player)
		{
			return player.Player().Script.mind;
		}

		private bool IsWizard(GameObject player)
		{
			var mind = GetMind(player);
			var antagonist = mind.GetAntag().Antagonist;

			return mind.IsAntag && antagonist is Antagonists.Wizard;
		}
	}
}
