using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Main component for cloning console.
/// </summary>
public class CloningConsole : MonoBehaviour, IServerSpawn
{
	private List<CloningRecord> cloningRecords = new List<CloningRecord>();

	private DNAscanner scanner;
	/// <summary>
	/// Scanner this is attached to. Null if none found.
	/// </summary>
	public DNAscanner Scanner => scanner;

	private CloningPod cloningPod;
	/// <summary>
	/// Cloning pod this is attached to. Null if none found.
	/// </summary>
	public CloningPod CloningPod => cloningPod;

	private GUI_Cloning consoleGUI;
	private RegisterTile registerTile;

	/// <summary>
	/// Saved cloning records.
	/// </summary>
	public IEnumerable<CloningRecord> CloningRecords => cloningRecords;

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
	}


	public void OnSpawnServer(SpawnInfo info)
	{
		scanner = null;
		cloningPod = null;
		consoleGUI = null;
		//TODO: Support persistance of this info somewhere, such as to a circuit board.
		//scan for adjacent dna scanner and cloning pod
		scanner = MatrixManager.GetAdjacent<DNAscanner>(registerTile.WorldPositionServer, true).FirstOrDefault();
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
		if(scanner && scanner.IsClosed && scanner.Powered)
		{
			scanner.ServerToggleLocked();
		}
	}

	public void Scan()
	{
		if (scanner && scanner.occupant && scanner.Powered)
		{
			var mob = scanner.occupant;
			var mobID = scanner.occupant.mobID;
			var playerScript = mob.GetComponent<PlayerScript>();
			if(playerScript?.mind?.bodyMobID != mobID)
			{
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
	}

	public void ServerTryClone(CloningRecord record)
	{
		if (cloningPod && cloningPod.CanClone())
		{
			var status = record.mind.GetCloneableStatus(record.mobID);

			if(status == CloneableStatus.Cloneable)
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

	private void CreateRecord(LivingHealthBehaviour mob, PlayerScript playerScript)
	{
		var record = new CloningRecord();
		record.UpdateRecord(mob, playerScript);
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

	public void UpdateRecord(LivingHealthBehaviour mob, PlayerScript playerScript)
	{
		mobID = mob.mobID;
		mind = playerScript.mind;
		name = playerScript.playerName;
		characterSettings = playerScript.characterSettings;
		oxyDmg = mob.bloodSystem.oxygenDamage;
		burnDmg = mob.GetTotalBurnDamage();
		toxinDmg = 0;
		bruteDmg = mob.GetTotalBruteDamage();
		uniqueIdentifier = "35562Eb18150514630991";
	}
}
