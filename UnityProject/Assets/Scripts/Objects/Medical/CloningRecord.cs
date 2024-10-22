using System;
using System.Collections.Generic;
using System.Text;
using HealthV2;
using Logs;
using Newtonsoft.Json;
using Systems.Character;
using Random = UnityEngine.Random;

namespace Objects.Medical
{
	public class CloningRecord
	{
		public string name;
		public string scanID;
		public int oxyDmg;
		public int burnDmg;
		public int toxinDmg;
		public int bruteDmg;
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
			oxyDmg = (int)livingHealth.GetOxyDamage;
			burnDmg = (int)livingHealth.GetTotalBurnDamage();
			toxinDmg = (int)livingHealth.GetTotalToxDamage();
			bruteDmg = (int)livingHealth.GetTotalBruteDamage();
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
			return ToJSON();
		}

		public static CloningRecord FromString(string recordData)
		{
			return FromJSON(recordData);
		}

		private string ToJSON()
		{
			return JsonConvert.SerializeObject(this);
		}

		private static CloningRecord FromJSON(string json)
		{
			return JsonConvert.DeserializeObject<CloningRecord>(json);
		}
	}


	public class BodyPartRecord
    {
		public string name;
		public float brute;
		public float burn;
		public float toxin;
		public float oxygen;
		public bool isBleeding;
		public BodyPartType type;
		public DamageSeverity severity;
		public List<BodyPartRecord> organs = new List<BodyPartRecord>();

		public BodyPartRecord Copy(BodyPart part)
        {
			name = part.gameObject.ExpensiveName();
			brute = (float)Math.Round(part.Brute, 1);
			burn = (float)Math.Round(part.Burn, 1);
			toxin = (float)Math.Round(part.Toxin, 1);
			oxygen = (float)Math.Round(part.Oxy, 1);
			isBleeding = part.IsBleeding;
			type = part.BodyPartType;
			severity = part.Severity;
			for (int i = 0; i < part.ContainBodyParts.Count; i++)
			{
				organs.Add(new BodyPartRecord());
				organs[i].name = part.ContainBodyParts[i].gameObject.ExpensiveName();
				organs[i].brute = (float)Math.Round(part.ContainBodyParts[i].Brute, 1);
				organs[i].burn = (float)Math.Round(part.ContainBodyParts[i].Burn, 1);
				organs[i].toxin = (float)Math.Round(part.ContainBodyParts[i].Toxin, 1);
				organs[i].oxygen = (float)Math.Round(part.ContainBodyParts[i].Oxy, 1);
				organs[i].isBleeding = part.ContainBodyParts[i].IsBleeding;
			}
			return this;
		}
    }
}