using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chemistry;
using NaughtyAttributes;

namespace HealthV2
{
	[RequireComponent(typeof(LivingHealthMasterBase))]
	public class CirculatorySystemBase : MonoBehaviour
	{
		[SerializeField]
		[Required("Must have a blood type in a circulatory system.")]
		private BloodType bloodType = null;
		public BloodType BloodType => bloodType;

		//How much of our blood reagent we have.
		//In a human, this would be oxygen.
		private float bloodReagentAmount;
		public float BloodReagentAmount => bloodReagentAmount;

		private float bloodAmount = 0;
		public float BloodAmount => bloodAmount;

		//The actual reagent our blood uses.
		public Chemistry.Reagent BloodReagent => bloodType.CirculatedReagent;

		//This is the list of the toxins in the circulatory system.
		//Different toxins can have different effects on different parts of the body.
		//For instance, a blood poison doesn't effect robotics, but a nano injection might.
		//TODO: Implement this. This is here so I don't forget atm.
		private List<ToxinBase> bloodToxins = new List<ToxinBase>();
		public List<ToxinBase> BloodToxins => bloodToxins;

		[SerializeField]
		[Required("Need to know our limits for how much blood we have and what not.")]
		private CirculatoryInfo bloodInfo = null;
		public CirculatoryInfo BloodInfo => bloodInfo;

		//For every point of strength that the heart has, this is how much blood flow the body will receive.
		//In the human body, it takes about 45 seconds for blood to fully circulate through your body.
		//This means that at 0.95f blood flow, the entirety of a humans blood (about 5L) would be cycled in  ~50 heart beats.
		private float heartStrengthRatio = 0.95f;

		//Now, this is useful, but it isn't actually set here. This will need to be modified by an organ.
		//So I need to remember that! It may be zero the frame the player is spawned in.
		//Probably update this each tick. Set to zero if there is no update from organs.
		private float heartRate = 0;

		//This bool is to let the heart organs know that they need to increase their heartrate.
		private bool heartRateNeedsIncreasing = false;

		public bool HeartRateNeedsIncreasing => heartRateNeedsIncreasing;

		public float HeartRate
		{
			get => heartRate;
			set => heartRate = value;
		}

		public bool HeartIsStopped => heartRate <= 0;

		private LivingHealthMasterBase healthMaster;

		private void Awake()
		{
			healthMaster = GetComponent<LivingHealthMasterBase>();
			bloodReagentAmount = BloodInfo.BLOOD_REAGENT_DEFAULT;
			bloodAmount = BloodInfo.BLOOD_DEFAULT;
		}

		//This circulates blood around the body.
		//This isn't actually called by the circulatory system, it is going to be activated by an organ pumping blood.
		public virtual void HeartBeat(float strength)
		{
			float initialPumpAmount = (bloodReagentAmount / bloodAmount) * (heartStrengthRatio * strength);
			float pumpedReagent = initialPumpAmount;
			foreach (ImplantBase implant in healthMaster.ImplantList)
			{
				pumpedReagent = Math.Max(implant.BloodPumpedEvent(bloodType.CirculatedReagent, pumpedReagent), 0);
			}

			if (pumpedReagent <= 0)
			{
				heartRateNeedsIncreasing = true;
			}

			float usedReagent = initialPumpAmount - pumpedReagent;
			bloodReagentAmount -= usedReagent + bloodInfo.BLOOD_REAGENT_CONSUME_PER_BEAT;
			bloodReagentAmount = Mathf.Max(bloodReagentAmount, 0);

		}

	}

}
