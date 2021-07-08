using Mirror;
using UnityEngine;
using WebSocketSharp;

namespace Items.Cargo
{
	public class NoteAttachable: NetworkBehaviour,
		ICheckedInteractable<HandApply>,
		ICheckedInteractable<InventoryApply>,
		ICheckedInteractable<ContextMenuApply>,
		IRightClickable,
		IExaminable,
		IServerSpawn
	{
		[SerializeField][Tooltip("Set this if you want this object to spawn with a note attached to it")]
		private string initialNoteText;

		[SyncVar(hook=nameof(SyncNoteText))]
		private string noteText;

		[SerializeField][Tooltip("Reference to the paper game object, needed for spawning notes when detaching")]
		private GameObject paperPrefab;

		/// <summary>
		/// Attaches a note to the package. This will modify its examine message.
		/// This method should only be called from server.
		/// </summary>
		/// <param name="performer"></param>
		/// <param name="text"></param>
		private void AttachNote(GameObject performer, string text)
		{
			if (noteText.IsNullOrEmpty() == false && performer != null)
			{
				Chat.AddExamineMsg(performer, $"{gameObject.ExpensiveName()} already has a note attached to it!");
				return;
			}

			ServerSetNoteText(text);
		}

		private void DetachNote(GameObject performer)
		{
			var note = Spawn.ServerPrefab(paperPrefab, gameObject.AssumedWorldPosServer()).GameObject;
			note.GetComponent<Paper>().SetServerString(noteText);
			ServerSetNoteText(string.Empty);

			Chat.AddActionMsgToChat(performer,
				$"You detach the note from {gameObject.ExpensiveName()}",
				$"{performer.ExpensiveName()} detaches the note from {gameObject.ExpensiveName()}");
		}

		private bool CommonWillInteract(TargetedInteraction interaction)
		{
			return interaction.TargetObject == gameObject &&
					interaction.UsedObject != null &&
					interaction.UsedObject.TryGetComponent<Paper>(out var paper) &&
					noteText.IsNullOrEmpty() &&
					(paper.ServerString.IsNullOrEmpty() == false || paper.PaperString.IsNullOrEmpty() == false);
					// Server uses ServerString, clients; PaperString.
		}

		private void SyncNoteText(string oldVal, string newVal)
		{
			noteText = newVal;
		}

		[Server]
		private void ServerSetNoteText(string newText)
		{
			SyncNoteText(noteText, newText);
		}

		public override void OnStartClient()
		{
			SyncNoteText(noteText, noteText);
		}

		#region Clicked in the world
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side) &&
			       CommonWillInteract(interaction);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var text = interaction.HandObject.GetComponent<Paper>().PaperString;
			AttachNote(interaction.Performer, text);
			Inventory.ServerDespawn(interaction.HandObject);
		}
		#endregion

		#region Clicked in inventory
		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side) &&
			       CommonWillInteract(interaction);
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			var text = interaction.UsedObject.GetComponent<Paper>().PaperString;
			AttachNote(interaction.Performer, text);
			Inventory.ServerDespawn(interaction.UsedObject);
		}
		#endregion

		#region RightClick interaction
		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();
			var detachInteraction = ContextMenuApply.ByLocalPlayer(gameObject, null);
			if (WillInteract(detachInteraction, NetworkSide.Client) == false) return result;
			if (noteText.IsNullOrEmpty()) return result;

			return result.AddElement("Detach note", () => OnDetachClicked(detachInteraction));
		}

		private void OnDetachClicked(ContextMenuApply interaction)
		{
			InteractionUtils.RequestInteract(interaction, this);
		}
		public bool WillInteract(ContextMenuApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side) && noteText.IsNullOrEmpty() == false;
		}

		public void ServerPerformInteraction(ContextMenuApply interaction)
		{
			DetachNote(interaction.Performer);
		}
		#endregion

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			if (noteText.IsNullOrEmpty())
			{
				return "";
			}

			return "It has a note attached to it. It reads: \"" + noteText + "\".";
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (initialNoteText.IsNullOrEmpty())
			{
				return;
			}

			AttachNote(null, initialNoteText);
		}
	}
}
