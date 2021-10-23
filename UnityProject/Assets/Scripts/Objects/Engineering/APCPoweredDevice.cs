using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Mirror;
using Core.Editor.Attributes;
using Systems.ObjectConnection;
using Objects.Engineering;
#if Unity_Editor
using UnityEditor;
#endif


namespace Systems.Electricity
{
	[ExecuteInEditMode]
	public class APCPoweredDevice : NetworkBehaviour, IServerDespawn, IMultitoolSlaveable
	{
		[SerializeField, PrefabModeOnly]
		[FormerlySerializedAs("MinimumWorkingVoltage")]
		private float minimumWorkingVoltage = 190;

		[SerializeField, PrefabModeOnly]
		[FormerlySerializedAs("ExpectedRunningVoltage")]
		private float expectedRunningVoltage = 240;

		[SerializeField, PrefabModeOnly]
		[FormerlySerializedAs("MaximumWorkingVoltage")]
		private float maximumWorkingVoltage = 300;

		[SerializeField, PrefabModeOnly]
		[Tooltip("Category of this powered device. " +
				"Different categories work like a set of breakers, so you can turn off lights and keep machines working.")]
		private DeviceType deviceType = DeviceType.None;

		[SerializeField]
		[Tooltip("This device powers itself and doesn't need an APC")]
		private bool isSelfPowered = false;

		public bool IsSelfPowered => isSelfPowered;

		[SerializeField, PrefabModeOnly]
		[Tooltip("Watts consumed per update when running at 240v")]
		private float wattusage = 0.01f;

		public float Wattusage {
			get => wattusage;
			set {
				wattusage = value;
				resistance = 240 / (value / 240);
			}
		}

		[SerializeField, PrefabModeOnly]
		[FormerlySerializedAs("Resistance")]
		[FormerlySerializedAs("resistance")]
		private float InitialResistance = 99999999;


		private float resistance = 99999999;


		public float Resistance {
			get => resistance;
			set => resistance = value;
		}

		[HideInInspector] public APC RelatedAPC;
		private IAPCPowerable Powered;

		[PrefabModeOnly]
		public bool AdvancedControlToScript;

		[PrefabModeOnly]
		public bool StateUpdateOnClient = true;

		[SyncVar(hook = nameof(UpdateSynchronisedState))]
		[FormerlySerializedAs("State")]
		private PowerState state = PowerState.Off;
		public PowerState State => state;

		/// <summary>
		/// 1 PowerState is the old state, 2 PowerState is the new state
		/// </summary>
		[NonSerialized]
		public UnityEvent<Tuple<PowerState, PowerState>> OnStateChangeEvent = new UnityEvent<Tuple<PowerState, PowerState>>();

		[SyncVar(hook = nameof(UpdateSynchronisedVoltage))]
		private float recordedVoltage = 0;

		public float Voltage => RelatedAPC == null ? 0 : RelatedAPC.Voltage;

		private Texture disconnectedImg;
		private RegisterTile registerTile;

		private bool blockApcChange;

		#region Lifecycle

		private void Awake()
		{
#if Unity_Editor
		disconnectedImg = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Textures/EditorAssets/disconnected.png");

#endif
			if (Application.isPlaying == false) return;
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
			if (this == null) return;
			if (Powered != null) return;
			resistance = InitialResistance;
			Powered = GetComponent<IAPCPowerable>();
			registerTile = GetComponent<RegisterTile>();
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
				OnStateChangeEvent.Invoke(new Tuple<PowerState, PowerState>(PowerState.Off, state));
			}
		}

		#endregion

		#region Multitool Interaction

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.APC;
		IMultitoolMasterable IMultitoolSlaveable.Master => RelatedAPC;
		bool IMultitoolSlaveable.RequireLink => isSelfPowered == false;

		bool IMultitoolSlaveable.TrySetMaster(PositionalHandApply interaction, IMultitoolMasterable master)
		{
			if (blockApcChange)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer,
						$"You try to set the {gameObject.ExpensiveName()}'s APC connection but it seems to be locked!");
				return false;
			}

			SetMaster(master);
			return true;
		}

		void IMultitoolSlaveable.SetMasterEditor(IMultitoolMasterable master)
		{
			SetMaster(master);
		}

		private void SetMaster(IMultitoolMasterable master)
		{
			if (RelatedAPC != null)
			{
				RemoveFromAPC();
				RelatedAPC = null;
			}

			if (master is APC apc)
			{
				RelatedAPC = apc;
				RelatedAPC.AddDevice(this);
			}
		}

		#endregion

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

		public void LockApcLinking(bool newState)
		{
			blockApcChange = newState;
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			RemoveFromAPC();
		}

		public static bool IsOn(PowerState states)
		{
			return (states == PowerState.On || states == PowerState.LowVoltage || states == PowerState.OverVoltage);
		}

		public bool ConnectToClosestApc()
		{
			var apcs = Physics2D.OverlapCircleAll(registerTile.WorldPositionServer.To2Int(), 30);

			apcs = apcs.Where(a => a.gameObject.GetComponent<APC>() != null).ToArray();

			if (apcs.Length == 0)
			{
				return false;
			}

			APC bestTarget = null;
			float closestDistance = Mathf.Infinity;
			var devicePosition = gameObject.transform.position;

			foreach (var potentialTarget in apcs)
			{
				var directionToTarget = potentialTarget.gameObject.transform.position - devicePosition;
				float dSqrToTarget = directionToTarget.sqrMagnitude;

				if (dSqrToTarget >= closestDistance) continue;
				closestDistance = dSqrToTarget;
				bestTarget = potentialTarget.gameObject.GetComponent<APC>();
			}

			if (bestTarget == null || bestTarget == RelatedAPC) return false;

			//If connected to apc before remove us
			if(RelatedAPC != null)
			{
				RelatedAPC.RemoveDevice(this);
			}

			RelatedAPC = bestTarget;

			bestTarget.AddDevice(this);

			return true;
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
