using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Mirror;
using Core.Editor.Attributes;
using Logs;
using Shared.Systems.ObjectConnection;
using Systems.Explosions;
using ScriptableObjects;
using Objects.Engineering;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Systems.Electricity
{
	[ExecuteInEditMode]
	public class APCPoweredDevice : NetworkBehaviour, IServerDespawn, IEmpAble, IMultitoolSlaveable
	{
		[SerializeField ]
		[FormerlySerializedAs("MinimumWorkingVoltage")]
		private float minimumWorkingVoltage = 190;

		[SerializeField ]
		[FormerlySerializedAs("ExpectedRunningVoltage")]
		private float expectedRunningVoltage = 240;

		[SerializeField ]
		[FormerlySerializedAs("MaximumWorkingVoltage")]
		private float maximumWorkingVoltage = 300;

		[SerializeField ]
		[Tooltip("Category of this powered device. " +
				"Different categories work like a set of breakers, so you can turn off lights and keep machines working.")]
		private DeviceType deviceType = DeviceType.None;

		[SerializeField]
		[Tooltip("This device powers itself and doesn't need an APC")]
		private bool isSelfPowered = false;

		public bool IsSelfPowered => isSelfPowered;

		[SerializeField ]
		[Tooltip("Watts consumed per update when running at 240v")]
		private float wattusage = 0.01f;

		public float Wattusage
		{
			get => wattusage;
			set
			{
				wattusage = value;
				Resistance = 240 / (value / 240);
			}
		}

		[SerializeField ]
		[FormerlySerializedAs("Resistance")]
		[FormerlySerializedAs("resistance")]
		private float InitialResistance = 99999999;

		[NonSerialized]
		public float Resistance = 99999999;

		[HideInInspector] public APC RelatedAPC;
		private IAPCPowerable Powered;


		public bool AdvancedControlToScript;


		public bool StateUpdateOnClient = true;

		[SyncVar(hook = nameof(UpdateSynchronisedState))]
		[FormerlySerializedAs("State")]
		private PowerState state = PowerState.Off;
		public PowerState State => state;

		/// <summary>
		/// 1 PowerState is the old state, 2 PowerState is the new state
		/// </summary>
		public event Action<PowerState, PowerState> OnStateChangeEvent;

		[SyncVar(hook = nameof(UpdateSynchronisedVoltage))]
		private float recordedVoltage = 0;

		public float Voltage => RelatedAPC == null ? 0 : RelatedAPC.Voltage;

		private Texture disconnectedImg;
		private RegisterTile registerTile;

		private bool blockApcChange;

		private bool isEMPed = false;

		public UnityEvent OnDeviceLinked = new UnityEvent();
		public UnityEvent OnDeviceUnLinked = new UnityEvent();

		#region Lifecycle

		private void Awake()
		{
#if UNITY_EDITOR
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
				Resistance = 240 / (Wattusage / 240);
			}
		}

		private void EnsureInit()
		{
			if (this == null) return;
			if (Powered != null) return;
			Resistance = InitialResistance;
			Powered = GetComponent<IAPCPowerable>();
			registerTile = GetComponent<RegisterTile>();
			if (isSelfPowered)
			{
				SelfPoweredUpdate();
			}
		}

		private void OnDestroy()
		{
			OnDeviceLinked?.RemoveAllListeners();
			OnDeviceUnLinked?.RemoveAllListeners();
		}

		private void SelfPoweredUpdate()
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
				OnStateChangeEvent?.Invoke(PowerState.Off, state);
			}
		}

		#endregion

		#region Multitool Interaction

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.APC;
		IMultitoolMasterable IMultitoolSlaveable.Master => RelatedAPC;
		bool IMultitoolSlaveable.RequireLink => isSelfPowered == false;

		bool IMultitoolSlaveable.TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			if (blockApcChange)
			{
				Chat.AddExamineMsgFromServer(performer,
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

				OnStateChangeEvent?.Invoke(state, newState);

				state = newState;
				Powered?.StateUpdate(state);
			}
		}

		private void UpdateSynchronisedVoltage(float oldVoltage, float newVoltage)
		{
			EnsureInit();
			if (oldVoltage != newVoltage)
			{
				Loggy.LogTraceFormat("{0}({1}) state changing {2} to {3}", Category.Electrical, name, transform.position.RoundTo2Int(), oldVoltage, newVoltage);
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
					Powered.PowerNetworkUpdate(expectedRunningVoltage);
				}
				else
				{
					Powered.PowerNetworkUpdate(newVoltage);
				}
			}
		}

		public void UpdateSynchronisedState(PowerState oldState, PowerState newState)
		{
			EnsureInit();
			if (!isEMPed)
			{
				if (newState != state)
				{
					Loggy.LogTraceFormat("{0}({1}) state changing {2} to {3}", Category.Electrical, name, transform.position.RoundTo2Int(), this.state, newState);
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
			else
			{
				state = PowerState.Off;
			}
		}

		public void OnEmp(int EmpStrength)
		{
			StartCoroutine(Emp(EmpStrength));
		}

		private IEnumerator Emp(int EmpStrength)
		{
			int effectTime = (int)(EmpStrength * 0.5f);

			if (DMMath.Prob(75))
			{
				_ = Spawn.ServerPrefab(CommonPrefabs.Instance.SparkEffect, registerTile.WorldPositionServer).GameObject;
			}

			isEMPed = true;
			UpdateSynchronisedState(State, PowerState.Off);
			yield return WaitFor.Seconds(effectTime);
			isEMPed = false;
			UpdateSynchronisedState(State, PowerState.On);
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

		public void ChangeToSelfPowered()
		{
			isSelfPowered = true;
			SelfPoweredUpdate();
			UpdateSynchronisedState(state, DMMath.Prob(5) ? PowerState.OverVoltage : PowerState.On);
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
