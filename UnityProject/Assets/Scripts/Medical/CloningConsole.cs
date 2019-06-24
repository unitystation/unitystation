using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main component for cloning console
/// </summary>
public class CloningConsole : MonoBehaviour
{
	public delegate void ChangeEvent();
	public static event ChangeEvent changeEvent;

	public List<CloningRecord> CloningRecords = new List<CloningRecord>();
	public DNAscanner scanner;

	public void ToggleLock()
	{
		if(scanner && scanner.IsClosed)
		{
			scanner.IsLocked = !scanner.IsLocked;
		}
	}

	public void Scan()
	{
		if (scanner && scanner.occupant)
		{
			var mob = scanner.occupant;
			var mobID = scanner.occupant.mobID;
			var playerScript = mob.GetComponent<PlayerScript>();
			var mind = playerScript.mind;
			var name = playerScript.playerName;
			var characterSettings = playerScript.CharacterSettings;
			var oxyDmg = mob.bloodSystem.oxygenDamage;
			var burnDmg = mob.GetTotalBurnDamage();
			var toxinDmg = 0;
			var bruteDmg = mob.GetTotalBruteDamage();
			var uniqueIdentifier = "35562Eb18150514630991";
			for (int i = 0; i < CloningRecords.Count; i++)
			{
				var record = CloningRecords[i];
				if (mobID == record.mobID)
				{
					record.UpdateRecord(name, oxyDmg, burnDmg, toxinDmg, bruteDmg, uniqueIdentifier, characterSettings, mobID, mind);
					return;
				}
			}
			CreateRecord(name, oxyDmg, burnDmg, toxinDmg, bruteDmg, uniqueIdentifier, characterSettings, mobID, mind);
		}
	}

	public void Clone(CloningRecord record)
	{
		record.mind.ClonePlayer(gameObject, record.characterSettings);
	}

	private void CreateRecord(string name, float oxyDmg, float burnDmg, float toxinDmg, float bruteDmg, string uniqueIdentifier, CharacterSettings characterSettings, int mobID, Mind mind)
	{

		var record = new CloningRecord();
		record.UpdateRecord(name, oxyDmg, burnDmg, toxinDmg, bruteDmg, uniqueIdentifier, characterSettings, mobID, mind);
		CloningRecords.Add(record);
	}

}

public class CloningRecord
{
	public string Name;
	public string ScanID;
	public float OxyDmg;
	public float BurnDmg;
	public float ToxinDmg;
	public float BruteDmg;
	public string UniqueIdentifier;
	public CharacterSettings characterSettings;
	public int mobID;
	public Mind mind;

	public CloningRecord()
	{
		int scanID = Random.Range(0, 1000);
		ScanID = scanID.ToString();
	}

	public void UpdateRecord(string name, float oxyDmg, float burnDmg, float toxinDmg, float bruteDmg, string uniqueIdentifier, CharacterSettings charSettings, int newID, Mind newMind)
	{
		Name = name;
		OxyDmg = oxyDmg;
		BurnDmg = burnDmg;
		ToxinDmg = toxinDmg;
		BruteDmg = bruteDmg;
		UniqueIdentifier = uniqueIdentifier;
		characterSettings = charSettings;
		mobID = newID;
		mind = newMind;
	}
}