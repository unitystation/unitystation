using System;
using System.Collections.Generic;
using Logs;
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

		[SyncVar(hook = nameof(SyncFireStacks))]
		private float fireStacks;
		public float FireStacks => fireStacks;

		[SyncVar]
		private bool isSuffocating;
		public bool IsSuffocating => isSuffocating;

		[SyncVar] private TemperatureAlert temperature = TemperatureAlert.None;
		public TemperatureAlert Temperature => temperature;

		[SyncVar]
		private PressureAlert pressure = PressureAlert.None;
		public PressureAlert Pressure => pressure;

		private HealthDollStorage CurrentHealthDollStorage = new HealthDollStorage();

		[SyncVar(hook = nameof(SyncHealthDoll))]
		private string healthDollData;
		public event Action<ConsciousState> ConsciousEvent;
		public event Action<float> OverallHealthEvent;

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
			DNABloodTypeJSONSync = JsonConvert.SerializeObject(newDNA);
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
			DNABloodType = JsonConvert.DeserializeObject<DNAandBloodType>(newDNABloodTypeJSON);
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
				Loggy.LogError(e.ToString()); //some weird ass serialisation error
				return;
			}

			for (int i = 0; i < CurrentHealthDollStorage.DollStates.Count; i++)
			{
				UIManager.PlayerHealthUI.bodyPartListeners[i].SetDamageColor(CurrentHealthDollStorage.DollStates[i].damageColor.UncompresseToColour());
				UIManager.PlayerHealthUI.bodyPartListeners[i].SetBodyPartColor(CurrentHealthDollStorage.DollStates[i].bodyPartColor.UncompresseToColour());
			}
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