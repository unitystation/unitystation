using System;
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

		public Printer(int trayCount, int trayCapacity, bool trayOpen)
		{
			TrayCount = trayCount;
			TrayCapacity = trayCapacity;
			TrayOpen = trayOpen;
		}

		public Printer ToggleTray()
		{
			return new Printer(TrayCount, TrayCapacity, !TrayOpen);
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
			return new Printer(TrayCount + 1, TrayCapacity, TrayOpen);
		}

		public bool CanPrint(string content, bool isAvailableForPrinting) =>
			content != null
			&& !TrayOpen
			&& TrayCount > 0
			&& isAvailableForPrinting;

		public Printer Print(string content, GameObject printerObj, bool isAvailableForPrinting, GameObject paperPrefab)
		{
			if (!CanPrint(content, isAvailableForPrinting))
				throw new InvalidOperationException("Cannot print");

			var result = Spawn.ServerPrefab(paperPrefab, SpawnDestination.At(printerObj));
			if (!result.Successful)
				throw new InvalidOperationException("Spawn paper failed!");

			var paperObj = result.GameObject;
			var paper = paperObj.GetComponent<Paper>();
			paper.SetServerString(content);
			return new Printer(TrayCount - 1, TrayCapacity, TrayOpen);
		}
	}
}
