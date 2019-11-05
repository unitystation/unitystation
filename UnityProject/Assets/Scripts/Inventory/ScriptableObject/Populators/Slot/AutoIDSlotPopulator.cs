

using UnityEngine;

/// <summary>
/// Slot populator which only works in the context of spawning a player.
/// Creates an ID with an ACL based on the Occupation of the player being spawned.
/// </summary>
[CreateAssetMenu(fileName = "AutoIDSlotPopulator", menuName = "Inventory/Populators/Slot/AutoIDSlotPopulator")]
public class AutoIDSlotPopulator : SlotPopulator
{
	[Tooltip("Prefab to use for the ID")]
	public GameObject idPrefab;

	public override void PopulateSlot(ItemSlot toPopulate, PopulationContext context)
	{
		var occupation = PopulatorUtils.TryGetOccupation(context);
		if (occupation == null) return;

		var idObj = Spawn.ServerPrefab(idPrefab).GameObject;
		var charSettings = context.SpawnInfo.CharacterSettings;
		var jobType = occupation.JobType;
		if (jobType == JobType.CAPTAIN)
		{
			idObj.GetComponent<IDCard>().Initialize(IDCardType.captain, jobType, occupation.AllowedAccess, charSettings.Name);
		}
		else if (jobType == JobType.HOP || jobType == JobType.HOS || jobType == JobType.CMO || jobType == JobType.RD ||
		         jobType == JobType.CHIEF_ENGINEER)
		{
			idObj.GetComponent<IDCard>().Initialize(IDCardType.command, jobType, occupation.AllowedAccess, charSettings.Name);
		}
		else
		{
			idObj.GetComponent<IDCard>().Initialize(IDCardType.standard, jobType, occupation.AllowedAccess, charSettings.Name);
		}

		Inventory.ServerAdd(idObj, toPopulate);

	}
}
