using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Items;
using UnityEngine;

namespace Systems.Cargo
{
	public partial class CargoManager : MonoBehaviour
	{
		public static List<string> ResearchedArtifacts { get; private set; }

		public static readonly Dictionary<string, ArtifactData> CorrectData = new Dictionary<string, ArtifactData>();

		private const int REPORT_SELL_PRICE = 12500; //The price a 100% accurate report sells for

		public static readonly string[] damageNames = { "Spontaneous Combustion", "Space Carp Materialisation", "Localised Stun", "Lightning", "Forcefield" };
		public static readonly string[] areaNames = { "Localised Teleport", "Localised Stun", "Space Carp Materialisation", "Facehugger Materialisation", "Milk.", "Plasma Materialisation", "Paranoia", "Heating Effect", "Cooling Effect", "Plasma Gas Formation", "Oxygen Gas Formation", "Oxygen Syphon", "Artifact Sickness", "Forcefield", "Organic Terraform", "Xenomorph Infection", "Magical Terraform", "Lavaland Terraform" };
		public static readonly string[] interactNames = { "Metal to Gold", "Metal to Plasma", "Portal Materialisation" };

		public const string ANOMALY_REPORT_TITLE_STRING = "nanotrasen anomaly report";


		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		public static void ClearStatics()
		{
			ResearchedArtifacts = new List<string>();
		}

		public static void AddArtifactToList(string ID)
		{
			ResearchedArtifacts.Add(ID);
		}

		//If and when any more Research Report style items are added in future,
		//use this to determine the type of report, and call the appropriate function to parse that report type.
		private static void ParseResearchData(Paper report)
		{
			if (report.ServerString == null) return;

			var reportAttributes = report.gameObject.GetComponent<ItemAttributesV2>();
			if(reportAttributes == null) return;

			Regex regex = new Regex(@"==(\D+)=="); //Pattern recognises a sentence of characters bared by double equals '=='. Used to recognise report type.
			Match firstMatch = regex.Match(report.ServerString);

			if(firstMatch.Success == false) return;
		
			switch(firstMatch.Groups[1].Value.ToLower())
			{
				case ANOMALY_REPORT_TITLE_STRING:
					ParseArtifactReport(report.ServerString, reportAttributes);
					break;
				default:
					break;
			}
			
		}

		#region ArtifactReports

		private static void ParseArtifactReport(string txt, ItemAttributesV2 reportAttributes)
		{
			ArtifactData artifactData = GetArtifactDataFromText(txt);
			int sellCost = CalculateAnomalyReportExportCost(artifactData);
			if (sellCost == -1)
			{
				reportAttributes.ServerSetArticleName("Anomaly report not recognised!");
				return;
			}

			if (ResearchedArtifacts.Contains(artifactData.ID) == false)
			{
				AddArtifactToList(artifactData.ID);
			}
			else
			{
				reportAttributes.ServerSetArticleName("Anomaly report already registered!");
				sellCost = 0;
			}

			reportAttributes.SetExportCost(sellCost);
		}

		public static ArtifactData GetArtifactDataFromText(string txt)
		{
			ArtifactData data = new ArtifactData();
			//An Explanation of this Regex pattern for future developers: (And poor code reviewers)
			//This pattern begins by looking for any three letter capital code enclosed in square brackets such as [OIL] => \[([A-Z]{3})\]
			//This implies a data field has been found, this code is put into group 1 for future reference.
			//It then looks for some string followed by a colon => [- \w]+:
			//It does this so it knows where the field name ends and field value beings, it then parses the value into group 2. => \s*([- \w]+)
			//This pattern is very flexible, as long as there is a three letter bracket code, a colon and some string after the colon it will parse the values.
			//Even if multiple values are on the same line. The bracket code is used to determine what artifact property the field applies too.
			//The bracket code is used as its easier to type, replicate and less prone to typing and spelling errors if a player edits the fields, the three letter code is all that matters.

			Regex regex = new Regex(@"\[([A-Z]{3})\][- \w]+:\s*([- \w]+)");
			
			foreach(Match match in regex.Matches(txt))
			{
				string bracketCode = match.Groups[1].Value;
				Regex unitRegex = new Regex(@"\d+"); //Gets the number out of strings, e.g.: 800mClw => 800
				Match unitNum;

				switch (bracketCode)
				{
					case "APP":
						Enum.TryParse<ArtifactType>(match.Groups[2].Value, true, out var type);
						data.Type = type;
						break;

					case "AIN":
						data.ID = match.Groups[2].Value;
						break;

					case "RAL":
						unitNum = unitRegex.Match(match.Groups[2].Value);
						int.TryParse(unitNum.Value, out int rad);
						data.radiationlevel = rad;
						break;

					case "BSA":
						unitNum = unitRegex.Match(match.Groups[2].Value);
						int.TryParse(unitNum.Value, out int bsa);
						data.bluespacesig = bsa;
						break;

					case "BSB":
						unitNum = unitRegex.Match(match.Groups[2].Value);
						int.TryParse(unitNum.Value, out int bsb);
						data.bluespacesig = bsb;
						break;

					case "PIF":
						data.AreaEffectValue = Array.FindIndex(areaNames, str => str.Equals(match.Groups[2].Value, StringComparison.OrdinalIgnoreCase)); 
						break;

					case "ONT":
						data.InteractEffectValue = Array.FindIndex(interactNames, str => str.Equals(match.Groups[2].Value, StringComparison.OrdinalIgnoreCase));
						break;

					case "OIL":
						data.DamageEffectValue = Array.FindIndex(damageNames, str => str.Equals(match.Groups[2].Value, StringComparison.OrdinalIgnoreCase));
						break;
				}
			}
			return data;
		}

		public static int CalculateAnomalyReportExportCost(ArtifactData inputData)
		{
			if (CorrectData.ContainsKey(inputData.ID) == false) return -1;

			int cost = 0;
			ArtifactData correctData = CorrectData[inputData.ID];

			cost += Mathf.Clamp(1000 - (2*Mathf.Abs(inputData.radiationlevel - correctData.radiationlevel)), 0, 1000);
			cost += Mathf.Clamp(1000 - (20*Mathf.Abs(inputData.bluespacesig - correctData.bluespacesig)), 0, 1000);
			cost += Mathf.Clamp(1000 - (5*Mathf.Abs(inputData.bananiumsig- correctData.bananiumsig)), 0, 1000);

			if (inputData.DamageEffectValue == correctData.DamageEffectValue) cost += 2000;
			if (inputData.InteractEffectValue == correctData.InteractEffectValue) cost += 2000;
			if (inputData.AreaEffectValue == correctData.AreaEffectValue) cost += 2000;
				
			if (inputData.Type == correctData.Type) cost += 1000;
			
			return (int)(cost *= REPORT_SELL_PRICE / 10000); 
		}

		#endregion
	}

	public struct ArtifactData //Artifact Data contains all properties of the artifact that will be transferred to samples and/or guessed by the research console, placed in a struct to make data transfer easier.
	{
		public ArtifactData(int radlvl = 0, int bluelvl = 0, int bnalvl = 0, int mss = 0, ArtifactType type = ArtifactType.Geological, int areaEffectValue = 0, int interactEffectValue = 0, int damageEffectValue = 0, string iD = "")
		{
			radiationlevel = radlvl;
			bluespacesig = bluelvl;
			bananiumsig = bnalvl;
			mass = mss;
			Type = type;
			AreaEffectValue = areaEffectValue;
			InteractEffectValue = interactEffectValue;
			DamageEffectValue = damageEffectValue;
			ID = iD;
		}

		public ArtifactType Type;
		public int radiationlevel;
		public int bluespacesig;
		public int bananiumsig;
		public int mass;
		public int AreaEffectValue;
		public int InteractEffectValue;
		public int DamageEffectValue;
		public string ID;
	}

	public enum ArtifactType //This determines the sprites the artifact can use, what tool is used to take samples from it, what samples it gives, and what materials it gives when dismantled.
	{
		Geological = 0,
		Mechanical = 1,
		Organic = 2,
	}
}
