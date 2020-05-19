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
	
	//Copied over from LightSource.cs
	void OnDrawGizmosSelected()
	{
		var sprite = GetComponentInChildren<SpriteRenderer>();
		if (sprite == null)
			return;
		if (fireAlarm == null)
			return;
		//Highlight associated fireAlarm.
		Gizmos.color = new Color(1, 0.5f, 0, 1);
		Gizmos.DrawLine(fireAlarm.transform.position, gameObject.transform.position);
		Gizmos.DrawSphere(fireAlarm.transform.position, 0.25f);
	}
}
