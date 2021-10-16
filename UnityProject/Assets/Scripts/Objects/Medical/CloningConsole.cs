using System;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using UnityEngine;
using UI.Objects.Medical;
using Random = UnityEngine.Random;

namespace Objects.Medical
{
	/// <summary>
	/// Main component for cloning console.
	/// </summary>
	public class CloningConsole : MonoBehaviour, IServerSpawn
	{
		private List<CloningRecord> cloningRecords = new List<CloningRecord>();

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

		private GUI_Cloning consoleGUI;
		private RegisterTile registerTile;
		private ClosetControl closet;

		/// <summary>
		/// Saved cloning records.
		/// </summary>
		public IEnumerable<CloningRecord> CloningRecords => cloningRecords;

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			closet = GetComponent<ClosetControl>();
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
				if (playerScript?.mind?.bodyMobID != mobID)
				{
					scanner.statusString = "Bad mind/body interface.";
					return;
				}
				for (int i = 0; i < cloningRecords.Count; i++)
				{
					var record = cloningRecords[i];
					if (mobID == record.mobID)
					{
						record.UpdateRecord(mob, playerScript);
						scanner.statusString = "Record updated.";
						return;
					}
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
			if (!scanner.RelatedAPC)
				scanner.statusString = "Scanner not connected to APC.";
			else if (!scanner.Powered)
				scanner.statusString = "Voltage too low.";
		}

		public void ServerTryClone(CloningRecord record)
		{
			if (cloningPod && cloningPod.CanClone())
			{
				var status = record.mind.GetCloneableStatus(record.mobID);

				if (status == CloneableStatus.Cloneable)
				{
					cloningPod.ServerStartCloning(record);
					cloningRecords.Remove(record);
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
			cloningRecords.Add(record);
		}

		public void UpdateDisplay()
		{
			consoleGUI.UpdateDisplay();
		}

		public void RegisterConsoleGUI(GUI_Cloning guiCloning)
		{
			consoleGUI = guiCloning;
		}

		public void RemoveRecord(CloningRecord specificRecord)
		{
			cloningRecords.Remove(specificRecord);
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
		public CharacterSettings characterSettings;
		public int mobID;
		public Mind mind;

		public CloningRecord()
		{
			scanID = Random.Range(0, 9999).ToString();
		}

		public void UpdateRecord(LivingHealthMasterBase livingHealth, PlayerScript playerScript)
		{
			mobID = livingHealth.mobID;
			mind = playerScript.mind;
			name = playerScript.playerName;
			characterSettings = playerScript.characterSettings;
			oxyDmg = livingHealth.GetOxyDamage;
			burnDmg = livingHealth.GetTotalBurnDamage();
			toxinDmg = 0;
			bruteDmg = livingHealth.GetTotalBruteDamage();
			uniqueIdentifier = "35562Eb18150514630991";
		}
	}
}
