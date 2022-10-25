using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Objects.Medical;
using UI.Core.NetUI;
using UnityEngine;

namespace UI.Objects.Medical.Cloning
{
	public class GUI_Cloning : NetTab
	{
		public CloningConsole CloningConsole;
		public GUI_CloningItemList recordList = null;

		public NetPageSwitcher netPageSwitcher;
		public NetPage PageAllRecords;
		public NetPage PageSpecificRecord;
		public NetPage PageHealthInspection;

		public CloningRecord specificRecord;

		public NetText_label[] cloningPodStatus;
		public NetText_label scannerStatus;
		public NetText_label buttonTextViewRecord;
		public NetText_label recordName;
		public NetText_label recordScanID;
		public NetText_label recordOxy;
		public NetText_label recordBurn;
		public NetText_label recordToxin;
		public NetText_label recordBrute;
		public NetText_label recordUniqueID;
		public NetText_label limbName;
		public NetText_label limbBurn;
		public NetText_label limbBrute;
		public NetText_label limbToxin;
		public NetText_label ailments;
		public NetText_label tabTitle;
		public NetText_label tabDamage;
		public NetText_label tabBurn;
		public NetText_label tabToxin;
		public NetText_label tabBrute;
		public NetText_label tabOxygen;
		public NetText_label tabBleeding;
		public NetText_label[] organButtons;
		public NetColorChanger organStatusTab;
		public NetColorChanger xButton;

		public NetColorChanger[] overlays;

		private List<BodyPartRecord> organList = new List<BodyPartRecord>();

		protected override void InitServer()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			CloningConsole = Provider.GetComponentInChildren<CloningConsole>();
			CloningConsole.RegisterConsoleGUI(this);
			//Subscribe to change event from CloningConsole.cs
			UpdateDisplay();
		}

		public void UpdateDisplay()
		{
			DisplayAllRecords();
			DisplayCurrentRecord();
			DisplayPodStatus();
			DisplayScannerStatus();
			LimbRecord(null);
			OrganRecord(null);
			buttonTextViewRecord.MasterSetValue($"View Records({CloningConsole.CloningRecords.Count()})");
		}

		public void StartScan()
		{
			CloningConsole.Scan();
			UpdateDisplay();
		}

		public void LockScanner()
		{
			CloningConsole.ServerToggleLock();
			UpdateDisplay();
		}

		public void DeleteRecord()
		{
			RemoveRecord();
			UpdateDisplay();
			netPageSwitcher.SetActivePage(PageAllRecords);
		}

		public void RemoveRecord()
		{
			CloningConsole.RemoveRecord(specificRecord);
			specificRecord = null;
		}

		public void Clone()
		{
			CloningConsole.ServerTryClone(specificRecord);
			UpdateDisplay();
			netPageSwitcher.SetActivePage(PageAllRecords);
		}

		public void ViewAllRecords()
		{
			UpdateDisplay();
			netPageSwitcher.SetActivePage(PageAllRecords);
		}

		public void ViewRecord(CloningRecord cloningRecord)
		{
			specificRecord = cloningRecord;
			UpdateDisplay();
			netPageSwitcher.SetActivePage(PageSpecificRecord);
		}

		public void ViewHealthInspection()
		{
			UpdateDisplay();
			DisplayAilments();
			SetOverlays();
			netPageSwitcher.SetActivePage(PageHealthInspection);
		}

		public void ViewRecordReturn()
        {
			ViewRecord(specificRecord);
        }

		public void DisplayCurrentRecord()
		{
			if (specificRecord != null)
			{
				recordName.MasterSetValue(specificRecord.name);
				recordScanID.MasterSetValue("Scan ID " + specificRecord.scanID);
				recordOxy.MasterSetValue(specificRecord.oxyDmg + "\tOxygen Damage");
				recordBurn.MasterSetValue(specificRecord.burnDmg + "\tBurn Damage");
				recordToxin.MasterSetValue(specificRecord.toxinDmg + "\tToxin Damage");
				recordBrute.MasterSetValue(specificRecord.bruteDmg + "\tBrute Damage");
				recordUniqueID.MasterSetValue(specificRecord.uniqueIdentifier);
			}
		}

		void LimbRecord(BodyPartRecord limb)
		{
			if (limb != null)
			{
				limbName.MasterSetValue($"{limb.name}");
				limbBrute.MasterSetValue($"{limb.brute}");
				limbBurn.MasterSetValue($"{limb.burn}");
				limbToxin.MasterSetValue($"{limb.toxin}");
			}
			else
            {
				limbName.MasterSetValue("---");
				limbBrute.MasterSetValue("0");
				limbBurn.MasterSetValue("0");
				limbToxin.MasterSetValue("0");
			}
			CloseOrganTab();
		}

		public void OrganRecord(BodyPartRecord limb)
        {
			foreach(NetText_label button in organButtons)
            {
				button.MasterSetValue("");
            }

			if (limb == null) return;

			organList.Clear();
			var i = 0;
			foreach(BodyPartRecord organ in limb.organs)
            {
				organButtons[i].MasterSetValue($"{organ.name}");
				organList.Add(organ);
				i++;
            }
        }

		//so you can't click buttons through tab
		private bool tabIsOpen = false;

		public void DisplayOrganTab(int i)
		{
			if (i >= organList.Count() || tabIsOpen) return;

			xButton.MasterSetValue(Color.black);
			organStatusTab.MasterSetValue(Color.white);
			tabTitle.MasterSetValue($"{organList[i].name} status");
			tabDamage.MasterSetValue("Damage");
			tabBurn.MasterSetValue($"Brn- {organList[i].burn}");
			tabToxin.MasterSetValue($"Tox- {organList[i].toxin}");
			tabBrute.MasterSetValue($"Brt- {organList[i].brute}");
			tabOxygen.MasterSetValue($"Oxy- {organList[i].oxygen}");
			tabBleeding.MasterSetValue("Bleeding: " + (organList[i].isBleeding ? "Yes" : "No"));
			tabIsOpen = true;
		}

		public void CloseOrganTab()
		{
			xButton.MasterSetValue(Color.clear);
			organStatusTab.MasterSetValue(Color.clear);
			tabTitle.MasterSetValue("");
			tabDamage.MasterSetValue("");
			tabBurn.MasterSetValue("");
			tabToxin.MasterSetValue("");
			tabBrute.MasterSetValue("");
			tabOxygen.MasterSetValue("");
			tabBleeding.MasterSetValue("");
			tabIsOpen = false;
        }

		public void DisplayAllRecords()
		{
			recordList.Clear();
			recordList.AddItems(CloningConsole.CloningRecords.Count());

			var i = 0;
			foreach (var cloningRecord in CloningConsole.CloningRecords)
			{
				GUI_CloningRecordItem item = recordList.Entries[i] as GUI_CloningRecordItem;
				item.gui_Cloning = this;
				item.cloningRecord = cloningRecord;
				item.SetValues();
				i++;
			}
		}

		public void DisplayPodStatus()
		{
			string text;
			if (CloningConsole.CloningPod)
			{
				text = CloningConsole.CloningPod.statusString;
			}
			else
			{
				text = "ERROR: no pod detected.";
			}
			for (int i = 0; i < cloningPodStatus.Length; i++)
			{
				cloningPodStatus[i].MasterSetValue(text);
			}
		}

		public void DisplayScannerStatus()
		{
			if (CloningConsole.Scanner)
			{
				scannerStatus.MasterSetValue(CloningConsole.Scanner.statusString);
			}
			else
			{
				scannerStatus.MasterSetValue("ERROR: no DNA scanner detected.");
			}
		}

		public void LimbInspection(int limbType)
        {
	        foreach (BodyPartRecord limbs in specificRecord.surfaceBodyParts)
	        {
		        if ((BodyPartType)limbType == limbs.type)
		        {
			        LimbRecord(limbs);
			        OrganRecord(limbs);
			        return;
		        }
	        }

	        LimbRecord(null);
	        OrganRecord(null);
        }

		public void DisplayAilments()
        {
			Debug.Log("Ailment Run");
			string sicknesses = "None";

			if(specificRecord.sicknessList.Count != 0)
            {
				Debug.Log("is sick");
				sicknesses = "";
				foreach (string sickness in specificRecord.sicknessList)
                {
					sicknesses += $"{sickness}\n";
                }
            }
			ailments.MasterSetValue(sicknesses);
		}
		/*
		 overlays array is ordered to match BodyPartType enums
			Head = 0,
			Chest = 1,
			RightArm = 2,
			LeftArm = 3,
			RightLeg = 4,
			LeftLeg = 5,
		 */
		public void SetOverlays()
        {
			foreach(NetColorChanger overlay in overlays)
            {
				overlay.MasterSetValue(Color.clear);
            }

			for (int i = 0; i < 6; i++)
			{
				BodyPartRecord surfaceBodyPart = null;
				foreach (BodyPartRecord limbs in specificRecord.surfaceBodyParts)
				{
					if ((BodyPartType)i == limbs.type)
					{
						surfaceBodyPart = limbs;
						break;
					}
				}
				// Sorry for the algebra
				// overlays has 36 images, 6 sets of 6 limbs, each set is the same limb from least to most damage
				// multiplying the six by the int gives you which limb it's looking for
				// the addition give you the severity of that limb type
				int arrayPosition = i * 6;
				if (surfaceBodyPart == null)
                {
					arrayPosition += 5;
					overlays[arrayPosition].MasterSetValue(Color.white);
					continue;
				}
				switch (surfaceBodyPart.severity)
				{
					case DamageSeverity.Light:
						overlays[arrayPosition].MasterSetValue(Color.white);
						break;

					case DamageSeverity.LightModerate :
						arrayPosition += 1;
						overlays[arrayPosition].MasterSetValue(Color.white);
						break;

					case DamageSeverity.Moderate :
						arrayPosition += 2;
						overlays[arrayPosition].MasterSetValue(Color.white);
						break;

					case DamageSeverity.Bad :
						arrayPosition += 3;
						overlays[arrayPosition].MasterSetValue(Color.white);
						break;

					case DamageSeverity.Critical :
						arrayPosition += 4;
						overlays[arrayPosition].MasterSetValue(Color.white);
						break;

					case DamageSeverity.Max:
						arrayPosition += 4;
						overlays[arrayPosition].MasterSetValue(Color.white);
						break;
				}
			}
		}
	}
}
