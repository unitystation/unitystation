using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Items.Bureaucracy
{
	[RequireComponent(typeof(ItemStorage))]
	public class PaperBin : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerSpawn
	{

		public int initialPaperCount = 20;
		public GameObject blankPaper;

		[SyncVar (hook = nameof(SyncPaperCount))]
		private int paperCount;

		[SyncVar (hook = nameof(SyncStoredPen))]
		private GameObject storedPen;

		private ItemStorage itemStorage;
		private ItemSlot penSlot;

		private SpriteRenderer binRenderer;
		private SpriteRenderer penRenderer;

		public Sprite binEmpty;
		public Sprite binLoaded;

		#region Networking and Sync

		private void SyncPaperCount(int oldCount, int newCount)
		{
			EnsureInit();
			paperCount = newCount;
			UpdateSpriteState();
		}

		private void SyncStoredPen(GameObject oldPen, GameObject pen)
		{
			EnsureInit();
			storedPen = pen;
			UpdateSpriteState();
		}

		public override void OnStartClient()
		{
			EnsureInit();
			SyncEverything();
		}

		public override void OnStartServer()
		{
			EnsureInit();
			SyncEverything();
		}

		private void SyncEverything()
		{
			var renderers = GetComponentsInChildren<SpriteRenderer>();
			binRenderer = renderers[0];
			penRenderer = renderers[1];

			SyncPaperCount(paperCount, paperCount);
			SyncStoredPen(storedPen, storedPen);
		}

		private void Awake()
		{
			EnsureInit();
		}

		private void EnsureInit()
		{
			if (itemStorage != null) return;
			itemStorage = GetComponent<ItemStorage>();
			penSlot = itemStorage.GetNamedItemSlot(NamedSlot.storage01);
			SetupInitialValues();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			SetupInitialValues();
		}

		private void SetupInitialValues()
		{
			paperCount = initialPaperCount;
			storedPen = null;

			SyncEverything();
		}

		#endregion

		#region Interactions

		public void OnExamine()
		{
			var count = PaperCount();
			var message = "It doesn't contain any paper.";

			if (count > 0)
			{
				if (count == 1)
				{
					message = "It contains one piece of paper.";
				}
				else if (count > 1)
				{
					message = "It contains " + count + " total pieces of paper.";
				}

				if (storedPen)
				{
					message += " A pen sits on top.";
				}
			}
			else if (storedPen)
			{
				message += " There is a pen inside.";
			}

			Chat.AddExamineMsgToClient(message);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side))
			{
				return false;
			}

			var ps = interaction.Performer.GetComponent<PlayerScript>();
			var cnt = GetComponent<CustomNetTransform>();
			if (!ps || !cnt || !ps.IsRegisterTileReachable(cnt.RegisterTile, side == NetworkSide.Server))
			{
				return false;
			}

			if (interaction.HandObject != null)
			{
				// If hand slot contains paper, we can place it into the bin.
				if (interaction.HandObject.GetComponent<Paper>())
				{
					return true;
				}

				// If hand slot contains a pen, we can try to put it in the bin.
				if (!storedPen && interaction.HandObject.GetComponent<Pen>())
				{
					return true;
				}

				// If we're not putting paper in the bin, we're picking it up- make sure the hand is empty
				return false;
			}

			return true;
		}

		// Interaction when clicking the bin
		public void ServerPerformInteraction(HandApply interaction)
		{
			var handObj = interaction.HandObject;
			if (handObj == null)
			{
				var pna = interaction.Performer.GetComponent<PlayerNetworkActions>();

				// Pen comes out before the paper
				if (storedPen)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "You take the pen out of the paper bin.");
					Inventory.ServerTransfer(penSlot, interaction.HandSlot);
					SyncStoredPen(storedPen, null);
					return;
				}

				// Player is picking up a piece of paper
				if (!HasPaper())
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "The paper bin is empty!");
					return;
				}

				Chat.AddExamineMsgFromServer(interaction.Performer, "You take the paper out of the paper bin.");
				if (!HasPaper())
				{
					throw new InvalidOperationException();
				}

				// First, get the papers that players have put in the bin
				var occupiedSlot = itemStorage.GetTopOccupiedIndexedSlot();
				if (occupiedSlot != null)
				{
					//remove it from the slot
					Inventory.ServerTransfer(occupiedSlot, interaction.HandSlot);
				}
				else // Otherwise, take from blank paper stash
				{

					var paper = Spawn.ServerPrefab(blankPaper).GameObject;
					var targetSlot = itemStorage.GetNextFreeIndexedSlot();
					Inventory.ServerAdd(paper, interaction.HandSlot);
				}

				SyncPaperCount(paperCount, paperCount - 1);
			}
			else
			{
				// Player is adding a piece of paper or a pen
				if (handObj.GetComponent<Pen>())
				{
					SyncStoredPen(storedPen, handObj);
					Chat.AddExamineMsgFromServer(interaction.Performer, "You put the pen in the paper bin.");
					Inventory.ServerTransfer(interaction.HandSlot, penSlot);
					return;
				}

				var freeSlot = itemStorage.GetNextFreeIndexedSlot();
				if (freeSlot == null)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "The bin is full.");
				}
				Chat.AddExamineMsgFromServer(interaction.Performer, "You put the paper in the paper bin.");
				Inventory.ServerTransfer(interaction.HandSlot, freeSlot);
				SyncPaperCount(paperCount, paperCount + 1);
			}
		}

		#endregion

		private bool HasPaper()
		{
			return paperCount > 0;
		}

		private int PaperCount()
		{
			return paperCount;
		}


		private void UpdateSpriteState()
		{
			if (binRenderer)
			{
				binRenderer.sprite = HasPaper() ? binLoaded : binEmpty;
			}

			if (penRenderer)
			{
				penRenderer.sprite = storedPen ? storedPen.GetComponentInChildren<SpriteRenderer>().sprite : null;
			}
		}
	}
}