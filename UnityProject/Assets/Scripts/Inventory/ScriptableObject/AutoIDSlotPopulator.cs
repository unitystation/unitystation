

using UnityEngine;

/// <summary>
/// Slot populator which creates an ID with an ACL based on the job type of the brain who owns the slot
/// being populated.
/// </summary>
[CreateAssetMenu(fileName = "AutoIDSlotPopulator", menuName = "Inventory/AutoIDSlotPopulator")]
public class AutoIDSlotPopulator : SlotPopulator
{
	[Tooltip("Occupation list containing the occupations to use to look up the access info.")]
	public OccupationList OccupationList;

	[Tooltip("Prefab to use for the ID")]
	public GameObject idPrefab;

	public override void PopulateSlot(ItemSlot toPopulate)
	{
		var ps = toPopulate.GetRootStorage().GetComponent<PlayerScript>();
		if (ps == null)
		{
			Logger.LogErrorFormat("Unable to auto-populate id into slot {0}, this slot is not" +
			                      " in the inventory of a player with a PlayerScript.", Category.Inventory, toPopulate);
			return;
		}
		var mind = ps.mind;

		if (mind == null)
		{
			Logger.LogErrorFormat("Unable to auto-populate id into slot {0}, this playerscript doesn't have" +
			                      " a mind.", Category.Inventory, toPopulate);
			return;
		}

		var jobType = mind.jobType;
		if (jobType == JobType.NULL)
		{
			Logger.LogErrorFormat("Unable to auto-populate id into slot {0}, this mind doesn't have a job specified.", Category.Inventory, toPopulate);
			return;
		}

		//spawn the id and put it into the slot
		var realName = ps.playerName;
		var idObj = Spawn.ServerPrefab(idPrefab).GameObject;

		var occupation = OccupationList.Get(jobType);
		if (occupation == null)
		{
			Logger.LogErrorFormat("Unable to auto-populate id into slot {0}, this jobtype {1} isn't defined in the OccupationList.",
				Category.Inventory, toPopulate, jobType);
			return;
		}

		if (jobType == JobType.CAPTAIN)
		{
			idObj.GetComponent<IDCard>().Initialize(IDCardType.captain, jobType, occupation.AllowedAccess, realName);
		}
		else if (jobType == JobType.HOP || jobType == JobType.HOS || jobType == JobType.CMO || jobType == JobType.RD ||
		         jobType == JobType.CHIEF_ENGINEER)
		{
			idObj.GetComponent<IDCard>().Initialize(IDCardType.command, jobType, occupation.AllowedAccess, realName);
		}
		else
		{
			idObj.GetComponent<IDCard>().Initialize(IDCardType.standard, jobType, occupation.AllowedAccess, realName);
		}

		Inventory.ServerAdd(idObj, toPopulate);

	}
}
