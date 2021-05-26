using System;
using UnityEngine;
using Mirror;
using UnityEngine.Serialization;
using Objects.Engineering;
using UnityEngine.Events;

namespace Systems.Electricity
{
	[ExecuteInEditMode]
	public class APCPoweredDevice : NetworkBehaviour, IServerDespawn, ISetMultitoolSlave
	{
		[SerializeField]
		[FormerlySerializedAs("MinimumWorkingVoltage")]
		private float minimumWorkingVoltage = 190;

		[SerializeField]
		[FormerlySerializedAs("ExpectedRunningVoltage")]
		private float expectedRunningVoltage = 240;

		[SerializeField]
		[FormerlySerializedAs("MaximumWorkingVoltage")]
		private float maximumWorkingVoltage = 300;

		[SerializeField]
		[Tooltip("Category of this powered device. " +
				"Different categories work like a set of breakers, so you can turn off lights and keep machines working.")]
		private DeviceType deviceType = DeviceType.None;

		[SerializeField]
		[Tooltip("This device powers itself and doesn't need an APC")]
		private bool isSelfPowered = false;

		public bool IsSelfPowered => isSelfPowered;

		[SerializeField]
		[Tooltip("Watts consumed per update when running at 240v")]
		private float wattusage = 0.01f;

		public float Wattusage {
			get => wattusage;
			set {
				wattusage = value;
				resistance = 240 / (value / 240);
			}
		}

		[SerializeField]
		[FormerlySerializedAs("Resistance")]
		private float resistance = 99999999;

		public float Resistance {
			get => resistance;
			set => resistance = value;
		}

		[HideInInspector] public APC RelatedAPC;
		public IAPCPowerable Powered;
		public bool AdvancedControlToScript;

		public bool StateUpdateOnClient = true;

		[SyncVar(hook = nameof(UpdateSynchronisedState))]
		[FormerlySerializedAs("State")]
		private PowerState state = PowerState.Off;
		public PowerState State => state;

		/// <summary>
		/// 1 PowerState is the old state, 2 PowerState is the new state
		/// </summary>
		public UnityEvent<Tuple<PowerState, PowerState>> OnStateChangeEvent = new UnityEvent<Tuple<PowerState, PowerState>>();

		[SyncVar(hook = nameof(UpdateSynchronisedVoltage))]
		private float recordedVoltage = 0;

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.APC;
		public MultitoolConnectionType ConType => conType;

		private Texture disconnectedImg;

		#region Lifecycle

		private void Awake()
		{
#if Unity_Editor
		disconnectedImg = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Textures/EditorAssets/disconnected.png");
#endif

			EnsureInit();
		}

		private void Start()
		{
			if (Application.isPlaying == false) return;

			if (Wattusage > 0)
			{
				resistance = 240 / (Wattusage / 240);
			}
		}

		private void EnsureInit()
		{
			if (Powered != null) return;
			Powered = GetComponent<IAPCPowerable>();
			if (isSelfPowered)
			{
				if (AdvancedControlToScript)
				{
					recordedVoltage = expectedRunningVoltage;
					Powered?.PowerNetworkUpdate(expectedRunningVoltage);
				}
				else
				{
					Powered?.StateUpdate(PowerState.On);
				}
			}
		}

		public override void OnStartClient()
		{
			EnsureInit();
			if (AdvancedControlToScript)
			{
				UpdateSynchronisedVoltage(recordedVoltage, recordedVoltage);
			}
			else
			{
				UpdateSynchronisedState(state, state);
			}
		}

		public override void OnStartServer()
		{
			EnsureInit();
			if (AdvancedControlToScript)
			{
				UpdateSynchronisedVoltage(recordedVoltage, recordedVoltage);
			}
			else
			{
				UpdateSynchronisedState(state, state);
			}
		}

		#endregion

		public void SetMaster(ISetMultitoolMaster imaster)
		{
			var inApc = (imaster as Component)?.gameObject.GetComponent<APC>();
			if (RelatedAPC != null)
			{
				RemoveFromAPC();
			}
			RelatedAPC = inApc;
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

		public void PowerNetworkUpdate(float voltage) // Could be optimised to not update when voltage is same as previous voltage
		{
			if (AdvancedControlToScript)
			{
				recordedVoltage = voltage;
				Powered?.PowerNetworkUpdate(voltage);
			}
			else
			{
				var newState = PowerState.On;
				if (voltage <= 1)
				{
					newState = PowerState.Off;
				}
				else if (voltage > maximumWorkingVoltage)
				{
					newState = PowerState.OverVoltage;
				}
				else if (voltage < minimumWorkingVoltage)
				{
					newState = PowerState.LowVoltage;
				}

				if (newState == state) return;

				OnStateChangeEvent.Invoke(new Tuple<PowerState, PowerState>(state, newState));

				state = newState;
				Powered?.StateUpdate(state);
			}
		}

		private void UpdateSynchronisedVoltage(float oldVoltage, float newVoltage)
		{
			EnsureInit();
			if (oldVoltage != newVoltage)
			{
				Logger.LogTraceFormat("{0}({1}) state changing {2} to {3}", Category.Electrical, name, transform.position.To2Int(), oldVoltage, newVoltage);
			}

			recordedVoltage = newVoltage;
			if (isSelfPowered)
			{
				recordedVoltage = expectedRunningVoltage;
			}

			if (Powered != null && StateUpdateOnClient && AdvancedControlToScript)
			{
				if (isSelfPowered)
				{
					Powered?.PowerNetworkUpdate(expectedRunningVoltage);
				}
				else
				{
					Powered?.PowerNetworkUpdate(newVoltage);
				}
			}
		}

		private void UpdateSynchronisedState(PowerState oldState, PowerState newState)
		{
			EnsureInit();
			if (newState != state)
			{
				Logger.LogTraceFormat("{0}({1}) state changing {2} to {3}", Category.Electrical, name, transform.position.To2Int(), this.state, newState);
			}

			state = newState;

			if (isSelfPowered)
			{
				state = PowerState.On;
			}

			if (Powered != null && StateUpdateOnClient)
			{
				if (isSelfPowered)
				{
					Powered?.StateUpdate(PowerState.On);
				}
				else
				{
					Powered?.StateUpdate(state);
				}
			}
		}

		private void OnDrawGizmosSelected()
		{
			if (RelatedAPC == null || isSelfPowered)
			{
				return;
			}

			//Highlighting APC
			Gizmos.color = new Color(0.5f, 0.5f, 1, 1);
			Gizmos.DrawLine(RelatedAPC.transform.position, gameObject.transform.position);
			Gizmos.DrawSphere(RelatedAPC.transform.position, 0.15f);
		}

		private void OnDrawGizmos()
		{
			if (RelatedAPC != null || isSelfPowered)
			{
				return;
			}

			Gizmos.DrawIcon(transform.position, "disconnected");
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			RemoveFromAPC();
		}

		public static bool IsOn(PowerState states)
		{
			return (states == PowerState.On || states == PowerState.LowVoltage || states == PowerState.OverVoltage);
		}
	}

	public enum DeviceType
	{
		None,
		Lights,
		Environment,
		Equipment
	}

	public enum PowerState
	{
		Off,
		LowVoltage,
		On,
		OverVoltage,
	}
}
