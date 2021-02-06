using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public partial class BodyPart
	{
		//How much damage gets transferred to This part
		public Armor BodyPartArmour = new Armor();

		//How much damage gets transferred to sub organs
		public Armor SubOrganBodyPartArmour = new Armor();


		//The point at which  Specified health of Category is below that Armour becomes less effective for organs
		[Range(0, 100)] public int SubOrganDamageIncreasePoint = 50;


		public bool DamageContributesToOverallHealth = true;


		public float DamageEfficiencyMultiplier = 1;

		private float health = 100;


		public Modifier DamageModifier = new Modifier();

		[SerializeField]
		[Tooltip("The maxmimum health of the implant." +
		         "Implants will start with this amount of health.")]
		private float maxHealth = 100; //Is used for organ functionIt


		//Used for Calculating Overall damage
		public float TotalDamageWithoutOxy
		{
			get
			{
				float TDamage = 0;
				for (int i = 0; i < Damages.Length; i++)
				{
					if ((int)DamageType.Oxy == i) continue;
					TDamage += Damages[i];
				}

				return TDamage;
			}
		}


		//
		public float TotalDamageWithoutOxyClone
		{
			get
			{
				float TDamage = 0;
				for (int i = 0; i < Damages.Length; i++)
				{
					if ((int)DamageType.Oxy == i) continue;
					if ((int)DamageType.Clone == i) continue;
					TDamage += Damages[i];
				}

				return TDamage;
			}
		}

		//Used for Calculating body part damage, Such as how effective an Organ is
		public float TotalDamage
		{
			get
			{
				float TDamage = 0;
				foreach (var Damage in Damages)
				{ TDamage += Damage; }

				return TDamage;
			}
		}


		public float Toxin => Damages[(int)DamageType.Tox];
		public float Brute => Damages[(int)DamageType.Brute];
		public float Burn => Damages[(int)DamageType.Burn];
		public float Cellular => Damages[(int)DamageType.Clone];
		public float Oxy => Damages[(int)DamageType.Oxy];
		public float Stamina => Damages[(int)DamageType.Stamina];

		public readonly float[] Damages = {
			0,
			0,
			0,
			0,
			0,
			0,
		};

		public void AffectDamage(float HealthDamage, int healthDamageType)
		{
			float Damage = Damages[healthDamageType] + HealthDamage;

			if (Damage < 0) Damage = 0;

			Damages[healthDamageType] = Damage;
			health = maxHealth - TotalDamage;
			RecalculateEffectiveness();
		}


		public void TakeDamage(GameObject damagedBy, float damage,
			AttackType attackType, DamageType damageType)
		{

			var damageToLimb = BodyPartArmour.GetDamage(damage, attackType);
			AffectDamage(damageToLimb,(int) damageType);

			//TotalDamage// Could do without oxygen maybe
			//May be changed to individual damage
			if (containBodyParts.Count > 0)
			{
				var organDamageRatingValue = SubOrganBodyPartArmour.GetRatingValue(attackType);
				if (maxHealth-Damages[(int)damageType] < SubOrganDamageIncreasePoint)
				{
					organDamageRatingValue += (1 - ((maxHealth - Damages[(int)damageType]) / SubOrganDamageIncreasePoint));
					organDamageRatingValue = Math.Min(1, organDamageRatingValue);
				}

				var OrganDamage = damage * organDamageRatingValue;
				var OrganToDamage = containBodyParts.PickRandom(); //It's not like you can aim for Someone's  liver can you
				OrganToDamage.TakeDamage(damagedBy,OrganDamage,attackType,damageType);
			}

		}

		public void DamageInitialisation()
		{
			this.AddModifier(DamageModifier);
		}

		public void HealDamage(GameObject healingItem, float healAmt,
			int damageTypeToHeal)
		{
			AffectDamage(-healAmt, damageTypeToHeal);
		}

		//Probably custom curves would be good here
		public void RecalculateEffectiveness()
		{
			DamageModifier.Multiplier = (Mathf.Max(health, 0)  / maxHealth);
		}
	}
}