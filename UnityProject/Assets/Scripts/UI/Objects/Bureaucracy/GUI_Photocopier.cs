﻿using System;
using System.Collections;
using UnityEngine;
using AddressableReferences;
using UI.Core.NetUI;
using Items.Bureaucracy;

namespace UI.Bureaucracy
{
	public class GUI_Photocopier : NetTab
	{
		[SerializeField] private AddressableAudioSource Beep = null;

		private Photocopier Photocopier { get; set; }
		private RegisterObject registerObject;

		private readonly NetText_label _statusLabel = null;
		private NetText_label StatusLabel => _statusLabel ? _statusLabel : this["StatusLabel"] as NetText_label;

		private readonly NetText_label _scannerLabel = null;
		private NetText_label ScannerLabel => _scannerLabel ? _scannerLabel : this["ScannerLabel"] as NetText_label;

		private readonly NetText_label _trayLabel = null;
		private NetText_label TrayLabel => _trayLabel ? _trayLabel : this["TrayLabel"] as NetText_label;

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
				StatusLabel.MasterSetValue("TRAY OPEN");
			}
			else if (Photocopier.ScannerOpen)
			{
				StatusLabel.MasterSetValue("SCANNER OPEN");
			}
			else if (Photocopier.TrayCount == 0)
			{
				StatusLabel.MasterSetValue("TRAY EMPTY");
			}
			else if (Photocopier.ScannedTextNull)
			{
				StatusLabel.MasterSetValue("DOCUMENT NOT SCANNED");
			}
			else
			{
				StatusLabel.MasterSetValue("COPIER READY");
			}
		}

		private void RenderScannerLabel()
		{
			if (Photocopier.ScannerOpen)
			{
				ScannerLabel.MasterSetValue("ERR: SCANNER OPEN");
			}
			else if (!Photocopier.ScannedTextNull)
			{
				ScannerLabel.MasterSetValue("DOCUMENT SCANNED");
			}
			else if (Photocopier.ScannedTextNull)
			{
				ScannerLabel.MasterSetValue("DOCUMENT NOT SCANNED");
			}
		}

		private void RenderTrayLabel()
		{
			if (Photocopier.TrayOpen)
			{
				TrayLabel.MasterSetValue("ERR: TRAY OPEN");
			}
			else
			{
				TrayLabel.MasterSetValue($"PAGES IN TRAY: {Photocopier.TrayCount}/{Photocopier.TrayCapacity}");
			}
		}

		public void OpenTray()
		{
			if (!Photocopier.TrayOpen && this.Photocopier.photocopierState == Photocopier.PhotocopierState.Idle)
			{
				SoundManager.PlayNetworkedAtPos(Beep, registerObject.WorldPosition);
				Photocopier.ToggleTray();
			}
			else if (this.Photocopier.photocopierState == Photocopier.PhotocopierState.TrayOpen)
			{
				TrayLabel.MasterSetValue("ERR: CLOSE SCANNER");
			}
		}

		public void OpenScanner()
		{
			if (!Photocopier.ScannerOpen && this.Photocopier.photocopierState == Photocopier.PhotocopierState.Idle)
			{
				SoundManager.PlayNetworkedAtPos(Beep, registerObject.WorldPosition);
				Photocopier.ToggleScannerLid();
			}
			else if(this.Photocopier.photocopierState == Photocopier.PhotocopierState.TrayOpen)
			{
				ScannerLabel.MasterSetValue("ERR: CLOSE TRAY");
			}
		}

		public void Print()
		{
			if (Photocopier.InkCartadge == null)
			{
				StatusLabel.MasterSetValue("NO INK");
				return;
			}
			if (Photocopier.CanPrint())
			{
				SoundManager.PlayNetworkedAtPos(Beep, registerObject.WorldPosition);
				Photocopier.Print();
			}
		}

		public void Scan()
		{
			if (Photocopier.CanScan())
			{
				SoundManager.PlayNetworkedAtPos(Beep, registerObject.WorldPosition);
				Photocopier.Scan();
			}
		}

		public void ClearScannedText() => Photocopier.ClearScannedText();
	}
}
