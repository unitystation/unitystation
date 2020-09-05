using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

public class Heart : ImplantBase
{

	//The actual heartrate of this implant, in BPM.
	private float heartRate = 0;

	[SerializeField]
	[Tooltip("How quickly the heart can change its heartrate per second. Human maximum is around 10.")]
	private float heartRateDelta = 10;

	[SerializeField]
	[Tooltip("The maximum heartrate of the implant. Human maximum is about 200.")]
	private float maxHeartRate = 200;

	[SerializeField]
	[Tooltip("Arbitrary rating of 'strengh'. Used to determine how much bloodflow the heart can provide." +
	         "100 is a human heart." +
	         "The higher this is, the more total blood can be pushed around per heartbeat.")]
	private float heartStrength = 100f;

	private float lastHeartBeat = 0f;
	private float nextHeartBeat = 0f;

	private float heartBeatDelay;
	public override void ImplantUpdate(LivingHealthMasterBase healthMaster)
	{
		base.ImplantUpdate(healthMaster);

		if (Time.time > nextHeartBeat)
		{
			DoHeartBeat(healthMaster);
			lastHeartBeat = Time.time;
			nextHeartBeat = lastHeartBeat + heartBeatDelay;
		}

	}

	public void DoHeartBeat(LivingHealthMasterBase healthMaster)
	{
		//If we actually have a circulatory system.
		CirculatorySystemBase circulatorySystem = healthMaster.CirculatorySystem;
		if (circulatorySystem)
		{
			circulatorySystem.HeartBeat(heartStrength);

			UpdateHeartRate(circulatorySystem);
		}
	}


	private void UpdateHeartRate(CirculatorySystemBase circulatorySystem)
	{
		if (circulatorySystem.HeartRateNeedsIncreasing)
		{
			heartRate += Math.Min((Time.time - lastHeartBeat) * heartRateDelta, heartRateDelta);
			heartRate = Math.Min(heartRate, maxHeartRate);
			circulatorySystem.HeartRate = heartRate;
		}
	}


}
