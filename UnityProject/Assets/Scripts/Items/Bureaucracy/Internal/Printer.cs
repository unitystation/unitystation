using System;
using System.Collections.Generic;
using Logs;
using UnityEngine;

namespace Items.Bureaucracy.Internal
{
	/// <summary>
	/// An immutable class representing an internal Printer that might be part of copiers, scanners, other office equipment.
	/// </summary>
	public class Printer
	{
		public int TrayCount { get; }
		public int TrayCapacity { get; }
		public bool TrayOpen { get; }
		public bool PrintBook { get; set; } = false;

		public Printer(int trayCount, int trayCapacity, bool trayOpen, bool printBook)
		{
			TrayCount = trayCount;
			TrayCapacity = trayCapacity;
			TrayOpen = trayOpen;
			PrintBook = printBook;
		}

		public Printer ToggleTray()
		{
			return new Printer(TrayCount, TrayCapacity, !TrayOpen, PrintBook);
		}

		public bool CanAddPageToTray(GameObject page) =>
			page != null
			&& page.GetComponent<Paper>() != null
			&& TrayOpen
			&& TrayCount < TrayCapacity;

		public Printer AddPageToTray(GameObject paperObj)
		{
			if (CanAddPageToTray(paperObj) == false)
				throw new InvalidOperationException("Cannot place page in tray");

			_ = Despawn.ServerSingle(paperObj);
			return new Printer(TrayCount + 1, TrayCapacity, TrayOpen, PrintBook);
		}

		public bool CanPrint(Scanner content, bool isAvailableForPrinting)
		{
			if (content == null)
			{
				return false;
			}
			if (content.ScannedText == false)
			{
				Chat.AddActionMsgToChat(content.Storage.gameObject, "The Printer bleeps 'Error! No documents have been scanned yet to print!'");
				return false;
			}
			if (TrayOpen)
			{
				Chat.AddActionMsgToChat(content.Storage.gameObject, "The Printer bleeps 'Error! Printer is open!'");
				return false;
			}
			if (TrayCount == 0)
			{
				Chat.AddActionMsgToChat(content.Storage.gameObject, "The Printer bleeps 'Error! Tray is empty!'");
				return false;
			}
			if (PrintBook && TrayCount < content.Storage.GetOccupiedSlots().Count)
			{
				Chat.AddActionMsgToChat(content.Storage.gameObject, "The Printer bleeps 'Error! Not enough pages to print a book!'");
				return false;
			}
			return isAvailableForPrinting;
		}

		public Printer Print(Scanner content, GameObject printerObj, GameObject bookObk, bool isAvailableForPrinting, GameObject paperPrefab)
		{
			if (CanPrint(content, isAvailableForPrinting) == false)
			{
				return this;
			}
			var newTrayCount = TrayCount - content.Storage.GetOccupiedSlots().Count;;
			if (PrintBook)
			{
				MakeBook(content, printerObj, bookObk);
			}
			else
			{
				MakePhotoCopy(content, printerObj, paperPrefab);
			}
			return new Printer(newTrayCount, TrayCapacity, TrayOpen, PrintBook);
		}

		private void MakeBook(Scanner content, GameObject printerObj, GameObject bookPrefab)
		{
			var result = Spawn.ServerPrefab(bookPrefab, SpawnDestination.At(printerObj));
			if (!result.Successful)
			{
				throw new InvalidOperationException("Spawn paper failed!");
			}
			var paperObj = result.GameObject;
			var book = paperObj.GetComponent<BookWritable>();
			var papers = new List<Paper>();
			foreach (var slot in content.Storage.GetOccupiedSlots())
			{
				papers.Add(slot.ItemObject.GetComponent<Paper>());
			}
			book.Setup(papers, "Book", "A freshly printed book.");
		}

		private void MakePhotoCopy(Scanner content, GameObject printerObj, GameObject paperPrefab)
		{
			foreach (var copySlot in content.Storage.GetOccupiedSlots())
			{
				if (copySlot.ItemObject.TryGetComponent<Paper>(out var ogPaper) == false) continue;
				var result = Spawn.ServerPrefab(paperPrefab, SpawnDestination.At(printerObj));
				if (result.Successful == false)
				{
					Loggy.LogError("[Photocopier/Printer/MakePhotoCopy()] - Spawn paper failed!");
					continue;
				}
				var paperObj = result.GameObject;
				var paper = paperObj.GetComponent<Paper>();
				paper.SetServerString(ogPaper.ServerString);
			}
		}
	}
}
