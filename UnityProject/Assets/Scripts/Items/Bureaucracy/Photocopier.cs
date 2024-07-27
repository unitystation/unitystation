using System;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using Items.Bureaucracy.Internal;
using Logs;
using Messages.Server;
using UnityEngine.Serialization;
using static Items.Bureaucracy.Photocopier;

namespace Items.Bureaucracy
{
	public class Photocopier : NetworkBehaviour, ICheckedInteractable<HandApply>, IRightClickable
	{
		public NetTabType NetTabType;
		public int trayCapacity;

		[field: SyncVar(hook = nameof(SyncPhotocopierState))]
		public PhotocopierState photocopierState { get; private set; } = PhotocopierState.Idle;

		[SerializeField] private GameObject paperPrefab = null;
		[SerializeField] private GameObject bookPrefab = null;

		[SerializeField] private SpriteHandler spriteHandler = null;
		private RegisterObject registerObject;

		[SerializeField] private ItemTrait tonerTrait;

		[SerializeField] private AddressableAudioSource Copier = null;
		[SerializeField] private ItemStorage inkStorage;
		[FormerlySerializedAs("paperStorage")] [SerializeField] private ItemStorage ScannerStorage;
		[SerializeField] private ItemStorage PaperTrayStorage;
		public Toner TonerCartadge => inkStorage.GetTopOccupiedIndexedSlot()?.ItemObject.GetComponent<Toner>();


		public bool trayOpen = false;
		public bool scannerOpen = false;

		public bool hasScanned;

		public int TrayCount => PaperTrayStorage.GetOccupiedSlots().Count;
		public int TrayCapacity => trayCapacity;
		public bool TrayOpen => trayOpen;
		public bool ScannerOpen => scannerOpen;

		public bool HasScanned => hasScanned;

		public bool PrintBook = false;

		public bool CanAddPageToTray(GameObject page) =>
			page != null
			&& page.GetComponent<Paper>() != null
			&& TrayOpen
			&& TrayCount < TrayCapacity;

		public bool CanPlaceDocument(GameObject page)
		{
			if (page == null) return false;
			if (page.TryGetComponent<Paper>(out var _) == false) return false;
			return ScannerOpen;
		}

		/// <summary>
		/// GUI_Photocopier subscribes to this event when it is initialized.
		/// The event is triggered when Photocopier decides a GUI render is required. (something changed)
		/// </summary>
		public event EventHandler GuiRenderRequired;

		public enum PhotocopierState
		{
			Idle = 0,
			TrayOpen = 1,
			ScannerOpen = 2,
			Production = 3
		}


		private void Awake()
		{
			photocopierState = PhotocopierState.Idle;
			registerObject = gameObject.GetComponent<RegisterObject>();
		}

		#region Sprite Sync

		private void SyncPhotocopierState(PhotocopierState oldState, PhotocopierState newState)
		{
			photocopierState = newState;
			switch (newState)
			{
				case PhotocopierState.ScannerOpen:
				case PhotocopierState.TrayOpen:
				case PhotocopierState.Idle:
					spriteHandler.SetCatalogueIndexSprite(0);
					break;

				case PhotocopierState.Production:
					spriteHandler.SetCatalogueIndexSprite(1);
					break;
			}
		}

		#endregion Sprite Sync

		#region Interactions

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false)
			{
				return false;
			}
			if (interaction.UsedObject != null && interaction.UsedObject.Item().HasTrait(tonerTrait)) return true;
			if (photocopierState != PhotocopierState.Production && interaction.HandObject == null) return true;
			if (photocopierState is PhotocopierState.TrayOpen or PhotocopierState.ScannerOpen && interaction.HandObject != null)
			{
				return interaction.HandObject.TryGetComponent<Paper>(out var paper);
			}

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (TonerCartadge == null && interaction.UsedObject != null && interaction.UsedObject.Item().HasTrait(tonerTrait))
			{
				Inventory.ServerTransfer(interaction.UsedObject.GetComponent<Pickupable>().ItemSlot,
					inkStorage.GetNextFreeIndexedSlot());
				return;
			}
			if (interaction.HandObject == null)
			{
				if (TrayOpen)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "You close the tray.");
					ToggleTray();
				}
				else if (ScannerOpen)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "You close the scanner lid.");
					ToggleScannerLid();
				}
				else
				{
					OnGuiRenderRequired();
					TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType, TabAction.Open);
				}
			}
			else if (CanAddPageToTray(interaction.HandObject))
			{
				Inventory.ServerTransfer(interaction.HandSlot, PaperTrayStorage.GetNextFreeIndexedSlot());
				Chat.AddExamineMsgFromServer(interaction.Performer, "You place the sheet in the tray.");
			}
			else if (CanPlaceDocument(interaction.HandObject))
			{
				hasScanned = false;
				var result = ScannerStorage.ServerTryTransferFrom(interaction.HandObject);
				Chat.AddActionMsgToChat(gameObject, result ? "The Scanner queues a document up for printing.." : "The Scanner refuses accepting the document..");
				Chat.AddExamineMsgFromServer(interaction.Performer, "You place the document in the scanner.");
			}
		}

		private void RemoveInkCartridge()
		{
			inkStorage.ServerDropAll();
		}

		#endregion Interactions

		#region Component API

		[Server]
		public void ToggleTray()
		{
			trayOpen = !trayOpen;
			photocopierState = TrayOpen ? PhotocopierState.TrayOpen : PhotocopierState.Idle;

			OnGuiRenderRequired();
		}

		[Server]
		public void ToggleScannerLid()
		{
			hasScanned = false;
			scannerOpen = !scannerOpen;
			photocopierState = scannerOpen ? PhotocopierState.ScannerOpen : PhotocopierState.Idle;

			if (photocopierState is PhotocopierState.ScannerOpen)
			{
				ScannerStorage.ServerDropAll();
			}

			OnGuiRenderRequired();
		}

		public bool CanPrint() => CanPrint(photocopierState == PhotocopierState.Idle) && TonerCartadge.CheckInkLevel();

		public bool CanPrint(bool isAvailableForPrinting)
		{

			if (HasScanned == false)
			{
				Chat.AddActionMsgToChat(gameObject, "The Printer bleeps 'Error! No documents have been scanned yet to print!'");
				return false;
			}
			if (TrayOpen)
			{
				Chat.AddActionMsgToChat(gameObject, "The Printer bleeps 'Error! Printer is open!'");
				return false;
			}
			if (TrayCount == 0)
			{
				Chat.AddActionMsgToChat(gameObject, "The Printer bleeps 'Error! Tray is empty!'");
				return false;
			}
			if (PrintBook && TrayCount < ScannerStorage.GetOccupiedSlots().Count)
			{
				Chat.AddActionMsgToChat(gameObject, "The Printer bleeps 'Error! Not enough pages to print a book!'");
				return false;
			}
			return isAvailableForPrinting;
		}


		[Server]
		public void Print()
		{
			photocopierState = PhotocopierState.Production;
			SoundManager.PlayNetworkedAtPos(Copier, registerObject.WorldPosition);
			StartCoroutine(WaitForPrint());
		}

		private IEnumerator WaitForPrint()
		{
			yield return WaitFor.Seconds(4f);
			photocopierState = PhotocopierState.Idle;
			Print(gameObject, bookPrefab, photocopierState == PhotocopierState.Idle, paperPrefab);
			OnGuiRenderRequired();
		}

		public void Print(GameObject printerObj, GameObject bookObk, bool isAvailableForPrinting, GameObject paperPrefab)
		{
			if (CanPrint(isAvailableForPrinting) == false)
			{
				return ;
			}

			if (PrintBook)
			{
				MakeBook(printerObj, bookObk);
			}
			else
			{
				MakePhotoCopy();
			}
			return;
		}

		private void MakePhotoCopy()
		{
			foreach (var copySlot in ScannerStorage.GetOccupiedSlots())
			{
				if (copySlot.ItemObject.TryGetComponent<Paper>(out var ogPaper) == false) continue;
				var paperSlot =  PaperTrayStorage.GetFirstOccupiedSlot();
				var paperObj = paperSlot.Item.gameObject;
				var paper = paperObj.GetComponent<Paper>();
				paper.SetServerString(ogPaper.ServerString); //TODO Funny effect with paper Writing Over Already printed text
				Inventory.ServerDrop(paperSlot);
			}
		}


		private void MakeBook(GameObject printerObj, GameObject bookPrefab)
		{
			var result = Spawn.ServerPrefab(bookPrefab, SpawnDestination.At(printerObj));
			if (!result.Successful)
			{
				throw new InvalidOperationException("Spawn paper failed!");
			}
			var paperObj = result.GameObject;
			var book = paperObj.GetComponent<BookWritable>();
			var papers = new List<Paper>();
			foreach (var slot in ScannerStorage.GetOccupiedSlots())
			{
				papers.Add(slot.ItemObject.GetComponent<Paper>());
				var paper =  PaperTrayStorage.GetFirstOccupiedSlot();
				Inventory.ServerDespawn(paper);
			}
			book.Setup(papers, "Book", "A freshly printed book.");
		}



		public bool CanScan()
		{
			if (ScannerOpen)
			{
				Chat.AddActionMsgToChat(gameObject, "The scanner blips 'Error: Scanner is open'");
				return false;
			}
			if (ScannerStorage.GetOccupiedSlots().Count == 0)
			{
				Chat.AddActionMsgToChat(gameObject, "The scanner blips 'Error: Nothing to scan, scanner empty.'");
				return false;
			}
			return true;
		}


		[Server]
		public void Scan()
		{
			photocopierState = PhotocopierState.Production;
			StartCoroutine(WaitForScan());
		}

		private IEnumerator WaitForScan()
		{
			yield return WaitFor.Seconds(4f);
			photocopierState = PhotocopierState.Idle;
			hasScanned = true;
			OnGuiRenderRequired();
		}

		[Server]
		public void ClearScannedText()
		{
			hasScanned = false;
			OnGuiRenderRequired();
		}

		[Command(requiresAuthority = false)]
		public void SwitchPrintingMode() //TODO Client validation!!!!!!!!!!
		{
			PrintBook = !PrintBook;
			var printingMode = PrintBook ? "Books" : "Copies of Paper";
			Chat.AddLocalMsgToChat($"The printer will now print {printingMode}", gameObject);
		}

		#endregion Component API

		private void OnGuiRenderRequired() => GuiRenderRequired?.Invoke(gameObject, new EventArgs());

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = new RightClickableResult();
			result.AddElement("Switch Printing Mode", SwitchPrintingMode);
			if (TonerCartadge == null) return result;
			result.AddElement("Remove Ink Cart", RemoveInkCartridge);
			return result;
		}
	}
}
