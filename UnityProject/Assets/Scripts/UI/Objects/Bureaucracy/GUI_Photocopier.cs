using System;
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
				StatusLabel.SetValueServer("TRAY OPEN");
			}
			else if (Photocopier.ScannerOpen)
			{
				StatusLabel.SetValueServer("SCANNER OPEN");
			}
			else if (Photocopier.TrayCount == 0)
			{
				StatusLabel.SetValueServer("TRAY EMPTY");
			}
			else if (Photocopier.ScannedTextNull)
			{
				StatusLabel.SetValueServer("DOCUMENT NOT SCANNED");
			}
			else
			{
				StatusLabel.SetValueServer("COPIER READY");
			}
		}

		private void RenderScannerLabel()
		{
			if (Photocopier.ScannerOpen)
			{
				ScannerLabel.SetValueServer("ERR: SCANNER OPEN");
			}
			else if (!Photocopier.ScannedTextNull)
			{
				ScannerLabel.SetValueServer("DOCUMENT SCANNED");
			}
			else if (Photocopier.ScannedTextNull)
			{
				ScannerLabel.SetValueServer("DOCUMENT NOT SCANNED");
			}
		}

		private void RenderTrayLabel()
		{
			if (Photocopier.TrayOpen)
			{
				TrayLabel.SetValueServer("ERR: TRAY OPEN");
			}
			else
			{
				TrayLabel.SetValueServer($"PAGES IN TRAY: {Photocopier.TrayCount}/{Photocopier.TrayCapacity}");
			}
		}

		public void OpenTray()
		{
			if (!Photocopier.TrayOpen)
			{
				SoundManager.PlayNetworkedAtPos(Beep, registerObject.WorldPosition);
				Photocopier.ToggleTray();
			}
		}

		public void OpenScanner()
		{
			if (!Photocopier.ScannerOpen)
			{
				SoundManager.PlayNetworkedAtPos(Beep, registerObject.WorldPosition);
				Photocopier.ToggleScannerLid();
			}
		}

		public void Print()
		{
			if (Photocopier.InkCartadge == null)
			{
				StatusLabel.SetValueServer("NO INK");
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
