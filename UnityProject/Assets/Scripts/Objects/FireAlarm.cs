using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class FireAlarm : NetworkBehaviour, IServerLifecycle, ICheckedInteractable<HandApply>//,IAPCPowered
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
	/*
	public List<LightSource> listOfLights;

	public Action<bool> switchTriggerEvent;

	[SyncVar(hook = nameof(SyncState))]
	public bool isOn = true;

	[SerializeField]
	private float coolDownTime = 1.0f;

	private bool isInCoolDown;

	[SerializeField]
	private Sprite[] sprites;

	[SerializeField]
	private SpriteRenderer spriteRenderer;

	private PowerStates powerState = PowerStates.On;
	private void Awake()
	{
		foreach (var lightSource in listOfLights)
		{
			if(lightSource != null)
				lightSource.SubscribeToSwitchEvent(this);
		}
	}

	public override void OnStartClient()
	{
		SyncState(isOn, isOn);
		base.OnStartClient();
	}
	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false;
		return !isInCoolDown;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		StartCoroutine(SwitchCoolDown());
		if (powerState == PowerStates.Off || powerState == PowerStates.LowVoltage) return;
		ServerChangeState(!isOn);
	}

	private void SyncState(bool oldState, bool newState)
	{
		isOn = newState;
		spriteRenderer.sprite = isOn ? sprites[0] : sprites[1];
	}

	[Server]
	public void ServerChangeState(bool newState)
	{
		isOn = newState;
		switchTriggerEvent?.Invoke(isOn);
	}

	void OnDrawGizmosSelected()
	{
		var sprite = GetComponentInChildren<SpriteRenderer>();
		if (sprite == null)
			return;

		//Highlighting all controlled lightSources
		Gizmos.color = new Color(1, 1, 0, 1);
		for (int i = 0; i < listOfLights.Count; i++)
		{
			var lightSource = listOfLights[i];
			if(lightSource == null) continue;
			Gizmos.DrawLine(sprite.transform.position, lightSource.transform.position);
			Gizmos.DrawSphere(lightSource.transform.position, 0.25f);
		}
	}

	public void PowerNetworkUpdate(float Voltage)
	{

	}

	public void StateUpdate(PowerStates State)
	{
		switch (State)
		{
			case PowerStates.On:
				ServerChangeState(true);
				powerState = State;
				break;
			case PowerStates.LowVoltage:
				ServerChangeState(false);
				powerState = State;
				break;
			case PowerStates.OverVoltage:
				ServerChangeState(true);
				powerState = State;
				break;
			default:
				ServerChangeState(false);
				powerState = State;
				break;
		}
	}

	private IEnumerator SwitchCoolDown()
	{
		isInCoolDown = true;
		yield return WaitFor.Seconds(coolDownTime);
		isInCoolDown = false;
	}
	*/
}

