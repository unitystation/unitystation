using System.Collections;
using System.Collections.Generic;
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

		public string GetFullBodyPartDamageDescReport()
		{
			string report = $"Analyizing damage report for {BodyPartReadableName}..";

			report += "[Open Wound Size -> ";
			switch (currentCutSize)
			{
				case (BodyPartCutSize.SMALL):
					report += $"{BodyPartCutSize.SMALL}]\n";
					break;
				case (BodyPartCutSize.MEDIUM):
					report += $"{BodyPartCutSize.MEDIUM}]\n";
					break;
				case (BodyPartCutSize.LARGE):
					report += $"{BodyPartCutSize.LARGE}]\n";
					break;
				default:
					report += $"{BodyPartCutSize.NONE}]\n";
					break;
			}

			report += $"[Wound Bleeding -> {isBleedingExternally}]";

			if(currentCutSize != BodyPartCutSize.NONE)
			{
				report += "[Wound Report]\n";
				switch (currentPierceDamageLevel)
				{
					case (PierceDamageLevel.SMALL):
						report += $"{pireceDamageDescOnSMALL}\n";
						break;
					case (PierceDamageLevel.MEDIUM):
						report += $"{pireceDamageDescOnMEDIUM}\n";
						break;
					case (PierceDamageLevel.LARGE):
						report += $"{pireceDamageDescOnLARGE}\n";
						break;
					default:
						report += $"{pireceDamageDescOnNone}]\n";
						break;
				}
				switch (currentSlashDamageLevel)
				{
					case (SlashDamageLevel.SMALL):
						report += $"{slashDamageDescOnSMALL}\n";
						break;
					case (SlashDamageLevel.MEDIUM):
						report += $"{slashDamageDescOnMEDIUM}\n";
						break;
					case (SlashDamageLevel.LARGE):
						report += $"{slashDamageDescOnLARGE}\n";
						break;
					default:
						report += $"{slashDamageDescOnNone}]\n";
						break;
				}
			}
			report += "[Burn Damage]\n";
			switch (currentBurnDamageLevel)
			{
				case (BurnDamageLevels.MINOR):
					report += $"{burnDamageDescOnMINOR}\n";
					break;
				case (BurnDamageLevels.MAJOR):
					report += $"{burnDamageDescOnMAJOR}\n";
					break;
				case (BurnDamageLevels.CHARRED):
					report += $"{burnDamageDescOnCHARRED}\n";
					break;
				default:
					report += $"{burnDamageDescOnNone}\n";
					break;
			}

			report = TranslateTags(report);
			return report;
		}

		private string TranslateTags(string txt)
		{
			return txt.Replace("{readableName}", BodyPartReadableName);
		}
	}
}

