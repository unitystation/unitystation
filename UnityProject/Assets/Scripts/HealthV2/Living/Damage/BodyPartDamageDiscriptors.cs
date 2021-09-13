using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace HealthV2
{
	public partial class BodyPart
	{
		[Header("Damage Descriptions")]
		public string BodyPartReadableName    = "[Default Part Name]";
		private string burnDamageDescOnNone   = "{readableName} suffers no burns.";
		private string burnDamageDescOnMINOR  = "{readableName} suffers Second Degree Burns.";
		private string burnDamageDescOnMAJOR  = "{readableName} suffers Third Degree Burns.";
		private string burnDamageDescOnCHARRED= "{readableName} suffers Catastrophic Burns! They're charred beyond recognation!";
		private string slashDamageDescOnNone  = "{readableName} has no large enough wounds to concern about.";
		private string slashDamageDescOnSMALL = "{readableName} suffers from Rough Abrasion.";
		private string slashDamageDescOnMEDIUM= "{readableName} suffers from Open Laceration. Their wound is too big to be ignored!";
		private string slashDamageDescOnLARGE = "{readableName} suffers from Rough Abrasion. Their guts are visable!";
		private string pireceDamageDescOnNone = "{readableName} has no major breakages that may affect internal organs.";
		private string pireceDamageDescOnSMALL= "{readableName} suffers from Minor Breakage.";
		private string pireceDamageDescOnMEDIUM="{readableName} suffers from an Open Puncture.";
		private string pireceDamageDescOnLARGE= "{readableName} suffers from a Ruptured Cavity.";
		[SerializeField] private string internalDamageDesc	 = "This {readableName} is suffering from internal damage.";
		[SerializeField] private string externalBleedingDesc = "This {readableName} is bleeding due to an open wound.";
		[SerializeField, NaughtyAttributes.EnableIf("CanBeBroken")] private string BoneFracturedLvlThreeDesc = "This {readableName} is suffering Hairline Fracture.";
		[SerializeField, NaughtyAttributes.EnableIf("CanBeBroken")] private string BoneFracturedLvlTwoDesc = "This {readableName} is suffering Compound Fracture. It is completely snapped in half.";

		public string GetFullBodyPartDamageDescReport()
		{
			StringBuilder report = new StringBuilder();
			report.Append($"Analyzing damage report for {BodyPartReadableName}.. \n");

			if (CanBeBroken)
			{
				CheckIfBroken();
				report.Append("[Fracture Level]\n");
				if (isFractured_Compound)
				{
					report.Append(BoneFracturedLvlThreeDesc);
					return TranslateTags(report.ToString());
				}

				if (isFractured_Hairline)
				{
					report.Append(BoneFracturedLvlTwoDesc);
					return TranslateTags(report.ToString());
				}

				if (Severity == DamageSeverity.Light)
				{
					report.Append("Joint Dislocation detected.");
					return report.ToString();
				}

				report.Append($"{BodyPartReadableName} suffers no fractures and is healthy.");

				return TranslateTags(report.ToString());
			}

			report.Append("[Open Wound Size -> ");
			switch (currentCutSize)
			{
				case (BodyPartCutSize.SMALL):
					report.Append($"{BodyPartCutSize.SMALL}]\n");
					break;
				case (BodyPartCutSize.MEDIUM):
					report.Append($"{BodyPartCutSize.MEDIUM}]\n");
					break;
				case (BodyPartCutSize.LARGE):
					report.Append($"{BodyPartCutSize.LARGE}]\n");
					break;
				default:
					report.Append("{BodyPartCutSize.NONE}]\n");
					break;
			}

			report.Append($"[Wound Bleeding -> {isBleedingExternally}] \n");

			if (CanBleedInternally)
			{
				report.Append($"[Organ is internally bleeding -> {isBleedingInternally}] \n");
			}

			if(currentCutSize != BodyPartCutSize.NONE)
			{
				report.Append("[Wound Report] \n");
				switch (currentPierceDamageLevel)
				{
					case (TraumaDamageLevel.SMALL):
						report.Append($"{pireceDamageDescOnSMALL} \n");
						break;
					case (TraumaDamageLevel.SERIOUS):
						report.Append($"{pireceDamageDescOnMEDIUM} \n");
						break;
					case (TraumaDamageLevel.CRITICAL):
						report.Append($"{pireceDamageDescOnLARGE} \n");
						break;
					default:
						report.Append($"{pireceDamageDescOnNone}] \n");
						break;
				}
				switch (currentSlashDamageLevel)
				{
					case (TraumaDamageLevel.SMALL):
						report.Append($"{slashDamageDescOnSMALL} \n");
						break;
					case (TraumaDamageLevel.SERIOUS):
						report.Append($"{slashDamageDescOnMEDIUM} \n");
						break;
					case (TraumaDamageLevel.CRITICAL):
						report.Append($"{slashDamageDescOnLARGE} \n");
						break;
					default:
						report.Append($"{slashDamageDescOnNone}] \n");
						break;
				}
			}
			report.Append("[Burn Damage] \n");
			switch (currentBurnDamageLevel)
			{
				case (TraumaDamageLevel.SMALL):
					report.Append($"{burnDamageDescOnMINOR} \n");
					break;
				case (TraumaDamageLevel.SERIOUS):
					report.Append($"{burnDamageDescOnMAJOR} \n");
					break;
				case (TraumaDamageLevel.CRITICAL):
					report.Append($"{burnDamageDescOnCHARRED} \n");
					break;
				default:
					report.Append($"{burnDamageDescOnNone} \n");
					break;
			}

			return TranslateTags(report.ToString());
		}

		private string TranslateTags(string txt)
		{
			return txt.Replace("{readableName}", BodyPartReadableName);
		}
	}
}

