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
		[SerializeField, NaughtyAttributes.EnableIf(nameof(CanBeBroken))] private string BoneFracturedLvlThreeDesc = "This {readableName} is suffering Hairline Fracture.";
		[SerializeField, NaughtyAttributes.EnableIf(nameof(CanBeBroken))] private string BoneFracturedLvlTwoDesc = "This {readableName} is suffering Compound Fracture. It is completely snapped in half.";

		public string GetFullBodyPartDamageDescReport()
		{
			StringBuilder report = new StringBuilder();
			report.Append($"Analyzing damage report for {BodyPartReadableName}.. \n");

			if (CanBeBroken)
			{
				CheckIfBroken();
				report.AppendLine("[Fracture Level]");
				if (isFractured_Compound)
				{
					report.AppendLine(BoneFracturedLvlThreeDesc);
					return TranslateTags(report.ToString());
				}

				if (isFractured_Hairline)
				{
					report.AppendLine(BoneFracturedLvlTwoDesc);
					return TranslateTags(report.ToString());
				}

				if (Severity == DamageSeverity.Light)
				{
					report.AppendLine("Joint Dislocation detected.");
					return report.ToString();
				}

				report.AppendLine($"{BodyPartReadableName} suffers no fractures and is healthy.");

				return TranslateTags(report.ToString());
			}

			report.Append("[Open Wound Size -> ");
			switch (currentCutSize)
			{
				case (BodyPartCutSize.SMALL):
					report.AppendLine($"{BodyPartCutSize.SMALL}]");
					break;
				case (BodyPartCutSize.MEDIUM):
					report.AppendLine($"{BodyPartCutSize.MEDIUM}]");
					break;
				case (BodyPartCutSize.LARGE):
					report.AppendLine($"{BodyPartCutSize.LARGE}]");
					break;
				default:
					report.AppendLine("{BodyPartCutSize.NONE}]");
					break;
			}

			report.AppendLine($"[Wound Bleeding -> {isBleedingExternally}]");

			if (CanBleedInternally)
			{
				report.AppendLine($"[Organ is internally bleeding -> {isBleedingInternally}]");
			}

			if(currentCutSize != BodyPartCutSize.NONE)
			{
				report.AppendLine("[Wound Report]");
				switch (currentPierceDamageLevel)
				{
					case (TraumaDamageLevel.SMALL):
						report.AppendLine($"{pireceDamageDescOnSMALL} \n");
						break;
					case (TraumaDamageLevel.SERIOUS):
						report.AppendLine($"{pireceDamageDescOnMEDIUM} \n");
						break;
					case (TraumaDamageLevel.CRITICAL):
						report.AppendLine($"{pireceDamageDescOnLARGE} \n");
						break;
					default:
						report.AppendLine($"{pireceDamageDescOnNone}] \n");
						break;
				}
				switch (currentSlashDamageLevel)
				{
					case (TraumaDamageLevel.SMALL):
						report.AppendLine($"{slashDamageDescOnSMALL}");
						break;
					case (TraumaDamageLevel.SERIOUS):
						report.AppendLine($"{slashDamageDescOnMEDIUM}");
						break;
					case (TraumaDamageLevel.CRITICAL):
						report.AppendLine($"{slashDamageDescOnLARGE}");
						break;
					default:
						report.AppendLine($"{slashDamageDescOnNone}]");
						break;
				}
			}
			report.AppendLine("[Burn Damage]");
			switch (currentBurnDamageLevel)
			{
				case (TraumaDamageLevel.SMALL):
					report.AppendLine($"{burnDamageDescOnMINOR}");
					break;
				case (TraumaDamageLevel.SERIOUS):
					report.AppendLine($"{burnDamageDescOnMAJOR}");
					break;
				case (TraumaDamageLevel.CRITICAL):
					report.AppendLine($"{burnDamageDescOnCHARRED}");
					break;
				default:
					report.AppendLine($"{burnDamageDescOnNone}");
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

