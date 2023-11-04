using System;
using System.Collections.Generic;
using Chemistry;
using Items.Implants.Organs;
using Logs;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace HealthV2.Living.PolymorphicSystems
{
	[System.Serializable]
	public class ReagentPoolSystem : HealthSystemBase
	{
		public ReagentMix BloodPool = new ReagentMix();
		public int StartingBlood = 500;

		public int NormalBlood = 500;

		public BloodType bloodType => bloodReagent as BloodType;

		[SerializeField]
		[Required("Must have a blood type in a circulatory system."), FormerlySerializedAs("bloodType")]
		public Reagent bloodReagent = null;

		[HideInInspector] public List<Heart> PumpingDevices = new List<Heart>();




		///<summary>
		/// Adds a volume of blood along with the maximum normal reagents
		///</summary>
		public void AddFreshBlood(ReagentMix bloodPool, float amount)
		{
			// Currently only does blood and required reagents, should at nutriments and other common gases
			if (bloodPool == null || bloodReagent == null)
			{
				Loggy.LogError("[ReagentPoolSystem/AddFreshBlood] - Missing data detected. Make sure you're not spawning a bodyPart without its proper systems defined.");
			}
			try
			{
				var bloodToAdd = new ReagentMix(bloodReagent, amount);
				if (bloodType != null)
				{
					bloodToAdd.Add(bloodType.CirculatedReagent, bloodType.GetSpareGasCapacity(bloodToAdd));
				}

				bloodPool?.Add(bloodToAdd);
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
			}
		}
		public override void StartFresh()
		{
			AddFreshBlood(BloodPool, StartingBlood);
		}



		public void Bleed(float amount, bool spawnReagentOnFloor = true)
		{
			var bloodLoss = new ReagentMix();
			BloodPool.TransferTo(bloodLoss, amount);
			if (spawnReagentOnFloor) MatrixManager.ReagentReact(bloodLoss, Base.gameObject.RegisterTile().WorldPositionServer);
		}

		/// <summary>
		/// Returns the total amount of blood in the body of the type of blood the body should have
		/// </summary>
		public float GetTotalBlood()
		{
			return GetSpareBlood();
		}

		public void RefreshPumps(List<BodyPart> currentBodyParts)
		{
			PumpingDevices.Clear();

			foreach (var x in currentBodyParts)
			{
				if (x.TryGetComponent<Heart>(out var hrt))
				{
					PumpingDevices.Add(hrt);
				}
			}
		}

		/// <summary>
		/// Updates blood pool with giving body start blood and removing previous
		/// </summary>
		public void UpdateBloodPool(bool needToTransferFood, HungerSystem hungerSystem = null)
		{
			//saving food
			SerializableDictionary<Reagent, float> blood = new(BloodPool.reagents);

			BloodPool.RemoveVolume(BloodPool.Total);
			AddFreshBlood(BloodPool, StartingBlood);

			if (hungerSystem == null || needToTransferFood == false)
				return;

			var foodComps = hungerSystem.NutrimentToConsume;

			foreach (var x in foodComps)
			{
				if (blood.ContainsKey(x.Key))
				{
					BloodPool.Add(x.Key, blood[x.Key]);
				}
				else
				{
					BloodPool.Add(x.Key, 100);
				}
			}
		}

		/// <summary>
		/// Returns the total amount of 'spare' blood outside of the organs
		/// </summary>
		public float GetSpareBlood()
		{
			return BloodPool.Total;
		}

		public override HealthSystemBase CloneThisSystem()
		{
			return new ReagentPoolSystem()
			{
				BloodPool = this.BloodPool.Clone(),
				StartingBlood = this.StartingBlood,
				bloodReagent = this.bloodReagent,
			};
		}
	}
}
