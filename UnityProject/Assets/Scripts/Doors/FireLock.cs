using UnityEngine;
using UnityEngine.Events;

public class FireLock : InteractableDoor
{
	private MetaDataNode metaNode;
	public FireAlarm fireAlarm;

	public override void TryClose()
	{
	}

	public override void TryOpen(GameObject performer)
	{

	}

	void TriggerAlarm()
	{
		if (fireAlarm)
		{
			fireAlarm.SendCloseAlerts();
		}
		else
		{
			ReceiveAlert();
		}
	}

	public void ReceiveAlert()
	{
		if (!Controller.IsClosed)
		{
			Controller.ServerTryClose();
		}
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		var integrity = GetComponent<Integrity>();
		integrity.OnExposedEvent.AddListener(TriggerAlarm);
		RegisterTile registerTile = GetComponent<RegisterTile>();
		MetaDataLayer metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
		metaNode = metaDataLayer.Get(registerTile.LocalPositionServer, false);
		Controller.ServerOpen();
	}


}
