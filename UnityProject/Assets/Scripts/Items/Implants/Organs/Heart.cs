using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using UnityEngine;

public class Heart : Organ
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

	private bool alarmedForInternalBleeding = false;

	public override void ImplantPeriodicUpdate()
	{
		base.ImplantPeriodicUpdate();
		if (RelatedPart.HealthMaster.OverallHealth < -100)
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
		else if (RelatedPart.HealthMaster.OverallHealth < -50)
		{
			CanTriggerHeartAttack = true;
			CurrentPulse = 0;
		}

		DoHeartBeat();
	}

	public override void InternalDamageLogic()
	{
		base.InternalDamageLogic();
		if(RelatedPart.CurrentInternalBleedingDamage > 50 && alarmedForInternalBleeding == false)
		{
			Chat.AddActionMsgToChat(RelatedPart.HealthMaster.gameObject,
			$"You feel a sharp pain in your {RelatedPart.gameObject.ExpensiveName()}!",
			$"{RelatedPart.HealthMaster.playerScript.visibleName} holds their {RelatedPart.gameObject.ExpensiveName()} in pain!");
			alarmedForInternalBleeding = true;
		}
		if(RelatedPart.CurrentInternalBleedingDamage > RelatedPart.MaximumInternalBleedDamage)
		{
			DoHeartAttack();
		}
	}

	public void DoHeartBeat()
	{
		//If we actually have a circulatory system.
		if (HeartAttack)
		{
			if (SecondsOfRevivePulse < CurrentPulse) return;
			var RNGInt = RNGesus.Next(0, 10000);
			if (RNGInt > 9990)
			{
				HeartAttack = false;
				alarmedForInternalBleeding = false;
			}

			return;
		}

		var TotalModified = 1f;
		foreach (var modifier in bodyPart.AppliedModifiers)
		{
			var toMultiply = 1f;
			if (modifier == bodyPart.DamageModifier)
			{
				toMultiply = Mathf.Max(0f,Mathf.Max(bodyPart.MaxHealth - bodyPart.TotalDamageWithoutOxyCloneRadStam, 0) / bodyPart.MaxHealth);
			}
			else if (modifier == bodyPart.HungerModifier)
			{
				continue;
			}
			else
			{
				toMultiply = Mathf.Max(0f, modifier.Multiplier);
			}
			TotalModified *= toMultiply;
		}

		Heartbeat(TotalModified);
	}


	public void Heartbeat(float efficiency)
	{
		CirculatorySystemBase circulatorySystem = RelatedPart.HealthMaster.CirculatorySystem;
		if (circulatorySystem)
		{
			float pumpedReagent = Math.Min(heartStrength * efficiency, circulatorySystem.ReadyBloodPool.Total);
			float totalWantedBlood = 0;
			foreach (BodyPart implant in RelatedPart.HealthMaster.BodyPartList)
			{
				if (implant.IsBloodCirculated == false) continue;
				totalWantedBlood += implant.BloodThroughput;
			}

			pumpedReagent = Math.Min(pumpedReagent, totalWantedBlood);
			ReagentMix SpareBlood = new ReagentMix();

			foreach (BodyPart implant in RelatedPart.HealthMaster.BodyPartList)
			{
				if (implant.IsBloodCirculated == false) continue;
				var BloodToGive = circulatorySystem.ReadyBloodPool.Take((implant.BloodThroughput / totalWantedBlood) * pumpedReagent);
				BloodToGive.Add(SpareBlood);
				SpareBlood.Clear();
				SpareBlood.Add(implant.BloodPumpedEvent(BloodToGive));
			}

			circulatorySystem.ReadyBloodPool.Add(SpareBlood);
		}
	}

	public void DoHeartAttack()
	{
		HeartAttack = true;
	}
}