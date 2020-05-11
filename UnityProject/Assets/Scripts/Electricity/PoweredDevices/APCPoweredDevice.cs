using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Mirror;
using Debug = UnityEngine.Debug;

public class APCPoweredDevice : NetworkBehaviour, IServerDespawn
{
	public float MinimumWorkingVoltage = 190;
	public float MaximumWorkingVoltage = 300;

	public DeviceType deviceType = DeviceType.None;

	[SerializeField]
	private bool isSelfPowered = false;

	public bool IsSelfPowered => isSelfPowered;

	[SerializeField]
	private float wattusage = 0.01f;

	public float Wattusage
	{
		get { return wattusage; }
		set
		{
			wattusage = value;
			Resistance = 240 / (value / 240);
		}
	}

	public float Resistance = 99999999;
	public APC RelatedAPC;
	public IAPCPowered Powered;
	public bool AdvancedControlToScript;

	public bool StateUpdateOnClient = true;

	[SyncVar(hook = nameof(UpdateSynchronisedState))]
	public PowerStates State;

	private void Awake()
	{
		EnsureInit();
	}

	void Start()
	{
		Logger.LogTraceFormat("{0}({1}) starting, state {2}", Category.Electrical, name, transform.position.To2Int(), State);
		if (Wattusage > 0)
		{
			Resistance = 240 / (Wattusage / 240);
		}
	}

	private void EnsureInit()
	{
		if (Powered != null) return;
		Powered = GetComponent<IAPCPowered>();
	}

	public void SetAPC(APC _APC)
	{
		if (_APC == null) return;
		RemoveFromAPC();
		RelatedAPC = _APC;
		RelatedAPC.ConnectedDevices.Add(this);
	}

	public void RemoveFromAPC()
	{
		if (RelatedAPC == null) return;
		if (RelatedAPC.ConnectedDevices.Contains(this))
		{
			RelatedAPC.ConnectedDevices.Remove(this);
			PowerNetworkUpdate(0.1f);
		}
	}

	public override void OnStartClient()
	{
		EnsureInit();
		UpdateSynchronisedState(State, State);
	}

	public override void OnStartServer()
	{
		EnsureInit();
		UpdateSynchronisedState(State, State);
	}

	public void PowerNetworkUpdate(float Voltage) //Could be optimised to not update when voltage is same as previous voltage
	{
		if (Powered == null) return;
		if (AdvancedControlToScript)
		{
			Powered.PowerNetworkUpdate(Voltage);
		}
		else
		{
			var NewState = PowerStates.Off;
			if (Voltage <= 1)
			{
				NewState = PowerStates.Off;
			}
			else if (Voltage > MaximumWorkingVoltage)
			{
				NewState = PowerStates.OverVoltage;
			}
			else if (Voltage < MinimumWorkingVoltage)
			{
				NewState = PowerStates.LowVoltage;
			}
			else {
				NewState = PowerStates.On;
			}

			if (NewState == State) return;
			State = NewState;
			Powered.StateUpdate(State);
		}
	}

	private void UpdateSynchronisedState(PowerStates _OldState, PowerStates _State)
	{
		EnsureInit();
		if (_State != State)
		{
			Logger.LogTraceFormat("{0}({1}) state changing {2} to {3}", Category.Electrical, name, transform.position.To2Int(), State, _State);
		}

		State = _State;
		if (Powered != null && StateUpdateOnClient)
		{
			Powered.StateUpdate(State);
		}
	}

	void OnDrawGizmosSelected()
	{
		if (RelatedAPC == null)
		{
			if (isSelfPowered) return;
			Gizmos.color = new Color(1f, 0f, 0, 1);
			Gizmos.DrawCube(gameObject.transform.position,new Vector3(0.3f,0.3f));
			return;
		}

		//Highlighting APC
		Gizmos.color = new Color(0.5f, 0.5f, 1, 1);
		Gizmos.DrawLine(RelatedAPC.transform.position, gameObject.transform.position);
		Gizmos.DrawSphere(RelatedAPC.transform.position, 0.15f);
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		RemoveFromAPC();
	}
}

public enum DeviceType
{
	None,
	Lights,
	Environment,
	Equipment
}

public enum PowerStates
{
	Off,
	LowVoltage,
	On,
	OverVoltage,
}