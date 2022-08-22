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
		private float bleedStacks;
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

		[SyncVar]
		private float temperature = 295.15f;
		public float Temperature => temperature;

		[SyncVar]
		private float pressure = 101;
		public float Pressure => pressure;

		private HealthDollStorage CurrentHealthDollStorage = new HealthDollStorage();

		[SyncVar(hook = nameof(SyncHealthDoll))]
		private string healthDollData;

		[SyncVar]
		private HungerState hungerState;
		public HungerState HungerState => hungerState;

		[SyncVar]
		private BleedingState bleedingState;
		public BleedingState BleedingState => bleedingState;


		private bool DollDataChanged = false;

		#endregion

		#region LifeCycle

		private void Awake()
		{
			CurrentHealthDollStorage.DollStates = new List<HealthDollStorage.HealthDollState>();
			livingHealthMasterBase = GetComponent<LivingHealthMasterBase>();
			overallHealthSync = livingHealthMasterBase.MaxHealth;
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

		//Holds all methods which the server will use to change a health value, will then sync change to client

		[Server]
		public void SetHunger(HungerState newHungerState)
		{
			hungerState = newHungerState;
		}

		[Server]
		public void SetBleedingState(BleedingState newBleedingState)
		{
			bleedingState = newBleedingState;
		}

		[Server]
		public void SetOverallHealth(float newHealth)
		{
			overallHealthSync = newHealth;
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
		}

		[Server]
		public void SetToxins(bool newState)
		{
			hasToxins = newState;
		}

		[Server]
		public void SetTemperature(float newTemperature)
		{
			temperature = newTemperature;
		}

		[Server]
		public void SetPressure(float newPressure)
		{
			pressure = newPressure;
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
			livingHealthMasterBase.OnClientFireStacksChange.Invoke(newStacks);
		}

		[Client]
		private void SyncHealthDoll(string oldDollData, string newDollData)
		{
			healthDollData = newDollData;
			if (isServer) return;
			CurrentHealthDollStorage = JsonConvert.DeserializeObject<HealthDollStorage>(healthDollData);
			for (int i = 0; i < CurrentHealthDollStorage.DollStates.Count; i++)
			{
				UIManager.PlayerHealthUI.bodyPartListeners[i].SetDamageColor(CurrentHealthDollStorage.DollStates[i].damageColor.UncompresseToColour());
				UIManager.PlayerHealthUI.bodyPartListeners[i].SetBodyPartColor(CurrentHealthDollStorage.DollStates[i].bodyPartColor.UncompresseToColour());
			}
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