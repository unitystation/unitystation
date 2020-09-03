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

		//The actual reagent our blood uses.
		public Reagent BloodReagent => bloodType.CirculatedReagent;

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

		//Now, this is useful, but it isn't actually set here. This will need to be modified by an organ.
		//So I need to remember that! It may be zero the frame the player is spawned in.
		//Probably update this each tick. Set to zero if there is no update from organs.
		private float heartRate = 0;
		public float HeartRate => heartRate;

		public bool HeartIsStopped => heartRate <= 0;

		private LivingHealthMasterBase healthMaster;

		private void Awake()
		{
			healthMaster = GetComponent<LivingHealthMasterBase>();
			bloodReagentAmount = BloodInfo.BLOOD_REAGENT_NORMAL;
		}

		//This circulates blood around the body.
		//This isn't actually called by the circulatory system, it is going to be activated by an organ pumping blood.
		public void PumpBlood()
		{
			//TODO: The whole thing. Need to start working on organs now.
		}

	}

}
