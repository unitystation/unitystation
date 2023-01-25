using System;
using System.Collections.Generic;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace HealthV2
{
	/// <summary>
	/// Holds all sync vars which are only sent to this controlling player
	/// Also holds client RPCs in order to get specific values for a non-controlling player
	/// </summary>
	[DisallowMultipleComponent]
	public class HealthStateController : NetworkBehaviour
	{
		public LivingHealthMasterBase livingHealthMasterBase;

		#region SyncVars

		[SyncVar]
		private float overallHealthSync = 100;
		public float OverallHealth => overallHealthSync;

		[SyncVar]
		private float maxHealthSync = 100;
		public float MaxHealth => maxHealthSync;

		[SyncVar(hook = nameof(SyncDNABloodTypeJSON))]
		private string DNABloodTypeJSONSync;

		public string DnaBloodTypeJsonSync => DNABloodTypeJSONSync;
		public DNAandBloodType DNABloodType { get; private set; }

		[SyncVar]
		private ConsciousState consciousState = ConsciousState.CONSCIOUS;

		public ConsciousState ConsciousState => consciousState;

		[SyncVar]
		private HealthBloodMessage bloodHealth;
		private HealthBloodMessage BloodHealth => bloodHealth;

		[SyncVar]
		private float bleedStacks; //TODO Change to per body part instead
		public float BleedStacks => bleedStacks;

		[SyncVar(hook = nameof(SyncFireStacks))]
		private float fireStacks;
		public float FireStacks => fireStacks;

		[SyncVar]
		private bool isSuffocating;
		public bool IsSuffocating => isSuffocating;

		[SyncVar]
		private bool hasToxins;
		public bool HasToxins => hasToxins;

		[SyncVar] private TemperatureAlert temperature = TemperatureAlert.None;
		public TemperatureAlert Temperature => temperature;

		[SyncVar]
		private PressureAlert pressure = PressureAlert.None;
		public PressureAlert Pressure => pressure;

		private HealthDollStorage CurrentHealthDollStorage = new HealthDollStorage();

		[SyncVar(hook = nameof(SyncHealthDoll))]
		private string healthDollData;

		[SyncVar]
		private HungerState hungerState;
		public HungerState HungerState => hungerState;

		[SyncVar]
		private BleedingState bleedingState;
		public BleedingState BleedingState => bleedingState;

		public event Action<HungerState> HungerEvent;
		public event Action<BleedingState> BleedingEvent;
		public event Action<ConsciousState> ConsciousEvent;
		public event Action<bool> SuffuicationEvent;
		public event Action<bool> ToxinEvent;
		public event Action<float> OverallHealthEvent;
		public event Action<float> FireStacksEvent;
		public event Action<PressureAlert> PressureEvent;
		public event Action<TemperatureAlert> TemperatureEvent;



		private bool DollDataChanged = false;

		#endregion

		#region LifeCycle

		private void Awake()
		{
			CurrentHealthDollStorage.DollStates = new List<HealthDollStorage.HealthDollState>();
			livingHealthMasterBase = GetComponent<LivingHealthMasterBase>();
			overallHealthSync = livingHealthMasterBase.MaxHealth;

			var Player = gameObject.GetComponent<PlayerScript>();
			if (Player != null)
			{
				Player.OnActionControlPlayer += UpdateSyncVar;
			}

		}

		#endregion

		private void LateUpdate()
		{
			if (DollDataChanged)
			{
				healthDollData = JsonConvert.SerializeObject(CurrentHealthDollStorage);
				DollDataChanged = false;
			}
		}

		#region ServerSetValue


		private void UpdateSyncVar()
		{
			SyncFireStacks(fireStacks, fireStacks);
			SyncHealthDoll(healthDollData, healthDollData);
		}
		//Holds all methods which the server will use to change a health value, will then sync change to client

		[Server]
		public void SetHunger(HungerState newHungerState)
		{
			hungerState = newHungerState;
			if (connectionToClient == null) return;
			InvokeClientHungerEvent(hungerState);
		}

		[Server]
		public void SetBleedingState(BleedingState newBleedingState)
		{
			bleedingState = newBleedingState;
			if (connectionToClient == null) return;
			InvokeClientBleedEvent(newBleedingState);
		}

		[Server]
		public void SetOverallHealth(float newHealth)
		{
			overallHealthSync = newHealth;
			if (connectionToClient == null) return;
			InvokeClientOverallHealthEvent(newHealth);
		}

		[Server]
		public void SetMaxHealth(float newMaxHealth)
		{
			maxHealthSync = newMaxHealth;
		}

		[Server]
		public void SetDNA(DNAandBloodType newDNA)
		{
			DNABloodTypeJSONSync = JsonUtility.ToJson(newDNA);
			DNABloodType = newDNA;
		}

		[Server]
		public void SetConsciousState(ConsciousState newConsciousState)
		{
			consciousState = newConsciousState;
			if (connectionToClient != null)
			{
				InvokeClientConsciousStateEvent(newConsciousState);
			}

		}

		[Server]
		public void SetBloodHealth(HealthBloodMessage newBloodHealth)
		{
			bloodHealth = newBloodHealth;
		}

		[Server]
		public void SetFireStacks(float newValue)
		{
			fireStacks = Math.Max(0, newValue);
			if (connectionToClient == null) return;
			InvokeClientFireStackEvent(newValue);
		}

		[Server]
		public void SetBleedStacks(float newValue)
		{
			bleedStacks = Math.Max(0, newValue);
		}

		[Server]
		public void SetSuffocating(bool newSuffocating)
		{
			isSuffocating = newSuffocating;
			if (connectionToClient == null) return;
			InvokeClientSufficationEvent(newSuffocating);
		}

		[Server]
		public void SetToxins(bool newState)
		{
			hasToxins = newState;
			if (connectionToClient == null) return;
			InvokeClientToxinsEvent(newState);
		}

		[Server]
		public void SetTemperature(TemperatureAlert newTemperature)
		{
			temperature = newTemperature;
			if (connectionToClient == null) return;
			InvokeClientTemperatureEvent(newTemperature);
		}

		[Server]
		public void SetPressure(PressureAlert newPressure)
		{
			pressure = newPressure;
			if (connectionToClient == null) return;
			IvokeClientPressureEvent(newPressure);
		}

		[Server]
		public void ServerUpdateDoll(int inLocation, Color INdamageColor, Color INbodyPartColor)
		{
			DollDataChanged = true;
			if (inLocation >= CurrentHealthDollStorage.DollStates.Count)
			{
				while (inLocation >= CurrentHealthDollStorage.DollStates.Count)
				{
					CurrentHealthDollStorage.DollStates.Add(new HealthDollStorage.HealthDollState());
				}
			}

			var DollState = CurrentHealthDollStorage.DollStates[inLocation];
			DollState.damageColor = INdamageColor.ToStringCompressed();
			DollState.bodyPartColor = INbodyPartColor.ToStringCompressed();
			CurrentHealthDollStorage.DollStates[inLocation] = DollState;
		}

		#endregion

		#region ClientSyncMethods

		//Called when client receives new data from sync vars

		[Client]
		private void SyncDNABloodTypeJSON(string oldDNABloodTypeJSON, string newDNABloodTypeJSON)
		{
			DNABloodTypeJSONSync = newDNABloodTypeJSON;
			DNABloodType = JsonUtility.FromJson<DNAandBloodType>(newDNABloodTypeJSON);
		}

		[Client]
		private void SyncFireStacks(float oldStacks, float newStacks)
		{
			fireStacks = newStacks;
			livingHealthMasterBase.OnClientFireStacksChange?.Invoke(newStacks);
		}

		[Client]
		private void SyncHealthDoll(string oldDollData, string newDollData)
		{
			healthDollData = newDollData;
			if (isServer) return;
			if (hasAuthority == false) return;
			try
			{
				CurrentHealthDollStorage = JsonConvert.DeserializeObject<HealthDollStorage>(healthDollData);
			}
			catch (Exception e)
			{
				Logger.LogError(e.ToString()); //some weird ass serialisation error
				return;
			}

			for (int i = 0; i < CurrentHealthDollStorage.DollStates.Count; i++)
			{
				UIManager.PlayerHealthUI.bodyPartListeners[i].SetDamageColor(CurrentHealthDollStorage.DollStates[i].damageColor.UncompresseToColour());
				UIManager.PlayerHealthUI.bodyPartListeners[i].SetBodyPartColor(CurrentHealthDollStorage.DollStates[i].bodyPartColor.UncompresseToColour());
			}
		}

		[TargetRpc]
		private void InvokeClientHungerEvent(HungerState state)
		{
			HungerEvent?.Invoke(state);
		}

		[TargetRpc]
		private void InvokeClientBleedEvent(BleedingState state)
		{
			BleedingEvent?.Invoke(state);
		}

		[TargetRpc]
		private void InvokeClientFireStackEvent(float state)
		{
			FireStacksEvent?.Invoke(state);
		}

		[TargetRpc]
		private void InvokeClientOverallHealthEvent(float state)
		{
			OverallHealthEvent?.Invoke(state);
		}

		[TargetRpc]
		private void InvokeClientConsciousStateEvent(ConsciousState state)
		{
			ConsciousEvent?.Invoke(state);
		}


		[TargetRpc]
		private void InvokeClientSufficationEvent(bool state)
		{
			SuffuicationEvent?.Invoke(state);
		}

		[TargetRpc]
		private void InvokeClientToxinsEvent(bool state)
		{
			ToxinEvent?.Invoke(state);
		}

		[TargetRpc]
		private void InvokeClientTemperatureEvent(TemperatureAlert state)
		{
			TemperatureEvent?.Invoke(state);
		}

		[TargetRpc]
		private void IvokeClientPressureEvent(PressureAlert state)
		{
			PressureEvent?.Invoke(state);
		}

		#endregion

		public struct HealthBloodMessage
		{
			public int HeartRate;
			public float BloodLevel;
			public float OxygenDamage;
			public float ToxinLevel;
		}


		public struct HealthDollStorage
		{
			public List<HealthDollState> DollStates;

			public struct HealthDollState
			{
				public string damageColor;
				public string bodyPartColor;
			}
		}
	}
}