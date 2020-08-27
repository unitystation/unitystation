using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Antagonists;
using UnityEngine;
using Mirror;
using Debug = UnityEngine.Debug;

public class APCPoweredDevice : NetworkBehaviour, IServerDespawn, ISetMultitoolSlave
{
	public float MinimumWorkingVoltage = 190;
	public float ExpectedRunningVoltage = 240;
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

	public bool SelfPowered = false;

	[SyncVar(hook = nameof(UpdateSynchronisedState))]
	public PowerStates State;

	[SerializeField]
	private MultitoolConnectionType conType = MultitoolConnectionType.APC;
	public MultitoolConnectionType ConType  => conType;

	public void SetMaster(ISetMultitoolMaster Imaster)
	{
		var InAPC = (Imaster as Component)?.gameObject.GetComponent<APC>();
		if (RelatedAPC != null)
		{
			RemoveFromAPC();
		}
		RelatedAPC = InAPC;
		RelatedAPC.AddDevice(this);
	}

	/// <summary>
	/// In case is a bit more tidy up needed when removing APC so not doing it it from APC end
	/// </summary>
	public void RemoveFromAPC()
	{
		if (RelatedAPC == null) return;
		RelatedAPC.RemoveDevice(this);
	}

	private void Awake()
	{
		EnsureInit();
	}

	void Start()
	{
		//Logger.LogTraceFormat("{0}({1}) starting, state {2}", Category.Electrical, name, transform.position.To2Int(), State);
		if (Wattusage > 0)
		{
			Resistance = 240 / (Wattusage / 240);
		}
	}

	private void EnsureInit()
	{
		if (Powered != null) return;
		Powered = GetComponent<IAPCPowered>();
		if (Powered == null) return;
		if (SelfPowered)
		{
			if (AdvancedControlToScript)
			{
				Powered.PowerNetworkUpdate(ExpectedRunningVoltage);
			}
			else
			{
				Powered.StateUpdate( PowerStates.On );
			}

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
			if (SelfPowered)
			{
				Powered.StateUpdate( PowerStates.On );
			}
			else
			{
				Powered.StateUpdate(State);
			}
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

	public static bool  IsOn(PowerStates States)
	{
		return (States == PowerStates.On || States == PowerStates.LowVoltage || States == PowerStates.OverVoltage);
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

