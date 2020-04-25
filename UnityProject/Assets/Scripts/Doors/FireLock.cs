using UnityEngine;
using UnityEngine.Events;

public class FireLock : InteractableDoor, IServerLifecycle
{
	private MetaDataNode metaNode;

	public override void TryClose()
	{
	}

	public override void TryOpen(GameObject performer)
	{

	}

	public void TickUpdate()
	{
		if (!Controller.IsClosed)
		{
			if (metaNode.GasMix.Pressure < AtmosConstants.MINIMUM_OXYGEN_PRESSURE)
			{
				ReceiveAlert();
			}
		}
	}

	public void ReceiveAlert()
	{
		Controller.ServerTryClose();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		var integrity = GetComponent<Integrity>();
		integrity.OnExposedEvent.AddListener(ReceiveAlert);
		AtmosManager.Instance.inGameFireLocks.Add(this);
		RegisterTile registerTile = GetComponent<RegisterTile>();
		MetaDataLayer metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
		metaNode = metaDataLayer.Get(registerTile.LocalPositionServer, false);
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		AtmosManager.Instance.inGameFireLocks.Remove(this);
	}
}
