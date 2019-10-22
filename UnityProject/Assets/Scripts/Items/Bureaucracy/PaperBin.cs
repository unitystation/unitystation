using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Items.Bureaucracy
{
	public class PaperBin : NBHandApplyInteractable, IOnStageServer
	{

		public int initialPaperCount = 20;
		public GameObject blankPaper;

		[SyncVar (hook = nameof(SyncPaperCount))]
		private int paperCount;

		[SyncVar (hook = nameof(SyncStoredPen))]
		private GameObject storedPen;

		private List<GameObject> storedPaper = new List<GameObject>();
		private SpriteRenderer binRenderer;
		private SpriteRenderer penRenderer;
		private Sprite binEmpty;
		private Sprite binLoaded;

		#region Networking and Sync

		private void SyncPaperCount(int newCount)
		{
			paperCount = newCount;
			UpdateSpriteState();
		}

		private void SyncStoredPen(GameObject pen)
		{
			storedPen = pen;
			UpdateSpriteState();
		}

		public override void OnStartClient()
		{
			SyncEverything();
			base.OnStartClient();
		}

		public override void OnStartServer()
		{
			SyncEverything();
			base.OnStartServer();
		}

		private void SyncEverything()
		{
			var renderers = GetComponentsInChildren<SpriteRenderer>();
			binRenderer = renderers[0];
			penRenderer = renderers[1];
			binEmpty = Resources.Load<Sprite>("textures/items/bureaucracy/bureaucracy_paper_bin0");
			binLoaded = Resources.Load<Sprite>("textures/items/bureaucracy/bureaucracy_paper_bin1");

			SyncPaperCount(paperCount);
			SyncStoredPen(storedPen);
		}

		private void Awake()
		{
			SetupInitialValues();
		}

		public void GoingOnStageServer(OnStageInfo info)
		{
			SetupInitialValues();
		}

		private void SetupInitialValues()
		{
			paperCount = initialPaperCount;
			storedPaper.Clear();
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

		protected override bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!base.WillInteract(interaction, side))
			{
				return false;
			}

			var ps = interaction.Performer.GetComponent<PlayerScript>();
			var cnt = GetComponent<CustomNetTransform>();
			if (!ps || !cnt || !ps.IsInReach(cnt.RegisterTile, side == NetworkSide.Server))
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
		protected override void ServerPerformInteraction(HandApply interaction)
		{
			var handObj = interaction.HandObject;
			if (handObj == null)
			{
				var pna = interaction.Performer.GetComponent<PlayerNetworkActions>();

				// Pen comes out before the paper
				if (storedPen)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "You take the pen out of the paper bin.");
					InventoryManager.EquipInInvSlot(pna.Inventory[pna.activeHand], GetStoredPen());
					return;
				}

				// Player is picking up a piece of paper
				if (!HasPaper())
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "The paper bin is empty!");
					return;
				}

				Chat.AddExamineMsgFromServer(interaction.Performer, "You take the paper out of the paper bin.");

				InventoryManager.EquipInInvSlot(pna.Inventory[pna.activeHand], GetPaperFromStack());
			}
			else
			{
				// Player is adding a piece of paper or a pen
				var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer,
					interaction.HandSlot.equipSlot);
				handObj.GetComponent<Pickupable>().DisappearObject(slot);

				if (handObj.GetComponent<Pen>())
				{
					SyncStoredPen(handObj);
					Chat.AddExamineMsgFromServer(interaction.Performer, "You put the pen in the paper bin.");
					return;
				}

				Chat.AddExamineMsgFromServer(interaction.Performer, "You put the paper in the paper bin.");
				AddPaperToStack(handObj);
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

		private void AddPaperToStack(GameObject paper)
		{

			storedPaper.Add(paper);
			SyncPaperCount(paperCount + 1);
		}

		private GameObject GetPaperFromStack()
		{
			GameObject paper;

			if (!HasPaper())
			{
				throw new InvalidOperationException();
			}

			// First, get the papers that players have put in the bin
			if (storedPaper.Count > 0)
			{
				var pos = storedPaper.Count - 1;
				paper = storedPaper[pos];
				storedPaper.RemoveAt(pos);
			}
			else // Otherwise, take from blank paper stash
			{
				paper = PoolManager.PoolNetworkInstantiate(blankPaper);
			}

			SyncPaperCount(paperCount - 1);
			return paper;
		}

		private GameObject GetStoredPen()
		{
			var pen = storedPen;
			SyncStoredPen(null);
			return pen;
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

	class SyncListPaper : SyncList<GameObject> {}
}