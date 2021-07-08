using System.Collections;
using UnityEngine;

/// <summary>
/// Used to synchronise all the sprites of the PlantTray
/// </summary>
/*public class PlantTrayMessage    : ServerMessage
{
	public override short MessageType => (short) MessageTypes.PlantTrayMessage;
	public string PlantSyncString;
	public int GrowingPlantStage;
	public PlantSpriteStage PlantSyncStage;
	public bool SyncHarvestNotifier;
	public bool SyncWeedNotifier;
	public bool SyncWaterNotifier;
	public bool SyncNutrimentNotifier;

	public uint Tray;

	public override void Process(NetMessage msg)
	{
		yield return WaitFor(Tray);

		if (NetworkObject != null)
		{
			NetworkObject.GetComponent<HydroponicsTray>()
				?.ReceiveMessage(PlantSyncString, GrowingPlantStage, PlantSyncStage,
					SyncHarvestNotifier, SyncWeedNotifier, SyncWaterNotifier, SyncNutrimentNotifier);
		}

		yield return null;
	}

	public static PlantTrayMessage SendToNearbyPlayers(GameObject tray,
		string plant, int growingStage, PlantSpriteStage spriteStage,
		bool harvestNotifier, bool weedNotifier, bool waterNotifier,
		bool nutrimentNotifier)
	{
		PlantTrayMessage msg = new PlantTrayMessage
		{
			Tray = tray.NetId(),
			PlantSyncString = plant,
			GrowingPlantStage = growingStage,
			PlantSyncStage = spriteStage,
			SyncHarvestNotifier = harvestNotifier,
			SyncNutrimentNotifier = nutrimentNotifier,
			SyncWaterNotifier = waterNotifier,
			SyncWeedNotifier = weedNotifier
		};
		msg.SendToAll();
		return msg;
	}
}*/