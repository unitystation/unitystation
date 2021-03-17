using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chemistry;
using Chemistry.Components;
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

		public ReagentMix UsedBloodPool;
		public ReagentMix ReadyBloodPool;

		public Chemistry.Reagent Blood => bloodType.Blood;
		public Chemistry.Reagent CirculatedReagent => bloodType.CirculatedReagent;

		[SerializeField]
		[Required("Need to know our limits for how much blood we have and what not.")]
		private CirculatoryInfo bloodInfo = null;
		public CirculatoryInfo BloodInfo => bloodInfo;

		private LivingHealthMasterBase healthMaster;

		public void SetBloodType(BloodType inBloodType)
		{
			bloodType = inBloodType;
		}

		private void Awake()
		{
			healthMaster = GetComponent<LivingHealthMasterBase>();
			AddFreshBlood(ReadyBloodPool, 5);
		}

		public void AddFreshBlood(ReagentMix bloodPool, float amount)
		{
			var bloodToAdd = new ReagentMix(Blood, amount);
			bloodToAdd.Add(CirculatedReagent, bloodType.GetCapacity(bloodToAdd));
			bloodPool.Add(bloodToAdd);
		}

		public void FillWithFreshBlood(ReagentContainerBody bloodContainer)
		{
			float bloodToAdd = (bloodContainer.MaxCapacity - bloodContainer.ReagentMixTotal) / bloodType.BloodCapacityOf;
			bloodContainer.CurrentReagentMix.Add(Blood, bloodToAdd);
			bloodContainer.CurrentReagentMix.Add(CirculatedReagent, bloodToAdd * bloodType.BloodCapacityOf);
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
			var addAmount = healthMaster.CirculatorySystem.UsedBloodPool.Take(Toadd);
			AddUsefulBloodReagent(addAmount);
		}

	}

}
