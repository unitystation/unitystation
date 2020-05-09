using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class FireAlarm : NetworkBehaviour, IServerLifecycle, ICheckedInteractable<HandApply>
{
	public List<FireLock> FireLockList = new List<FireLock>();
	private MetaDataNode metaNode;
	public bool activated = false;
	public float coolDownTime = 1.0f;
	public bool isInCooldown = false;

	public void SendCloseAlerts()
	{
		if (!activated && !isInCooldown)
		{
			activated = true;
			StartCoroutine(SwitchCoolDown());
			foreach (var firelock in FireLockList)
			{
				firelock.ReceiveAlert();
			}
		}
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		var integrity = GetComponent<Integrity>();
		integrity.OnExposedEvent.AddListener(SendCloseAlerts);
		AtmosManager.Instance.inGameFireAlarms.Add(this);
		RegisterTile registerTile = GetComponent<RegisterTile>();
		MetaDataLayer metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
		metaNode = metaDataLayer.Get(registerTile.LocalPositionServer, false);
		foreach (var firelock in FireLockList)
		{
			firelock.fireAlarm = this;
		}
	}

	public void TickUpdate()
	{
		if (!activated)
		{
			if (metaNode.GasMix.Pressure < AtmosConstants.WARNING_LOW_PRESSURE || metaNode.GasMix.Pressure > AtmosConstants.WARNING_HIGH_PRESSURE)
			{
				SendCloseAlerts();
			}
		}
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		AtmosManager.Instance.inGameFireAlarms.Remove(this);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (activated && !isInCooldown)
		{
			activated = false;
			StartCoroutine(SwitchCoolDown());
			foreach (var firelock in FireLockList)
			{
				if (firelock.Controller.IsClosed)
				{
					firelock.Controller.ServerOpen();
				}
			}
		}
		else
		{
			SendCloseAlerts();
		}
	}

	private IEnumerator SwitchCoolDown()
	{
		isInCooldown = true;
		yield return WaitFor.Seconds(coolDownTime);
		isInCooldown = false;
	}
}

