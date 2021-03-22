﻿using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using UnityEngine;

public class Heart : BodyPart
{
	//The actual heartrate of this implant, in BPM.
	/// <summary>
	/// Heart Rate in BPM (beats per minute).
	/// </summary>
	private float heartRate = 0;

	[SerializeField]
	[Tooltip("How quickly the heart can change its heartrate per second. Human maximum is around 10.")]
	private float heartRateDelta = 10;

	[SerializeField]
	[Tooltip("The maximum heartrate of the implant. Human maximum is about 200.")]
	private float maxHeartRate = 200;

	[Tooltip("Arbitrary rating of 'strength'. Used to determine how much bloodflow the heart can provide." +
			 "100 is a human heart." +
			 "The higher this is, the more total blood can be pushed around per heartbeat.")]
	[SerializeField] private float heartStrength = 100f;

	private float lastHeartBeat = 0f;
	private float nextHeartBeat = 0f;

	private float heartBeatDelay;

	public bool HeartAttack = false;

	System.Random RNGesus = new System.Random();


	public bool CanTriggerHeartAttack = true;

	public int SecondsOfRevivePulse = 30;

	public int CurrentPulse = 0;

	public override void ImplantPeriodicUpdate()
	{
		base.ImplantPeriodicUpdate();
		if (healthMaster.OverallHealth < -100)
		{

			if (CanTriggerHeartAttack)
			{
				DoHeartAttack();
				CanTriggerHeartAttack = false;
				CurrentPulse = 0;
				return;
			}

			if (HeartAttack == false)
			{
				CurrentPulse++;
				if (SecondsOfRevivePulse < CurrentPulse)
				{
					DoHeartAttack();
				}
			}
		}
		else if (healthMaster.OverallHealth < -50)
		{
			CanTriggerHeartAttack = true;
			CurrentPulse = 0;
		}

		DoHeartBeat(healthMaster);
	}

	public void DoHeartBeat(LivingHealthMasterBase healthMaster)
	{
		//If we actually have a circulatory system.
		if (HeartAttack)
		{
			if (SecondsOfRevivePulse < CurrentPulse) return;
			var RNGInt = RNGesus.Next(0, 10000);
			if (RNGInt > 9990)
			{
				HeartAttack = false;
			}

			return;
		}

		Heartbeat(TotalModified);
	}


	public void Heartbeat(float efficiency)
	{
		CirculatorySystemBase circulatorySystem = HealthMaster.CirculatorySystem;
		if (circulatorySystem)
		{
			//circulatorySystem.HeartBeat(heartStrength * TotalModified);
			//TODOH Balance all this stuff, Currently takes an eternity to suffocate
			// Logger.Log("heart Available  " + ReadyBloodPool);
			// Logger.Log("heart pumpedReagent " + pumpedReagent);

			float totalWantedBlood = 0;
			foreach (BodyPart implant in HealthMaster.ImplantList)
			{
				if (implant.IsBloodCirculated == false) continue;
				totalWantedBlood += implant.BloodThroughput * efficiency;
				// Due to how blood is implemented as a single pool with its solutes, we need to compensate for
				// consumed solutes.  This may change in the future if blood changes
				totalWantedBlood += implant.BloodContainer.MaxCapacity - implant.BloodContainer.ReagentMixTotal;
			}
			float toPump = Mathf.Min(totalWantedBlood, heartStrength * efficiency);
			var bloodToGive = circulatorySystem.ReadyBloodPool.Take(Mathf.Min(toPump, circulatorySystem.ReadyBloodPool.Total));
			if (bloodToGive.Total < toPump)
			{
				// Try to maintain blood levels in organs by taking the remainder from used
				circulatorySystem.UsedBloodPool.TransferTo(bloodToGive, Mathf.Min(toPump - bloodToGive.Total, circulatorySystem.UsedBloodPool.Total));
			}

			ReagentMix SpareBlood = new ReagentMix();
			foreach (BodyPart implant in HealthMaster.ImplantList)
			{
				if (implant.IsBloodCirculated == false) continue;
				ReagentMix transfer = bloodToGive.Take((implant.BloodThroughput * efficiency + implant.BloodContainer.MaxCapacity 
					- implant.BloodContainer.ReagentMixTotal) / totalWantedBlood * bloodToGive.Total);
				transfer.Add(SpareBlood);
				SpareBlood.Clear();
				SpareBlood.Add(implant.BloodPumpedEvent(transfer, efficiency));
			}
			circulatorySystem.ReadyBloodPool.Add(SpareBlood);
			circulatorySystem.ReadyBloodPool.Add(bloodToGive);
		}
	}

	public void DoHeartAttack()
	{
		HeartAttack = true;
	}
}