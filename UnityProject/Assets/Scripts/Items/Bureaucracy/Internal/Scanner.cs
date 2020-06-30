using System;
using UnityEngine;

namespace Assets.Scripts.Items.Bureaucracy.Internal
{
	public class Scanner
	{
		public bool ScannerOpen { get; }
		public bool ScannerEmpty { get; }
		public string DocumentText { get; }
		public string ScannedText { get; }

		public Scanner(bool scannerOpen, bool scannerEmpty, string documentText, string scannedText)
		{
			ScannerOpen = scannerOpen;
			ScannerEmpty = scannerEmpty;
			DocumentText = documentText;
			ScannedText = scannedText;
		}

		public Scanner ToggleScannerLid(GameObject scannerObj)
		{
			if (ScannerOpen)
			{
				return new Scanner(false, ScannerEmpty, DocumentText, ScannedText);
			}
			else
			{
				//If we're opening the scanner and it aint empty spawn paper
				if (!ScannerEmpty)
				{
					var prefab = Spawn.GetPrefabByName("Paper");
					var result = Spawn.ServerPrefab(prefab, SpawnDestination.At(scannerObj));
					if (!result.Successful)
						throw new InvalidOperationException("Spawn paper failed!");

					var paperObj = result.GameObject;
					var paper = paperObj.GetComponent<Paper>();
					paper.SetServerString(DocumentText);
					return new Scanner(true, true, null, ScannedText);
				}
				else
				{
					return new Scanner(true, ScannerEmpty, DocumentText, ScannedText);
				}
			}
		}

		public bool CanPlaceDocument(GameObject page) =>
			page != null
			&& page.GetComponent<Paper>() != null
			&& ScannerOpen
			&& ScannerEmpty;

		public Scanner PlaceDocument(GameObject paperObj)
		{
			if (!CanPlaceDocument(paperObj))
				throw new InvalidOperationException("Cannot place document in scanner");

			var paper = paperObj.GetComponent<Paper>();
			Despawn.ServerSingle(paperObj);
			return new Scanner(ScannerOpen, false, paper.ServerString, ScannedText);
		}

		public bool CanScan() => !ScannerOpen && !ScannerEmpty && DocumentText != null;

		public Scanner Scan()
		{
			if (!CanScan())
				throw new InvalidOperationException("Cannot scan document");

			return new Scanner(ScannerOpen, ScannerEmpty, DocumentText, DocumentText);
		}

		public Scanner ClearScannedText()
		{
			return new Scanner(ScannerOpen, ScannerEmpty, DocumentText, null);
		}
	}
}