﻿using System;
using System.Collections;
using Assets.Scripts.Items.Bureaucracy;

namespace Assets.Scripts.UI.Bureaucracy
{
	public class GUI_Photocopier : NetTab
	{
		private Photocopier Photocopier { get; set; }
		private RegisterObject registerObject;

		private readonly NetLabel _statusLabel = null;
		private NetLabel StatusLabel => _statusLabel ? _statusLabel : this["StatusLabel"] as NetLabel;

		private readonly NetLabel _scannerLabel = null;
		private NetLabel ScannerLabel => _scannerLabel ? _scannerLabel : this["ScannerLabel"] as NetLabel;

		private readonly NetLabel _trayLabel = null;
		private NetLabel TrayLabel => _trayLabel ? _trayLabel : this["TrayLabel"] as NetLabel;

		public void Start()
		{
			StartCoroutine(WaitForProviderAndRender());
		}

		private IEnumerator WaitForProviderAndRender()
		{
			while (Provider == null)
				yield return WaitFor.EndOfFrame;

			registerObject = Provider.GetComponent<RegisterObject>();
			Photocopier = Provider.GetComponent<Photocopier>();
			Photocopier.GuiRenderRequired += HandleGuiRenderRequired;
			RenderStatusLabel();
			RenderScannerLabel();
			RenderTrayLabel();
		}

		private void HandleGuiRenderRequired(object sender, EventArgs e)
		{
			RenderStatusLabel();
			RenderScannerLabel();
			RenderTrayLabel();
		}

		private void RenderStatusLabel()
		{
			if (Photocopier.TrayOpen)
			{
				StatusLabel.SetValue = "TRAY OPEN";
			}
			else if (Photocopier.ScannerOpen)
			{
				StatusLabel.SetValue = "SCANNER OPEN";
			}
			else if (Photocopier.TrayCount == 0)
			{
				StatusLabel.SetValue = "TRAY EMPTY";
			}
			else if (Photocopier.ScannedTextNull)
			{
				StatusLabel.SetValue = "DOCUMENT NOT SCANNED";
			}
			else
			{
				StatusLabel.SetValue = "COPIER READY";
			}
		}

		private void RenderScannerLabel()
		{
			if (Photocopier.ScannerOpen)
			{
				ScannerLabel.SetValue = "ERR: SCANNER OPEN";
			}
			else if (!Photocopier.ScannedTextNull)
			{
				ScannerLabel.SetValue = "DOCUMENT SCANNED";
			}
			else if (Photocopier.ScannedTextNull)
			{
				ScannerLabel.SetValue = "DOCUMENT NOT SCANNED";
			}
		}

		private void RenderTrayLabel()
		{
			if (Photocopier.TrayOpen)
			{
				TrayLabel.SetValue = "ERR: TRAY OPEN";
			}
			else
			{
				TrayLabel.SetValue = $"PAGES IN TRAY: {Photocopier.TrayCount}/{Photocopier.TrayCapacity}";
			}
		}

		public void OpenTray()
		{
			if (!Photocopier.TrayOpen)
			{
				SoundManager.PlayNetworkedAtPos("Beep", registerObject.WorldPosition);
				Photocopier.ToggleTray();
			}
		}

		public void OpenScanner()
		{
			if (!Photocopier.ScannerOpen)
			{
				SoundManager.PlayNetworkedAtPos("Beep", registerObject.WorldPosition);
				Photocopier.ToggleScannerLid();
			}
		}

		public void Print()
		{
			if (Photocopier.CanPrint())
			{
				SoundManager.PlayNetworkedAtPos("Beep", registerObject.WorldPosition);
				Photocopier.Print();
			}
		}

		public void Scan()
		{
			if (Photocopier.CanScan())
			{
				SoundManager.PlayNetworkedAtPos("Beep", registerObject.WorldPosition);
				Photocopier.Scan();
			}
		}

		public void ClearScannedText() => Photocopier.ClearScannedText();
	}
}