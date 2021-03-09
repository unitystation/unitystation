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

		public ReagentMix UseBloodPool;
		public ReagentMix ReadyBloodPool;

		public Chemistry.Reagent BloodReagent => bloodType.Blood;

		[SerializeField]
		[Required("Need to know our limits for how much blood we have and what not.")]
		private CirculatoryInfo bloodInfo = null;
		public CirculatoryInfo BloodInfo => bloodInfo;

		private LivingHealthMasterBase healthMaster;

		public void SetBloodType(BloodType inBloodType)
		{
			bloodType = inBloodType;
			ReadyBloodPool.Add(BloodReagent,5);
			ReadyBloodPool.Add(bloodType.CirculatedReagent,2.5f);
		}

		private void Awake()
		{
			healthMaster = GetComponent<LivingHealthMasterBase>();
			ReadyBloodPool.Add(BloodReagent,5);
			// ReadyBloodPool.Add(bloodType.CirculatedReagent,2.5f);
			// bloodReagentAmount = BloodInfo.BLOOD_REAGENT_DEFAULT;
			// bloodAmount = BloodInfo.BLOOD_DEFAULT;
		}

		public virtual void AddUsefulBloodReagent(ReagentMix amount) //Return excess
		{
			ReadyBloodPool.Add(amount);
			//Add mechanism for recovering blood naturally
			//ReadyBloodPool = Mathf.Min(bloodInfo.BLOOD_REAGENT_MAX, bloodReagentAmount);
			//return 0;
		}

		public void ConvertUsedBlood(float Toadd)
		{
			//TODOH technically violates the laws of thermodynamics but meh
			var addAmount = healthMaster.CirculatorySystem.UseBloodPool.Take(Toadd);
			AddUsefulBloodReagent(addAmount);
		}

	}

}
