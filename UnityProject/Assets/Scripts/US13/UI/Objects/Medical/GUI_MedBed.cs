using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.HealthV2;
using US13.HealthV2.Living.BodyParts;
using US13.Objects.Closets;
using US13.Objects.Medical;
using US13.UI.Core.Net;
using US13.UI.Core.Net.Elements;
using US13.UI.Objects.MedBed;

namespace US13.UI.Objects.Medical
{
	public class GUI_MedBed : NetTab
	{
		public US13.Objects.Medical.MedBed MedBed;

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

		public NetText_label TotalBurn;
		public NetText_label TotalToxin;
		public NetText_label TotalBrute;


		public NetColorChanger[] overlays;

		private List<BodyPart> organList = new List<BodyPart>();

		public ChemicalInjection_List ChemicalInjection_List;

		public List<GUI_ChemicalInjection> GUI_ChemicalInjections = new List<GUI_ChemicalInjection>();

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
			MedBed = Provider.GetComponentInChildren<US13.Objects.Medical.MedBed>();
			MedBed.RegisterGUI(this);


			foreach (var gen in MedBed.ReagentsGen)
			{
				GUI_ChemicalInjections.Add( ChemicalInjection_List.AddElementReagent(gen, this));
			}


			//Subscribe to change event from CloningConsole.cs
			UpdateDisplay();
		}

		public void UpdateDisplay()
		{
			foreach (var GUI in GUI_ChemicalInjections)
			{
				GUI.UpdateVariables();
			}

			if (MedBed.LivingHealthMasterBase != null)
			{
				TotalBurn.MasterSetValue($"{-Math.Round(MedBed.LivingHealthMasterBase.GetTotalBurnDamage(),2)}");
				TotalToxin.MasterSetValue($"{-Math.Round(MedBed.LivingHealthMasterBase.GetTotalToxDamage(),2)}");
				TotalBrute.MasterSetValue($"{-Math.Round(MedBed.LivingHealthMasterBase.GetTotalBruteDamage(),2)}");
			}
			else
			{
				TotalBurn.MasterSetValue("N/A");
				TotalToxin.MasterSetValue("N/A");
				TotalBrute.MasterSetValue("N/A");
			}

			if (MedBed.LivingHealthMasterBase == null)
			{
				LimbRecord(null);
				OrganRecord(null);
			}

			SetOverlays();
		}


		void LimbRecord(BodyPart limb)
		{
			if (limb != null)
			{
				limbName.MasterSetValue($"{limb.name}");
				limbBrute.MasterSetValue($"{ (float)Math.Round((double)limb.Brute, 2)}");
				limbBurn.MasterSetValue($"{ (float)Math.Round((double)limb.Burn, 2)}");
				limbToxin.MasterSetValue($"{ (float)Math.Round((double)limb.Toxin, 2)}");
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

		public void OrganRecord(BodyPart limb)
		{
			foreach(NetText_label button in organButtons)
			{
				button.MasterSetValue("");
			}

			if (limb == null) return;

			var i = 0;
			foreach(var organ in limb.ContainBodyParts)
			{
				if(organButtons.Length <= i) continue;
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
			tabBurn.MasterSetValue($"Brn- {organList[i].Burn}");
			tabToxin.MasterSetValue($"Tox- {organList[i].Toxin}");
			tabBrute.MasterSetValue($"Brt- {organList[i].Brute}");
			tabOxygen.MasterSetValue($"Oxy- {organList[i].Oxy}");
			tabBleeding.MasterSetValue("Bleeding: " + (organList[i].IsBleeding ? "Yes" : "No"));
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


		public void LimbInspection(int limbType)
		{
			foreach (var  limbs in MedBed.LivingHealthMasterBase.SurfaceBodyParts)
			{
				if ((BodyPartType)limbType == limbs.BodyPartType)
				{
					LimbRecord(limbs);
					OrganRecord(limbs);
					return;
				}
			}

			LimbRecord(null);
			OrganRecord(null);
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
			if (MedBed.LivingHealthMasterBase == null)
			{
				return;
			}

			foreach(NetColorChanger overlay in overlays)
			{
				overlay.MasterSetValue(Color.clear);
			}

			for (int i = 0; i < 6; i++)
			{
				BodyPart surfaceBodyPart = null;
				foreach (var limbs in MedBed.LivingHealthMasterBase.SurfaceBodyParts)
				{
					if ((BodyPartType)i == limbs.BodyPartType)
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
				switch (surfaceBodyPart.Severity)
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

		public void ToggleOpen()
		{
			var Control = MedBed.ObjectContainer.GetComponent<ClosetControl>();

			Control.SetDoor(Control.IsOpen ? ClosetControl.Door.Closed : ClosetControl.Door.Opened);
		}

	}
}
