using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

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

		if ( NetworkObject != null)
		{
			NetworkObject.GetComponent<hydroponicsTray>()?.ReceiveMessage(PlantSyncString,GrowingPlantStage,PlantSyncStage);
		}
		yield return null;
	}

	public static PlantTrayMessage Send(GameObject Tray, string Plant, int  _GrowingPlantStage,  PlantSpriteStage _PlantSyncStage )
	{
		PlantTrayMessage msg = new PlantTrayMessage
		{
			Tray = Tray.NetId(),
			PlantSyncString = Plant,
			GrowingPlantStage = _GrowingPlantStage,
			PlantSyncStage = _PlantSyncStage
		};
		msg.SendToAll();
		return msg;
	}
}
