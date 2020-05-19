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

	public SpriteHandler spriteHandler;
	public Sprite topLightSpriteNormal;
	public SpriteSheetAndData topLightSpriteAlert;

	public void SendCloseAlerts()
	{
		if (!activated && !isInCooldown)
		{
			activated = true;
			spriteHandler.SetSprite(topLightSpriteAlert, 0);
			SoundManager.PlayNetworkedAtPos("FireAlarm", metaNode.Position);
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
		var wallMount = GetComponent<WallmountBehavior>();
		var direction = wallMount.CalculateFacing().CutToInt();
		metaNode = metaDataLayer.Get(registerTile.LocalPositionServer + direction, false);
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
			spriteHandler.SetSprite(topLightSpriteNormal);
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

	//Copied over from LightSwitchV2.cs
	void OnDrawGizmosSelected()
	{
		var sprite = GetComponentInChildren<SpriteRenderer>();
		if (sprite == null)
			return;

		//Highlighting all controlled FireLocks
		Gizmos.color = new Color(1, 0.5f, 0, 1);
		for (int i = 0; i < FireLockList.Count; i++)
		{
			var FireLock = FireLockList[i];
			if(FireLock == null) continue;
			Gizmos.DrawLine(sprite.transform.position, FireLock.transform.position);
			Gizmos.DrawSphere(FireLock.transform.position, 0.25f);
		}
	}

	private IEnumerator SwitchCoolDown()
	{
		isInCooldown = true;
		yield return WaitFor.Seconds(coolDownTime);
		isInCooldown = false;
	}
}

