using Items;
using Systems.Explosions;
using UnityEngine;

namespace Systems.Construction.Parts
{
	public class Battery : MonoBehaviour, IEmpAble, IExaminable, IChargeable
	{
		[SerializeField]
		private int Watts = 9000;

		public int watts
		{
			get => watts;
			set
			{
				if (SelfCharging)
				{
					if (MaxWatts >= value)
					{
						UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, SelfCharge);
					}
					else
					{
						UpdateManager.Add(SelfCharge, 1);
					}
				}

				Watts = value;
			}
		}

		public int MaxWatts = 9000;

		public int InternalResistance = 240;

		public bool isBroken = false;

		public bool SelfCharging = false;

		public int SelfChargeWatts = 0;

		public bool IsFullyCharged =>  Watts >= MaxWatts;

		public void SelfCharge()
		{
			if (SelfCharging)
			{
				watts += SelfChargeWatts;
			}
		}

		public void ChargeBy(float watts)
		{
			if(this.Watts + watts > MaxWatts)
			{
				this.Watts = MaxWatts;
				return;
			}
			this.Watts += (int)watts;
			return;
		}

		public void OnEmp(int EmpStrength)
		{
			Watts -= EmpStrength * 100;
			Mathf.Clamp(Watts, 0, MaxWatts);

			if(EmpStrength > 50 && DMMath.Prob(25))
			{
				isBroken = true;
			}
		}

		public string Examine(Vector3 worldPos = default)
		{
			string status = "";
			if (isBroken)
			{
				status = $"<color=red>It appears to be broken.";
			}
			return $"{gameObject.GetComponent<ItemAttributesV2>().InitialDescription}. Charge indicator shows a {Watts/MaxWatts*100} percent charge." +
			       status;
		}
	}
}
