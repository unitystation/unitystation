using System.Collections.Generic;
using System.Linq;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems
{
	public class BatterySystem : HealthSystemBase
	{
		public List<BatteryPack> BatteryPacks = new List<BatteryPack>();

		public List<BatteryPoweredComponent> Consuming = new List<BatteryPoweredComponent>();

		public float ConsumingWatts = 0;


		public float PreviousStoredWatts;

		public BatteryState CashedBatteryState = BatteryState.FullyCharged;

		private BodyAlertManager BodyAlertManager;

		public override void InIt()
		{
			base.InIt();
			BodyAlertManager = Base.GetComponent<BodyAlertManager>();
		}


		public enum BatteryState
		{
			Missing,
			NoCharge,
			LowCharge,
			MediumCard,
			MostlyCharged,
			FullyCharged,
			Charging
		}

		public float ChargePercentage()
		{
			float max = 0;
			float charge = 0;

			foreach (var BatteryPack in BatteryPacks)
			{
				foreach (var Cell in BatteryPack.Cells)
				{
					charge += Cell.Watts;
					max += Cell.MaxWatts;
				}
			}

			if (max == 0)
			{
				return 0;
			}
			return charge / max;

		}

		public BatteryState GetBatteryStateWithChargePercentage(float chargePercentage, float charge )
		{
			if (BatteryPacks.Count == 0 || BatteryPacks.Sum(x => x.Cells.Count) == 0)
			{
				return BatteryState.Missing;
			}

			if (charge > PreviousStoredWatts)
			{
				return BatteryState.Charging;
			}

			if (chargePercentage == 0)
			{
				return BatteryState.NoCharge;
			}
			else if (chargePercentage <  0.25f)
			{
				return BatteryState.LowCharge;
			}
			else if (chargePercentage < 0.5f)
			{
				return BatteryState.MediumCard;
			}
			else if (chargePercentage < 0.75f)
			{
				return BatteryState.MostlyCharged;
			}
			else
			{
				return BatteryState.FullyCharged;
			}
		}

		public AlertSO GetAlertSOFromBatteryState(BatteryState batteryState)
		{
			switch (batteryState)
			{
				case BatteryState.Missing:
					return CommonAlertSOs.Instance.CellMissing;
				case BatteryState.NoCharge:
					return CommonAlertSOs.Instance.CellDeadcharge;
				case BatteryState.LowCharge:
					return CommonAlertSOs.Instance.CellLowcharge;
				case BatteryState.MediumCard:
					return CommonAlertSOs.Instance.CellMidcharge;
				case BatteryState.MostlyCharged:
					return CommonAlertSOs.Instance.CellMostlycharge;
				case BatteryState.FullyCharged:
					return null;
				case BatteryState.Charging:
					return CommonAlertSOs.Instance.CellCharging;
				default:
					return null;
			}


		}

		public override void BodyPartAdded(BodyPart bodyPart)
		{
			var newSaturation =  bodyPart.GetComponent<BatteryPoweredComponent>();
			if (newSaturation != null)
			{
				if (Consuming.Contains(newSaturation) == false)
				{
					Consuming.Add(newSaturation);
					BodyPartListChange();
				}
			}

			var Battery =  bodyPart.GetComponent<BatteryPack>();
			if (Battery != null)
			{
				if (BatteryPacks.Contains(Battery) == false)
				{
					BatteryPacks.Add(Battery);
				}
			}
		}

		public override void BodyPartRemoved(BodyPart bodyPart)
		{
			var newSaturation =  bodyPart.GetComponent<BatteryPoweredComponent>();
			if (newSaturation != null)
			{
				if (Consuming.Contains(newSaturation))
				{
					Consuming.Remove(newSaturation);
				}

				BodyPartListChange();
			}

			var Battery =  bodyPart.GetComponent<BatteryPack>();
			if (Battery == null) return;
			if (BatteryPacks.Contains(Battery) == false)
			{
				BatteryPacks.Remove(Battery);
			}
		}

		public void BodyPartListChange()
		{
			ConsumingWatts = 0;
			foreach (var Consume in Consuming)
			{
				ConsumingWatts += Consume.WattConsumption;
			}
		}

		public override void SystemUpdate()
		{
			// Calculate the sum of all the numbers in the list
			float sum = 0;

			foreach (var batteryPack in BatteryPacks)
			{
				foreach (var battery in batteryPack.Cells)
				{
					sum += battery.Watts;
				}
			}


			if (sum - ConsumingWatts > 0)
			{
				// Subtract x from the sum and calculate the adjustment factor
				float adjustmentFactor = (sum - ConsumingWatts) / sum;

				// Adjust each number in the list by the factor
				foreach (var batteryPack in BatteryPacks)
				{
					foreach (var Cell in batteryPack.Cells)
					{
						Cell.Watts = Mathf.RoundToInt((Cell.Watts * adjustmentFactor)) ;
					}
				}

				foreach (var consumer in Consuming) //TODO is Bit slow
				{
					if (consumer.PowerModifier.Multiplier != 1)
					{
						consumer.PowerModifier.Multiplier = 1f;
					}
				}
			}
			else
			{
				if (sum > 0)
				{
					foreach (var batteryPack in BatteryPacks)
					{
						foreach (var cell in batteryPack.Cells)
						{
							cell.Watts = 0;
						}
					}
				}

				foreach (var consumer in Consuming) //TODO is Bit slow
				{
					if (consumer.PowerModifier.Multiplier != 0)
					{
						//consumer.PowerModifier.Multiplier = 0.0f; //TODO!!!!
					}
				}
			}

			var Percentage =ChargePercentage();
			var State =  GetBatteryStateWithChargePercentage(Percentage, sum);

			if (State != CashedBatteryState)
			{
				var old = GetAlertSOFromBatteryState(CashedBatteryState);
				if (old != null)
				{
					BodyAlertManager.UnRegisterAlert(old);
				}

				CashedBatteryState = State;

				var newOne = GetAlertSOFromBatteryState(State);
				if (newOne != null)
				{
					BodyAlertManager.RegisterAlert(newOne);
				}

			}

			PreviousStoredWatts = sum;
			CashedBatteryState = State;


		}

		public override HealthSystemBase CloneThisSystem()
		{
			return new BatterySystem();
		}
	}
}

