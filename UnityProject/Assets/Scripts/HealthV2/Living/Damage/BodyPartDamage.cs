using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using Health.Objects;
using Logs;

namespace HealthV2
{
	public partial class BodyPart
	{

		public bool CanNotBeHealedByExternalHealingPack = false;

		/// <summary>
		/// The armor of the clothing covering a part of the body, ignoring selfArmor.
		/// </summary>
		private readonly LinkedList<Armor> clothingArmors = new LinkedList<Armor>();

		public LinkedList<Armor> ClothingArmors => clothingArmors;

		/// <summary>
		/// The armor of the body part itself, ignoring the clothing (for example the xenomorph's exoskeleton).
		/// </summary>
		[HorizontalLine] [Tooltip("The armor of the body part itself, ignoring the clothing.")]
		public Armor SelfArmor = new Armor();

		/// The amount damage taken by body parts contained within this body part is modified by
		/// increases as this body part sustains more damage, to a maximum of 100% damage when the
		/// health of this part is 0.
		[Tooltip("The amount that damage taken by contained body parts is modified by")]
		public Armor SubOrganBodyPartArmour = new Armor();

		/// <summary>
		/// Threshold at which body parts contained within this body part start taking increased damage
		/// </summary>
		[Tooltip("Health threshold at which contained body parts start taking more damage")] [Range(0, 100)]
		public int SubOrganDamageIncreasePoint = 50;

		/// <summary>
		/// Determines whether dealing damage to this body part should impact the overall health of the creature
		/// </summary>
		[Tooltip("Does damaging this body part affect the creature's overall health?")]
		public bool DamageContributesToOverallHealth = true;


		/// <summary>
		/// Modifier that multiplicatively reduces the efficiency of the body part based on damage
		/// </summary>
		[Tooltip("Modifier to reduce efficiency with as damage is taken")]
		[HideInInspector] public Modifier DamageModifier = new Modifier();

		/// <summary>
		/// The body part's maximum health
		/// </summary>
		[Tooltip("This part's maximum health, which it will start at.")] [SerializeField]
		private float maxHealth = 100;

		public float MaxHealth => maxHealth;

		/// <summary>
		/// The body part's current health
		/// </summary>
		private float health;

		public float Health => health;

		/// <summary>
		/// Stores how severely the body part is damage for purposes of examine
		/// </summary>
		[HideInInspector] public DamageSeverity Severity = DamageSeverity.None;

		/// <summary>
		/// Toxin damage taken
		/// </summary>
		public float Toxin => Damages[(int) DamageType.Tox];

		/// <summary>
		/// Brute damage taken
		/// </summary>
		public float Brute => Damages[(int) DamageType.Brute];

		/// <summary>
		/// Burn damage taken
		/// </summary>
		public float Burn => Damages[(int) DamageType.Burn];

		/// <summary>
		/// Cellular (clone) damage taken
		/// </summary>
		public float Cellular => Damages[(int) DamageType.Clone];

		/// <summary>
		/// Damage taken from lack of blood reagent
		/// </summary>
		public float Oxy => Damages[(int) DamageType.Oxy];

		/// <summary>
		/// Stamina damage taken
		/// </summary>
		public float Stamina => Damages[(int) DamageType.Stamina];

		/// <summary>
		/// Amount of radiation sustained. Not actually 'stacks' but rather a float.
		/// </summary>
		public float RadiationStacks => Damages[(int) DamageType.Radiation];
		/// <summary>
		/// List of all damage taken
		/// </summary>
		public readonly float[] Damages =
		{
			0,
			0,
			0,
			0,
			0,
			0,
			0
		};

		public DamageWeaknesses damageWeaknesses { get; } = new DamageWeaknesses();

		/// <summary>
		/// The total damage this body part has taken that is not from lack of blood reagent
		/// and not including cellular (clone) damage
		/// </summary>
		public float TotalDamageWithoutOxy
		{
			get
			{
				float TDamage = 0;
				for (int i = 0; i < Damages.Length; i++)
				{
					if ((int) DamageType.Oxy == i) continue;
					if ((int) DamageType.Radiation == i) continue;
					TDamage += Damages[i];
				}

				return TDamage;
			}
		}

		/// <summary>
		/// The total damage this body part has taken that is not from lack of blood reagent
		/// including cellular (clone) damage
		/// </summary>
		public float TotalDamageWithoutOxyCloneRadStam
		{
			get
			{
				float TDamage = 0;
				for (int i = 0; i < Damages.Length; i++)
				{
					if ((int) DamageType.Oxy == i) continue;
					if ((int) DamageType.Clone == i) continue;
					if ((int) DamageType.Radiation == i) continue;
					if ((int) DamageType.Stamina == i) continue;
					TDamage += Damages[i];
				}

				return TDamage;
			}
		}

		public float TotalDamageWithoutOxyRadStam
		{
			get
			{
				float TDamage = 0;
				for (int i = 0; i < Damages.Length; i++)
				{
					if ((int) DamageType.Oxy == i) continue;
					if ((int) DamageType.Radiation == i) continue;
					if ((int) DamageType.Stamina == i) continue;
					TDamage += Damages[i];
				}

				return TDamage;
			}
		}

		/// <summary>
		/// The total damage this body part has taken
		/// </summary>
		public float TotalDamage
		{
			get
			{
				float TDamage = 0;
				for (int i = 0; i < Damages.Length; i++)
				{
					if ((int) DamageType.Radiation == i) continue;
					TDamage += Damages[i];
				}

				return TDamage;
			}
		}

		/// <summary>
		/// Triggers when a body part receives damage.
		/// It has the attack type, damage type and the amount of damage as parameters for the callback
		/// </summary>
		public event Action<BodyPartDamageData> OnDamageTaken;

		public BodyPartDamageData LastDamageData { get; private set; } = new BodyPartDamageData();


		public const float DMG_Environment_Multiplier = 1.5f;

		/// <summary>
		/// Adjusts the appropriate damage type by the given damage amount and updates body part
		/// functionality based on its new health total
		/// </summary>
		/// <param name="damage">Damage amount</param>
		/// <param name="damageType">The type of damage</param>
		private void AffectDamage(float damage, int damageType)
		{
			if (damage == 0) return;

			if (float.IsNormal(damage) == false)
			{
				Loggy.LogError("oh no/..!!!! NAN /Abnormal number as damage > " + damage );
				return;
			}
			float toDamage = Damages[damageType] + damage;

			if (toDamage < 0) toDamage = 0;
			if (damageType != (int) DamageType.Radiation)
			{
				if (toDamage > (maxHealth * 3))
				{
					toDamage = maxHealth * 3;
				}
			}


			Damages[damageType] = toDamage;
			health = maxHealth - TotalDamage;
			RecalculateEffectiveness();
			UpdateSeverity();
		}


		public TemperatureAlert ExposeTemperature(float environmentalTemperature, float divideBy)
		{
			bool alertTypeHigherTemperature = false;
			if (SelfArmor.TemperatureOutsideSafeRange(environmentalTemperature))
			{
				float min = SelfArmor.TemperatureProtectionInK.x;
				float max = SelfArmor.TemperatureProtectionInK.y;

				foreach (var armour in ClothingArmors)
				{
					if (armour.InvalidValuesInTemperature() == false)
					{
						min = Mathf.Min(min, armour.TemperatureProtectionInK.x);
						max = Mathf.Max(max, armour.TemperatureProtectionInK.y);
					}
				}

				if (environmentalTemperature < min)
				{
					//so, Half Temperature of the minimum threshold that's when the maximum damage will kick in
					TakeDamage(null,   (DMG_Environment_Multiplier*Mathf.Clamp((min-environmentalTemperature)/(min/2f), 0f,0.30f))/divideBy, AttackType.Internal, DamageType.Burn, true);
					return TemperatureAlert.TooCold;
				}
				else if (environmentalTemperature > max)
				{

					var mid = SelfArmor.GetMiddleTemperature();
					var hotRange =  (max - mid ); //To get how much hot protection it has

					//so, Double of the maximum temperature that's when the maximum damage Will start kicking
					TakeDamage(null, DMG_Environment_Multiplier*Mathf.Clamp((environmentalTemperature-max)/hotRange, 0f,0.30f), AttackType.Internal, DamageType.Burn, true);
					return TemperatureAlert.TooHot;
				}
			}

			if (SelfArmor.TemperatureNearingLimits(environmentalTemperature, out alertTypeHigherTemperature))
			{
				bool NotNearingLimit = true;
				foreach (var armour in ClothingArmors)
				{
					if (armour.InvalidValuesInTemperature() == false)
					{
						NotNearingLimit = armour.TemperatureNearingLimits(environmentalTemperature, out alertTypeHigherTemperature);
						if (NotNearingLimit == false)
						{
							return TemperatureAlert.None;
						}
					}
				}


				if (alertTypeHigherTemperature)
				{
					return TemperatureAlert.Hot;
				}
				else
				{
					return TemperatureAlert.Cold;
				}
			}
			return TemperatureAlert.None;
		}

		public PressureAlert ExposePressure(float environmentalPressure, float divideBy)
		{
			bool alertTypeHigherPressure = false;

			if (SelfArmor.PressureOutsideSafeRange(environmentalPressure))
			{

				float min = SelfArmor.PressureProtectionInKpa.x;
				float max = SelfArmor.PressureProtectionInKpa.y;

				foreach (var armour in ClothingArmors)
				{
					if (armour.InvalidValuesInPressure() == false)
					{
						min = Mathf.Min(min, armour.PressureProtectionInKpa.x);
						max = Mathf.Max(max, armour.PressureProtectionInKpa.y);
					}
				}

				if (environmentalPressure < min)
				{
					//so, Half Pressure of the minimum threshold that's when the maximum damage will kick in
					TakeDamage(null,   (DMG_Environment_Multiplier*Mathf.Clamp((min - environmentalPressure)/(min/2f), 0f,0.30f))/divideBy, AttackType.Internal, DamageType.Brute, true);
					return PressureAlert.PressureTooLow;

				}
				else if (environmentalPressure > max)
				{
					//Alert UI here

					var mid = SelfArmor.GetMiddlePressure();
					var PressureRange =  (max - mid ); //To get how much PressureRange protection it has

					//so, Double of the maximum Pressure that's when the maximum damage Will start kicking
					TakeDamage(null, DMG_Environment_Multiplier*Mathf.Clamp((environmentalPressure-max)/PressureRange, 0f,0.30f), AttackType.Internal, DamageType.Brute, true);
					return PressureAlert.PressureTooHigher;
				}
			}

			if (SelfArmor.PressureNearingLimits(environmentalPressure, out alertTypeHigherPressure))
			{
				bool NotNearingLimit = true;
				foreach (var armour in ClothingArmors)
				{
					if (armour.InvalidValuesInPressure() == false)
					{
						NotNearingLimit = armour.PressureNearingLimits(environmentalPressure, out alertTypeHigherPressure);
						if (NotNearingLimit == false)
						{
							return PressureAlert.None;
						}
					}
				}

				if (alertTypeHigherPressure)
				{
					return PressureAlert.PressureHigher;
				}
				else
				{
					return PressureAlert.PressureLow;
				}
			}
			return PressureAlert.None;
		}


		/// <summary>
		/// Applies damage to this body part. Damage will be divided among it and sub organs depending on their
		/// armor values.
		/// </summary>
		/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
		/// <param name="damage">Damage amount</param>
		/// <param name="attackType">Type of attack that is causing the damage</param>
		/// <param name="damageType">The type of damage</param>
		/// <param name="organDamageSplit">Should the damage be divided amongst the contained organs or applied to a random one</param>
		public void TakeDamage(GameObject damagedBy, float damage, AttackType attackType, DamageType damageType,
								bool organDamageSplit = false, bool DamageSubOrgans = true, float armorPenetration = 0,
								double traumaDamageChance = 100, TraumaticDamageTypes tramuticDamageType = TraumaticDamageTypes.NONE, bool invokeOnDamageEvent = true)
		{
			if (damage == 0) return;
			LastDamageData = new BodyPartDamageData()
			{
				DamageAmount = damage,
				DamagedBy = damagedBy,
				AttackType = attackType,
				DamageType = damageType,
				OrganDamageSplit = organDamageSplit,
				DamageSubOrgans = DamageSubOrgans,
				ArmorPenetration = armorPenetration,
				TramuticDamageType = tramuticDamageType,
				TraumaDamageChance = traumaDamageChance,
				InvokeOnDamageEvent = invokeOnDamageEvent
			};
			float damageToLimb = Armor.GetTotalDamage(
				SelfArmor.GetDamage(damage, attackType, armorPenetration),
				attackType,
				ClothingArmors,
				armorPenetration
			);
			if (damageToLimb > 0)
			{
				damageToLimb = damageWeaknesses.CalculateAppliedDamage(damageToLimb, damageType);
			}

			AffectDamage(damageToLimb, (int) damageType);
			if (invokeOnDamageEvent) OnDamageTaken?.Invoke(LastDamageData);

			// May be changed to individual damage
			// May also want it so it can miss sub organs
			if (DamageSubOrgans && containBodyParts.Count > 0)
			{
				DamageOrgans(damage, attackType, damageType, organDamageSplit, armorPenetration);
			}
		}

		private void DamageOrgans(float damage, AttackType attackType, DamageType damageType, bool organDamageSplit, float armorPenetration)
		{
			var organDamageRatingValue = SubOrganBodyPartArmour.GetRatingValue(attackType, armorPenetration);
			if (maxHealth - Damages[(int) damageType] < SubOrganDamageIncreasePoint)
			{
				organDamageRatingValue += 1 - ((maxHealth - Damages[(int) damageType]) / SubOrganDamageIncreasePoint);
				organDamageRatingValue = Math.Min(1, organDamageRatingValue);
			}

			var subDamage = damage * organDamageRatingValue;

			//TODO: remove BodyPart component from organ
			if (organDamageSplit)
			{
				foreach (var organ in containBodyParts)
				{
					organ.TakeDamage(null, subDamage / containBodyParts.Count , attackType , damageType);
				}
			}
			else
			{
				var organBodyPart = containBodyParts.PickRandom(); //It's not like you can aim for Someone's liver can you
				organBodyPart.TakeDamage(null, subDamage, attackType , damageType);
			}
		}

		/// <summary>
		/// Heals damage taken by this body part
		/// </summary>
		public void HealDamage(GameObject healingItem, float healAmt,
			DamageType damageTypeToHeal)
		{
			TakeDamage(healingItem, -healAmt, AttackType.Internal, damageTypeToHeal, DamageSubOrgans  : false);
		}

		/// <summary>
		/// Heals damage taken by this body part
		/// </summary>
		public void HealDamage(GameObject healingItem, float healAmt,
			int damageTypeToHeal)
		{
			HealDamage(healingItem, healAmt, (DamageType) damageTypeToHeal);
		}


		/// <summary>
		/// Resets all damage sustained by this body part
		/// </summary>
		public void ResetDamage()
		{
			for (int i = 0; i < Damages.Length; i++)
			{
				Damages[i] = 0;
			}

			health = maxHealth - TotalDamage;
			RecalculateEffectiveness();
			UpdateSeverity();
		}

		/// <summary>
		/// Recalculates the effectiveness of the organ based off of damage
		/// </summary>
		//Probably custom curves would be good here
		public void RecalculateEffectiveness()
		{
			DamageModifier.Multiplier = Mathf.Max(health, 0) / maxHealth;
		}

		/// <summary>
		/// Consumes radiation stacks and processes it into toxin damage
		/// </summary>
		public void CalculateRadiationDamage()
		{
			if (RadiationStacks == 0) return;
			var ProcessingRadiation = RadiationStacks * 0.01f;
			if (ProcessingRadiation > 2 && ProcessingRadiation < 0.05f)
			{
				ProcessingRadiation = 2;
			}

			HealDamage(null,ProcessingRadiation, DamageType.Radiation);
			TakeDamage(null, ProcessingRadiation * 0.1f,AttackType.Internal , DamageType.Tox, DamageSubOrgans : false); //This Should bypass all armour
		}

		/// <summary>
		/// Updates the reported severity of injuries for examine based off of current health
		/// </summary>
		private void UpdateSeverity()
		{

			var oldSeverity = Severity;
			// update UI limbs depending on their severity of damage
			float severity = 1 - (Mathf.Max(maxHealth - TotalDamageWithoutOxyCloneRadStam, 0) / maxHealth);
			// If the limb is uninjured
			if (severity <= 0)
			{
				Severity = DamageSeverity.None;
			}
			// If the limb is under 10% damage
			else if (severity < 0.1)
			{
				Severity = DamageSeverity.Light;
			}
			// If the limb is under 25% damage
			else if (severity < 0.25)
			{
				Severity = DamageSeverity.LightModerate;
			}
			// If the limb is under 45% damage
			else if (severity < 0.45)
			{
				Severity = DamageSeverity.Moderate;
			}
			// If the limb is under 85% damage
			else if (severity < 0.85)
			{
				Severity = DamageSeverity.Bad;
			}
			// If the limb is under 95% damage
			else if (severity < 0.95f)
			{
				Severity = DamageSeverity.Critical;
			}
			// If the limb is 95% damage or over
			else if (severity >= 0.95f)
			{
				Severity = DamageSeverity.Max;
			}

			if (DamageContributesToOverallHealth && oldSeverity != Severity && HealthMaster != null)
			{
				UpdateIcons();
			}
		}

		/// <summary>
		/// Updates the player health UI if present
		/// </summary>
		private void UpdateIcons()
		{
			UIManager.PlayerHealthUI.SetBodyTypeOverlay(this);
		}

		public float GetDamage(DamageType damageType)
		{
			return Damages[(int) damageType];
		}

		public void SetMaxHealth(float newMaxHealth)
		{
			maxHealth = newMaxHealth;
		}
	}
}

public class BodyPartDamageData
{
	public GameObject DamagedBy = null;
	public float DamageAmount = 0f;
	public AttackType AttackType = AttackType.Melee;
	public DamageType DamageType = DamageType.Brute;
	public bool OrganDamageSplit = false;
	public bool DamageSubOrgans = true;
	public float ArmorPenetration = 0;
	public double TraumaDamageChance = 100;
	public TraumaticDamageTypes TramuticDamageType = TraumaticDamageTypes.NONE;
	public bool InvokeOnDamageEvent = true;
}
