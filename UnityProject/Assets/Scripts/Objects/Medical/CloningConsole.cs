using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Admin.Logs;
using HealthV2;
using UnityEngine;
using Random = UnityEngine.Random;
using Health.Sickness;
using Mirror;
using Systems.Character;
using UI.Objects.Medical.Cloning;

namespace Objects.Medical
{
	/// <summary>
	/// Main component for cloning console.
	/// </summary>
	[RequireComponent(typeof(ItemStorage))]
	public class CloningConsole : MonoBehaviour, ICheckedInteractable<HandApply>, IServerSpawn
	{
		[SerializeField] private GameObject paperPrefab;

		private DNAScanner scanner;
		/// <summary>
		/// Scanner this is attached to. Null if none found.
		/// </summary>
		public DNAScanner Scanner => scanner;

		private CloningPod cloningPod;
		/// <summary>
		/// Cloning pod this is attached to. Null if none found.
		/// </summary>
		public CloningPod CloningPod => cloningPod;

		private CloningRecord currentRecord;
		public CloningRecord CurrentRecord => currentRecord;

		private GUI_Cloning consoleGUI;
		private RegisterTile registerTile;
		private ClosetControl closet;
		private ItemStorage recordsStorage;
		private HasNetworkTab networkTab;

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			closet = GetComponent<ClosetControl>();
			recordsStorage = GetComponent<ItemStorage>();
			networkTab = GetComponent<HasNetworkTab>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			scanner = null;
			cloningPod = null;
			consoleGUI = null;

			//TODO: Support persistance of this info somewhere, such as to a circuit board.
			//scan for adjacent dna scanner and cloning pod
			scanner = MatrixManager.GetAdjacent<DNAScanner>(registerTile.WorldPositionServer, true).FirstOrDefault();
			cloningPod = MatrixManager.GetAdjacent<CloningPod>(registerTile.WorldPositionServer, true).FirstOrDefault();

			if (cloningPod)
			{
				cloningPod.console = this;
			}
		}

		/// <summary>
		/// Toggle the locking / unlocking of the scanner.
		/// </summary>
		public void ServerToggleLock()
		{
			if (Inoperable())
			{
				UpdateInoperableStatus();
				return;
			}
			if (closet.IsOpen == false)
			{
				closet.SetLock(closet.IsLocked ? ClosetControl.Lock.Unlocked : ClosetControl.Lock.Locked);
				scanner.statusString = closet.IsLocked ? "Scanner locked." : "Scanner unlocked.";
			}
			else
			{
				scanner.statusString = "Scanner is not closed.";
			}
		}

		[RightClickMethod()]
		[NaughtyAttributes.Button()]

		public void Scan()
		{
			if (Inoperable())
			{
				UpdateInoperableStatus();
				return;
			}

			if (scanner.occupant)
			{
				var mob = scanner.occupant;
				var mobID = scanner.occupant.mobID;
				var playerScript = mob.GetComponent<PlayerScript>();
				if (playerScript.OrNull()?.Mind?.bodyMobID != mobID)
				{
					scanner.statusString = "Bad mind/body interface.";
					return;
				}

				CreateRecord(mob, playerScript);
				scanner.statusString = "Subject successfully scanned.";
			}
			else
			{
				scanner.statusString = "Scanner is empty.";
			}
		}

		private bool Inoperable()
		{
			return !(scanner && scanner.RelatedAPC && scanner.Powered);
		}

		private void UpdateInoperableStatus()
		{
			if (scanner.RelatedAPC == null)
			{
				scanner.statusString = "Scanner not connected to APC!";
				return;
			}

			if (scanner.Powered == false)
			{
				scanner.statusString = "Voltage too low!";
			}
		}

		public void ServerTryClone(CloningRecord record)
		{
			if (cloningPod && cloningPod.CanClone())
			{
				Mind mind = NetworkUtils.FindObjectOrNull(record.mindID)?.GetComponent<Mind>();
				if (mind == null)
				{
					return;
				}
				else
				{
					record.mind = mind;
				}
				CloneableStatus status = mind.GetCloneableStatus(record.mobID);

				if (status == CloneableStatus.Cloneable)
				{
					cloningPod.ServerStartCloning(record);
					recordsStorage.ServerDropAll();
					consoleGUI.ViewMainPage();
				}
				else
				{
					cloningPod.UpdateStatusString(status);
				}
			}
		}

		private void CreateRecord(LivingHealthMasterBase livingHealth, PlayerScript playerScript)
		{
			var record = new CloningRecord();
			record.UpdateRecord(livingHealth, playerScript);
			AdminLogsManager.AddNewLog(
				null,
				$"{gameObject.ExpensiveName()} at {gameObject.AssumedWorldPosServer()}" +
				$" has created a new cloning record for {playerScript.playerName}.",
				LogCategory.RoundFlow);
			var paper = Spawn.ServerPrefab(paperPrefab, gameObject.AssumedWorldPosServer());
			paper.GameObject.GetComponent<Paper>().SetServerString(record.ToCompactString());
		}

		public void UpdateDisplay()
		{
			consoleGUI.OrNull()?.UpdateDisplay();
		}

		public void RegisterConsoleGUI(GUI_Cloning guiCloning)
		{
			consoleGUI = guiCloning;
		}

		public void RemoveRecord()
		{
			recordsStorage.ServerDropAll();
			currentRecord = null;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject == null)
			{
				networkTab.ServerPerformInteraction(interaction);
				return;
			}
			if (interaction.HandObject.TryGetComponent<Paper>(out var p) == false) return;
			var record = CloningRecord.FromCompactString(p.ServerString);
			if (record == null)
			{
				Chat.AddExamineMsg(interaction.Performer, "The console spits the paper out back into your hand..");
				return;
			}
			else
			{
				Chat.AddExamineMsg(interaction.Performer, "The paper disappears wihtin the cloning console.");
			}
			recordsStorage.ServerTryTransferFrom(interaction.HandSlot);
			currentRecord = record;
			consoleGUI?.UpdateDisplay();
		}
	}

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

		// Method to serialize the object to a compact string
		public string ToCompactString()
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

		// Method to deserialize the object from a compact string
		public static CloningRecord FromCompactString(string compactString)
		{
			var parts = compactString.Split('|');
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
			for (int i = 0; i < part.ContainBodyParts.Count(); i++)
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
