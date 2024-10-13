using System;
using System.Collections.Generic;
using System.Text;
using HealthV2;
using Newtonsoft.Json;
using Systems.Character;
using Random = UnityEngine.Random;

namespace Objects.Medical
{
	public class CloningRecord
	{
		public string name;
		public string scanID;
		public float oxyDmg;
		public float burnDmg;
		public float toxinDmg;
		public float bruteDmg;
		public string uniqueIdentifier;
		public CharacterSheet characterSettings;
		public int mobID;
		public uint mindID; // New field for mindID
		public Mind mind;
		public List<BodyPartRecord> surfaceBodyParts = new();
		public List<string> sicknessList = new();

		public CloningRecord()
		{
			scanID = Random.Range(0, 9999).ToString();
		}

		public void UpdateRecord(LivingHealthMasterBase livingHealth, PlayerScript playerScript)
		{
			mobID = livingHealth.mobID;
			mindID = playerScript.Mind.netId; // Set mindID here
			name = playerScript.playerName;
			characterSettings = playerScript.characterSettings;
			oxyDmg = livingHealth.GetOxyDamage;
			burnDmg = livingHealth.GetTotalBurnDamage();
			toxinDmg = livingHealth.GetTotalToxDamage();
			bruteDmg = livingHealth.GetTotalBruteDamage();
			uniqueIdentifier = $"{livingHealth.mobID}{playerScript.playerName}".ToHexString();

			surfaceBodyParts.Clear();
			foreach (var part in livingHealth.SurfaceBodyParts)
			{
				var partRecord = new BodyPartRecord();
				surfaceBodyParts.Add(partRecord.Copy(part));
			}

			sicknessList.Clear();
			foreach (var sickness in livingHealth.mobSickness.sicknessAfflictions)
				sicknessList.Add(sickness.Sickness.SicknessName);
		}

		public string Copy()
		{
			var sb = new StringBuilder();

			// Serialize simple fields with delimiters, now includes mindID
			sb.Append(name).Append("|")
				.Append(scanID).Append("|")
				.Append(oxyDmg.ToString("F2")).Append("|")
				.Append(burnDmg.ToString("F2")).Append("|")
				.Append(toxinDmg.ToString("F2")).Append("|")
				.Append(bruteDmg.ToString("F2")).Append("|")
				.Append(uniqueIdentifier).Append("|")
				.Append(mobID).Append("|")
				.Append(mindID).Append("|"); // Include mindID here

			// Serialize surfaceBodyParts
			foreach (var part in surfaceBodyParts) sb.Append(part.ToCompactString()).Append(";");
			sb.Append("|");

			// Serialize sicknessList
			sb.Append(string.Join(",", sicknessList));

			return sb.ToString();
		}


		public static CloningRecord FromString(string recordData)
		{
			var parts = recordData.Split('|');
			var record = new CloningRecord();

			// Deserialize simple fields, including mindID
			record.name = parts[0];
			record.scanID = parts[1];
			record.oxyDmg = float.Parse(parts[2]);
			record.burnDmg = float.Parse(parts[3]);
			record.toxinDmg = float.Parse(parts[4]);
			record.bruteDmg = float.Parse(parts[5]);
			record.uniqueIdentifier = parts[6];
			record.mobID = int.Parse(parts[7]);
			record.mindID = uint.Parse(parts[8]); // Deserialize mindID

			// Deserialize surfaceBodyParts
			var bodyParts = parts[9].Split(';', StringSplitOptions.RemoveEmptyEntries);
			foreach (var bodyPartString in bodyParts)
				record.surfaceBodyParts.Add(BodyPartRecord.FromCompactString(bodyPartString));

			// Deserialize sicknessList
			record.sicknessList = new List<string>(parts[10].Split(',', StringSplitOptions.RemoveEmptyEntries));

			return record;
		}
	}

	public class BodyPartRecord
    {
		public string name;
		public decimal brute;
		public decimal burn;
		public decimal toxin;
		public decimal oxygen;
		public bool isBleeding;
		public BodyPartType type;
		public DamageSeverity severity;
		public List<BodyPartRecord> organs = new List<BodyPartRecord>();

		public BodyPartRecord Copy(BodyPart part)
        {
			name = part.gameObject.ExpensiveName();
			brute = Math.Round((decimal)part.Brute, 1);
			burn = Math.Round((decimal)part.Burn, 1);
			toxin = Math.Round((decimal)part.Toxin, 1);
			oxygen = Math.Round((decimal)part.Oxy, 1);
			isBleeding = part.IsBleeding;
			type = part.BodyPartType;
			severity = part.Severity;
			for (int i = 0; i < part.ContainBodyParts.Count; i++)
			{
				organs.Add(new BodyPartRecord());
				organs[i].name = part.ContainBodyParts[i].gameObject.ExpensiveName();
				organs[i].brute = Math.Round((decimal)part.ContainBodyParts[i].Brute, 1);
				organs[i].burn = Math.Round((decimal)part.ContainBodyParts[i].Burn, 1);
				organs[i].toxin = Math.Round((decimal)part.ContainBodyParts[i].Toxin, 1);
				organs[i].oxygen = Math.Round((decimal)part.ContainBodyParts[i].Oxy, 1);
				organs[i].isBleeding = part.ContainBodyParts[i].IsBleeding;
			}
			return this;
		}

		public static BodyPartRecord FromCompactString(string compactString)
		{
			var parts = compactString.Split(',', StringSplitOptions.RemoveEmptyEntries);
			var bodyPart = new BodyPartRecord
			{
				name = parts[0],
				brute = decimal.Parse(parts[1]),
				burn = decimal.Parse(parts[2]),
				toxin = decimal.Parse(parts[3]),
				oxygen = decimal.Parse(parts[4]),
				isBleeding = bool.Parse(parts[5]),
				type = (BodyPartType)int.Parse(parts[6]),
				severity = (DamageSeverity)int.Parse(parts[7])
			};

			// Deserialize any organs
			var organParts = compactString.Split('|', StringSplitOptions.RemoveEmptyEntries);
			for (int i = 1; i < organParts.Length; i++)  // Start from 1 because the first part is the main body part
			{
				bodyPart.organs.Add(FromCompactString(organParts[i]));
			}

			return bodyPart;
		}

		public string ToCompactString()
		{
			var sb = new StringBuilder();
			sb.Append(name).Append(",")
				.Append(brute.ToString("F1")).Append(",")
				.Append(burn.ToString("F1")).Append(",")
				.Append(toxin.ToString("F1")).Append(",")
				.Append(oxygen.ToString("F1")).Append(",")
				.Append(isBleeding).Append(",")
				.Append((int)type).Append(",")
				.Append((int)severity);
			foreach (var organ in organs)
			{
				sb.Append("|").Append(organ.ToCompactString());
			}
			return sb.ToString();
		}
    }
}