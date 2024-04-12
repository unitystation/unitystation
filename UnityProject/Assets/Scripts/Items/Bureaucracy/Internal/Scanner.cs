using System;
using UnityEngine;

namespace Items.Bureaucracy.Internal
{
	public class Scanner
	{
		public bool ScannerOpen { get; }
		public bool ScannedText { get; }

		public ItemStorage Storage { get; }

		public Scanner(bool scannerOpen, ItemStorage storage, bool scannedText)
		{
			ScannerOpen = scannerOpen;
			Storage = storage;
			ScannedText = scannedText;
		}

		public Scanner ToggleScannerLid()
		{
			if (ScannerOpen)
			{
				Chat.AddLocalMsgToChat("The Scanner's lid closes, and no longer accepts any more new documents to scan.", Storage.gameObject);
				return new Scanner(false, Storage, ScannedText);
			}
			else
			{
				DropAllContent();
				return new Scanner(true, Storage, false);
			}
		}

		private void DropAllContent()
		{
			Storage.ServerDropAll();
			Chat.AddLocalMsgToChat("The Scanner spits out all the original documents.", Storage.gameObject);
		}

		public bool CanPlaceDocument(GameObject page)
		{
			if (page == null) return false;
			if (page.TryGetComponent<Paper>(out var _) == false) return false;
			return ScannerOpen;
		}

		public void PlaceDocument(GameObject paperObj)
		{
			if (CanPlaceDocument(paperObj) == false)
			{
				throw new InvalidOperationException("Cannot place document in scanner");
			}

			var result = Storage.ServerTryTransferFrom(paperObj);
			Chat.AddActionMsgToChat(Storage.gameObject, result ?
				"The Scanner queues a document up for printing.." : "The Scanner refuses accepting the document..");
		}

		public bool CanScan()
		{
			if (ScannerOpen)
			{
				Chat.AddActionMsgToChat(Storage.gameObject, "The scanner blips 'Error: Scanner is open'");
				return false;
			}
			if (Storage.GetOccupiedSlots().Count == 0)
			{
				Chat.AddActionMsgToChat(Storage.gameObject, "The scanner blips 'Error: Nothing to scan, scanner empty.'");
				return false;
			}
			return true;
		}

		public Scanner Scan()
		{
			if (CanScan() == false)
			{
				return new Scanner(ScannerOpen, Storage, false);
			}

			return new Scanner(ScannerOpen, Storage, true);
		}

		public Scanner ClearScannedText()
		{
			DropAllContent();
			return new Scanner(ScannerOpen, Storage, false);
		}
	}
}
