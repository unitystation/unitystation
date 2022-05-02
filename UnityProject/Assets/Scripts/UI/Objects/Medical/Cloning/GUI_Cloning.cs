using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Medical;
using Health.Sickness;

namespace UI.Objects.Medical
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

		public NetLabel[] cloningPodStatus;
		public NetLabel scannerStatus;
		public NetLabel buttonTextViewRecord;
		public NetLabel recordName;
		public NetLabel recordScanID;
		public NetLabel recordOxy;
		public NetLabel recordBurn;
		public NetLabel recordToxin;
		public NetLabel recordBrute;
		public NetLabel recordUniqueID;
		public NetLabel limbName;
		public NetLabel limbBurn;
		public NetLabel limbBrute;
		public NetLabel limbToxin;
		public NetLabel ailments;
		public NetLabel tabTitle;
		public NetLabel tabDamage;
		public NetLabel tabBurn;
		public NetLabel tabToxin;
		public NetLabel tabBrute;
		public NetLabel tabOxygen;
		public NetLabel tabBleeding;
		public NetLabel[] organButtons;
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
			buttonTextViewRecord.SetValueServer($"View Records({CloningConsole.CloningRecords.Count()})");
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
				recordName.SetValueServer(specificRecord.name);
				recordScanID.SetValueServer("Scan ID " + specificRecord.scanID);
				recordOxy.SetValueServer(specificRecord.oxyDmg + "\tOxygen Damage");
				recordBurn.SetValueServer(specificRecord.burnDmg + "\tBurn Damage");
				recordToxin.SetValueServer(specificRecord.toxinDmg + "\tToxin Damage");
				recordBrute.SetValueServer(specificRecord.bruteDmg + "\tBrute Damage");
				recordUniqueID.SetValueServer(specificRecord.uniqueIdentifier);
			}
		}

		void LimbRecord(BodyPartRecord limb)
		{
			if (limb != null)
			{
				limbName.SetValueServer($"{limb.name}");
				limbBrute.SetValueServer($"{limb.brute}");
				limbBurn.SetValueServer($"{limb.burn}");
				limbToxin.SetValueServer($"{limb.toxin}");
			}
			else
            {
				limbName.SetValueServer("---");
				limbBrute.SetValueServer("0");
				limbBurn.SetValueServer("0");
				limbToxin.SetValueServer("0");
			}
			CloseOrganTab();
		}

		public void OrganRecord(BodyPartRecord limb)
        {
			foreach(NetLabel button in organButtons)
            {
				button.SetValueServer("");
            }

			if (limb == null) return;

			organList.Clear();
			var i = 0;
			foreach(BodyPartRecord organ in limb.organs)
            {
				organButtons[i].SetValueServer($"{organ.name}");
				organList.Add(organ);
				i++;
            }
        }

		//so you can't click buttons through tab
		private bool tabIsOpen = false;

		public void DisplayOrganTab(int i)
		{
			if (i >= organList.Count() || tabIsOpen) return;

			xButton.SetValueServer(Color.black);
			organStatusTab.SetValueServer(Color.white);
			tabTitle.SetValueServer($"{organList[i].name} status");
			tabDamage.SetValueServer("Damage");
			tabBurn.SetValueServer($"Brn- {organList[i].burn}");
			tabToxin.SetValueServer($"Tox- {organList[i].toxin}");
			tabBrute.SetValueServer($"Brt- {organList[i].brute}");
			tabOxygen.SetValueServer($"Oxy- {organList[i].oxygen}");
			tabBleeding.SetValueServer("Bleeding: " + (organList[i].isBleeding ? "Yes" : "No"));
			tabIsOpen = true;
		}

		public void CloseOrganTab()
		{
			xButton.SetValueServer(Color.clear);
			organStatusTab.SetValueServer(Color.clear);
			tabTitle.SetValueServer("");
			tabDamage.SetValueServer("");
			tabBurn.SetValueServer("");
			tabToxin.SetValueServer("");
			tabBrute.SetValueServer("");
			tabOxygen.SetValueServer("");
			tabBleeding.SetValueServer("");
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
				cloningPodStatus[i].SetValueServer(text);
			}
		}

		public void DisplayScannerStatus()
		{
			if (CloningConsole.Scanner)
			{
				scannerStatus.SetValueServer(CloningConsole.Scanner.statusString);
			}
			else
			{
				scannerStatus.SetValueServer("ERROR: no DNA scanner detected.");
			}
		}

		public void LimbInspection(int limbType)
        {
			if (limbType != null)
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
			ailments.SetValueServer(sicknesses);
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
				overlay.SetValueServer(Color.clear);
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
					overlays[arrayPosition].SetValueServer(Color.white);
					continue;
				}
				switch (surfaceBodyPart.severity)
				{
					case DamageSeverity.Light:
						overlays[arrayPosition].SetValueServer(Color.white);
						break;

					case DamageSeverity.LightModerate :
						arrayPosition += 1;
						overlays[arrayPosition].SetValueServer(Color.white);
						break;

					case DamageSeverity.Moderate :
						arrayPosition += 2;
						overlays[arrayPosition].SetValueServer(Color.white);
						break;

					case DamageSeverity.Bad :
						arrayPosition += 3;
						overlays[arrayPosition].SetValueServer(Color.white);
						break;

					case DamageSeverity.Critical :
						arrayPosition += 4;
						overlays[arrayPosition].SetValueServer(Color.white);
						break;

					case DamageSeverity.Max:
						arrayPosition += 4;
						overlays[arrayPosition].SetValueServer(Color.white);
						break;
				}
			}
		}
	}
}
