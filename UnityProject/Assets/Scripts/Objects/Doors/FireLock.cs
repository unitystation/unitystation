using UnityEngine;
using UnityEngine.Events;

public class FireLock : InteractableDoor, ISetMultitoolSlave
{
	private MetaDataNode metaNode;
	public FireAlarm fireAlarm;

	[SerializeField]
	private MultitoolConnectionType conType = MultitoolConnectionType.FireAlarm;
	public MultitoolConnectionType ConType  => conType;

	public void SetMaster(ISetMultitoolMaster Imaster)
	{
		if (fireAlarm)
		{
			fireAlarm.FireLockList.Remove(this);
		}
		fireAlarm = (Imaster as Component)?.gameObject.GetComponent<FireAlarm>();
		fireAlarm.FireLockList.Add(this);

	}

	public override void TryClose()
	{
	}

	public override void TryOpen(GameObject performer)
	{
	}

	void TriggerAlarm()
	{
		if (!Controller.IsWelded)
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
	}

	public void ReceiveAlert()
	{
		if (Controller == null)
			return;
		Controller.CloseSignal();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		var integrity = GetComponent<Integrity>();
		integrity.OnExposedEvent.AddListener(TriggerAlarm);
		RegisterTile registerTile = GetComponent<RegisterTile>();
		MetaDataLayer metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
		metaNode = metaDataLayer.Get(registerTile.LocalPositionServer, false);
		Controller.Open();
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
