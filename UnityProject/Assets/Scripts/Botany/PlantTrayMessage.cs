using System.Collections;
using UnityEngine;

/// <summary>
/// Used to synchronise all the sprites of the PlantTray
/// </summary>
public class PlantTrayMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.PlantTrayMessage;
	public string PlantSyncString;
	public int GrowingPlantStage;
	public PlantSpriteStage PlantSyncStage;

	public uint Tray;

	public override IEnumerator Process()
	{
		yield return WaitFor(Tray);

		if (NetworkObject != null)
		{
			NetworkObject.GetComponent<HydroponicsTray>()
				?.ReceiveMessage(PlantSyncString, GrowingPlantStage, PlantSyncStage);
		}

		yield return null;
	}

	public static PlantTrayMessage Send(GameObject tray, string plant, int growingStage, PlantSpriteStage spriteStage)
	{
		PlantTrayMessage msg = new PlantTrayMessage
		{
			Tray = tray.NetId(),
			PlantSyncString = plant,
			GrowingPlantStage = growingStage,
			PlantSyncStage = spriteStage
		};
		msg.SendToAll();
		return msg;
	}
}