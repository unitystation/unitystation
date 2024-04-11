using System;
using UnityEngine;

namespace Items.Bureaucracy.Internal
{
	public class Scanner
	{
		public bool ScannerOpen { get; }
		public bool ScannerEmpty { get; }
		public string ScannedText { get; }

		public ItemStorage Storage { get; }

		public Scanner(bool scannerOpen = false, bool scannerEmpty = true, string scannedText = "", ItemStorage storage = null)
		{
			ScannerOpen = scannerOpen;
			ScannerEmpty = scannerEmpty;
			ScannedText = scannedText;
			Storage = storage;
		}

		public Scanner ToggleScannerLid(GameObject scannerObj, GameObject paperPrefab)
		{
			if (ScannerOpen)
			{
				return new Scanner(false, ScannerEmpty, ScannedText, Storage);
			}
			else
			{
				//If we're opening the scanner and it aint empty spawn paper
				if (ScannerEmpty == false)
				{
					Storage.ServerDropAll();
					Chat.AddLocalMsgToChat("The Scanner spits out all the original documents.", Storage.gameObject);
					return new Scanner(true, true, ScannedText, Storage);
				}
				else
				{
					return new Scanner(true, ScannerEmpty, ScannedText, Storage);
				}
			}
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

			if (Storage.ServerTryAdd(paperObj))
			{
				Chat.AddLocalMsgToChat("The Scanner queues a document up for printing..", Storage.gameObject);
			}
			else
			{
				Chat.AddLocalMsgToChat("The Scanner refuses accepting the document..", Storage.gameObject);
			}
		}

		public bool CanScan() => !ScannerOpen && !ScannerEmpty;

		public Scanner Scan()
		{
			if (CanScan() == false)
			{
				throw new InvalidOperationException("Cannot scan document");
			}

			var combinedText = "";

			foreach (var page in Storage.GetOccupiedSlots())
			{
				combinedText += page.ItemObject.GetComponent<Paper>()?.ServerString;
			}

			return new Scanner(ScannerOpen, ScannerEmpty, combinedText, Storage);
		}

		public Scanner ClearScannedText()
		{
			return new Scanner(ScannerOpen, ScannerEmpty, "", Storage);
		}
	}
}
