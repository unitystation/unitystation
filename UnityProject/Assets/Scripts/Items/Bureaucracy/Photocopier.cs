using System;
using UnityEngine;
using Mirror;
using System.Collections;
using AddressableReferences;
using Messages.Server;
using static Items.Bureaucracy.Photocopier;

namespace Items.Bureaucracy
{
	public class Photocopier : NetworkBehaviour, ICheckedInteractable<HandApply>, IRightClickable
	{
		public NetTabType NetTabType;
		public int trayCapacity;

		private Internal.Printer printer;
		private Internal.Scanner scanner;

		[field: SyncVar(hook = nameof(SyncPhotocopierState))]
		public PhotocopierState photocopierState { get; private set; } = PhotocopierState.Idle;

		[SerializeField] private GameObject paperPrefab = null;

		[SerializeField] private SpriteHandler spriteHandler = null;
		private RegisterObject registerObject;

		[SerializeField] private AddressableAudioSource Copier = null;
		[SerializeField] private ItemStorage inkStorage;
		[SerializeField] private ItemTrait tonerTrait;
		public Toner InkCartadge => inkStorage.GetTopOccupiedIndexedSlot()?.ItemObject.GetComponent<Toner>();


		private void Awake()
		{
			photocopierState = PhotocopierState.Idle;
			registerObject = gameObject.GetComponent<RegisterObject>();
			printer = new Internal.Printer(0, trayCapacity, false);
			scanner = new Internal.Scanner(false, true, null, null);
			if (inkStorage == null) inkStorage = GetComponent<ItemStorage>();
		}

		public enum PhotocopierState
		{
			Idle = 0,
			TrayOpen = 1,
			ScannerOpen = 2,
			Production = 3
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
			if (!DefaultWillInteract.Default(interaction, side))
			{
				return false;
			}

			if (interaction.UsedObject != null && interaction.UsedObject.Item().HasTrait(tonerTrait)) return true;

			if (photocopierState != PhotocopierState.Production && interaction.HandObject == null) return true;
			else if ((photocopierState == PhotocopierState.TrayOpen || photocopierState == PhotocopierState.ScannerOpen) && interaction.HandObject != null) return interaction.HandObject.TryGetComponent<Paper>(out var paper);
	
			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (InkCartadge == null && interaction.UsedObject != null && interaction.UsedObject.Item().HasTrait(tonerTrait))
			{
				Inventory.ServerTransfer(interaction.UsedObject.GetComponent<Pickupable>().ItemSlot,
					inkStorage.GetNextFreeIndexedSlot());
				return;
			}
			if (interaction.HandObject == null)
			{
				if (printer.TrayOpen)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "You close the tray.");
					ToggleTray();
				}
				else if (scanner.ScannerOpen)
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
			else if (printer.CanAddPageToTray(interaction.HandObject))
			{
				printer = printer.AddPageToTray(interaction.HandObject);
				Chat.AddExamineMsgFromServer(interaction.Performer, "You place the sheet in the tray.");
			}
			else if (scanner.CanPlaceDocument(interaction.HandObject))
			{
				scanner = scanner.PlaceDocument(interaction.HandObject);
				Chat.AddExamineMsgFromServer(interaction.Performer, "You place the document in the scanner.");
			}
		}

		private void RemoveInkCartridge()
		{
			inkStorage.ServerDropAll();
		}

		#endregion Interactions

		#region Component API

		/*
		 * The following methods and properties are the API for this component.
		 * Other components (notably the GUI_Photocopier tab) can call these methods.
		 */

		public int TrayCount => printer.TrayCount;
		public int TrayCapacity => printer.TrayCapacity;
		public bool TrayOpen => printer.TrayOpen;
		public bool ScannerOpen => scanner.ScannerOpen;
		public bool ScannedTextNull => scanner.ScannedText == null;

		[Server]
		public void ToggleTray()
		{
			printer = printer.ToggleTray();
			photocopierState = printer.TrayOpen ? PhotocopierState.TrayOpen : PhotocopierState.Idle;

			OnGuiRenderRequired();
		}

		[Server]
		public void ToggleScannerLid()
		{
			scanner = scanner.ToggleScannerLid(gameObject, paperPrefab);
			photocopierState = scanner.ScannerOpen ? PhotocopierState.ScannerOpen : PhotocopierState.Idle;

			OnGuiRenderRequired();
		}

		public bool CanPrint() => printer.CanPrint(scanner.ScannedText, photocopierState == PhotocopierState.Idle) && InkCartadge.CheckInkLevel();

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
			printer = printer.Print(scanner.ScannedText, gameObject, photocopierState == PhotocopierState.Idle, paperPrefab);
			OnGuiRenderRequired();
		}

		public bool CanScan() => scanner.CanScan();

		[Server]
		public void Scan()
		{
			photocopierState = PhotocopierState.Production;
			InkCartadge.SpendInk();
			StartCoroutine(WaitForScan());
		}

		private IEnumerator WaitForScan()
		{
			yield return WaitFor.Seconds(4f);
			photocopierState = PhotocopierState.Idle;
			scanner = scanner.Scan();
			OnGuiRenderRequired();
		}

		[Server]
		public void ClearScannedText()
		{
			scanner = scanner.ClearScannedText();
			OnGuiRenderRequired();
		}

		#endregion Component API

		/// <summary>
		/// GUI_Photocopier subscribes to this event when it is initialized.
		/// The event is triggered when Photocopier decides a GUI render is required. (something changed)
		/// </summary>
		public event EventHandler GuiRenderRequired;

		private void OnGuiRenderRequired() => GuiRenderRequired?.Invoke(gameObject, new EventArgs());

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = new RightClickableResult();
			if (InkCartadge == null) return result;
			result.AddElement("Remove Ink Cart", RemoveInkCartridge);
			return result;
		}
	}
}
