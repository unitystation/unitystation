using UnityEngine;
using Mirror;
using UnityEngine.Serialization;

public class APCPoweredDevice : NetworkBehaviour, IServerDespawn, ISetMultitoolSlave
{
	[SerializeField][FormerlySerializedAs("MinimumWorkingVoltage")]
	private float minimumWorkingVoltage = 190;

	[SerializeField][FormerlySerializedAs("ExpectedRunningVoltage")]
	private float expectedRunningVoltage = 240;

	[SerializeField][FormerlySerializedAs("MaximumWorkingVoltage")]
	private float maximumWorkingVoltage = 300;

	[SerializeField][Tooltip("Category of this powered device. Different categories work like a set of breakers, so you" +
	                         " can turn off lights and keep machines working.")]
	private DeviceType deviceType = DeviceType.None;

	[SerializeField][Tooltip("This device powers itself and doesn't need an APC")]
	private bool isSelfPowered = false;

	public bool IsSelfPowered => isSelfPowered;

	[SerializeField][Tooltip("Watts consumed per update when running at 240v")]
	private float wattusage = 0.01f;

	public float Wattusage
	{
		get => wattusage;
		set
		{
			wattusage = value;
			resistance = 240 / (value / 240);
		}
	}

	[SerializeField][FormerlySerializedAs("Resistance")]
	private float resistance = 99999999;

	public float Resistance
	{
		get => resistance;
		set => resistance = value;
	}

	public APC RelatedAPC;
	public IAPCPowered Powered;
	public bool AdvancedControlToScript;

	public bool StateUpdateOnClient = true;

	[SyncVar(hook = nameof(UpdateSynchronisedState))]
	public PowerStates State = PowerStates.Off;


	[SyncVar(hook = nameof(UpdateSynchronisedVoltage))]
	public float RecordedVoltage = 0;

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
			resistance = 240 / (Wattusage / 240);
		}
	}

	private void EnsureInit()
	{
		if (Powered != null) return;
		Powered = GetComponent<IAPCPowered>();
		if (isSelfPowered)
		{
			if (AdvancedControlToScript)
			{
				RecordedVoltage = expectedRunningVoltage;
				Powered?.PowerNetworkUpdate(expectedRunningVoltage);
			}
			else
			{
				Powered?.StateUpdate( PowerStates.On );
			}

		}
	}

	public override void OnStartClient()
	{
		EnsureInit();
		if (AdvancedControlToScript)
		{
			UpdateSynchronisedVoltage(RecordedVoltage, RecordedVoltage);
		}
		else
		{
			UpdateSynchronisedState(State, State);
		}
	}

	public override void OnStartServer()
	{
		EnsureInit();
		if (AdvancedControlToScript)
		{
			UpdateSynchronisedVoltage(RecordedVoltage, RecordedVoltage);
		}
		else
		{
			UpdateSynchronisedState(State, State);
		}
	}

	public void PowerNetworkUpdate(float Voltage) //Could be optimised to not update when voltage is same as previous voltage
	{
		if (AdvancedControlToScript)
		{
			RecordedVoltage = Voltage;
			Powered?.PowerNetworkUpdate(Voltage);
		}
		else
		{
			var NewState = PowerStates.Off;
			if (Voltage <= 1)
			{
				NewState = PowerStates.Off;
			}
			else if (Voltage > maximumWorkingVoltage)
			{
				NewState = PowerStates.OverVoltage;
			}
			else if (Voltage < minimumWorkingVoltage)
			{
				NewState = PowerStates.LowVoltage;
			}
			else {
				NewState = PowerStates.On;
			}

			if (NewState == State) return;
			State = NewState;
			Powered?.StateUpdate(State);
		}
	}

	private void UpdateSynchronisedVoltage(float _OldVoltage,float _NewVoltage)
	{
		EnsureInit();
		if (_OldVoltage != _NewVoltage)
		{
			Logger.LogTraceFormat("{0}({1}) state changing {2} to {3}", Category.Electrical, name, transform.position.To2Int(), _OldVoltage, _NewVoltage);
		}

		RecordedVoltage = _NewVoltage;
		if (isSelfPowered)
		{
			RecordedVoltage = expectedRunningVoltage;
		}

		if (Powered != null && StateUpdateOnClient && AdvancedControlToScript)
		{
			if (isSelfPowered)
			{
				Powered?.PowerNetworkUpdate( expectedRunningVoltage );
			}
			else
			{
				Powered?.PowerNetworkUpdate(_NewVoltage);
			}
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

		if (isSelfPowered)
		{
			State = PowerStates.On;
		}

		if (Powered != null && StateUpdateOnClient)
		{
			if (isSelfPowered)
			{
				Powered?.StateUpdate( PowerStates.On );
			}
			else
			{
				Powered?.StateUpdate(State);
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

	public static bool IsOn(PowerStates States)
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

