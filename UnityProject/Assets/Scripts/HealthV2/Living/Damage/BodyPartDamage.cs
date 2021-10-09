using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using System.Linq;
using Random = System.Random;

namespace HealthV2
{
	public partial class BodyPart
	{
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
		/// Affects how much damage contributes to the efficiency of the body part, currently unimplemented
		/// </summary>
		[HideInInspector] public float DamageEfficiencyMultiplier = 1;

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
		[HideInInspector] public DamageSeverity Severity = DamageSeverity.LightModerate;

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
		/// Adjusts the appropriate damage type by the given damage amount and updates body part
		/// functionality based on its new health total
		/// </summary>
		/// <param name="damage">Damage amount</param>
		/// <param name="damageType">The type of damage</param>
		public void AffectDamage(float damage, int damageType)
		{
			if (damage == 0) return;
			float toDamage = Damages[damageType] + damage;

			if (toDamage < 0) toDamage = 0;

			Damages[damageType] = toDamage;
			health = maxHealth - TotalDamage;
			RecalculateEffectiveness();
			UpdateSeverity();
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
								double traumaDamageChance = 100, TraumaticDamageTypes tramuticDamageType = TraumaticDamageTypes.NONE)
		{
			float damageToLimb = Armor.GetTotalDamage(
				SelfArmor.GetDamage(damage, attackType, armorPenetration),
				attackType,
				ClothingArmors,
				armorPenetration
			);
			AffectDamage(damageToLimb, (int) damageType);

			// May be changed to individual damage
			// May also want it so it can miss sub organs
			if (DamageSubOrgans && OrganList.Count > 0)
			{
				DamageOrgans(damage, attackType, damageType, organDamageSplit, armorPenetration);
			}

			if(damage < damageThreshold) return; //Do not apply traumas if the damage is not serious.
			if(damageType == DamageType.Brute) //Check damage type to avoid bugs where you can blow someone's head off with a shoe.
			{
				if (attackType == AttackType.Melee || attackType == AttackType.Laser || attackType == AttackType.Energy)
				{
					if (tramuticDamageType != TraumaticDamageTypes.NONE && DMMath.Prob(traumaDamageChance))
					{
						//TODO: move this to an utility, its hard to read! - picks a random enum from the ones already flagged
						Random random = new Random();
						TraumaticDamageTypes[] typeToSelectFrom = Enum.GetValues(typeof(TraumaticDamageTypes)).Cast<TraumaticDamageTypes>().Where(x => tramuticDamageType.HasFlag(x)).ToArray();
						TraumaticDamageTypes selectedType = typeToSelectFrom[random.Next(1, typeToSelectFrom.Length)];
						ApplyTraumaDamage(selectedType);
					}
					CheckBodyPartIntigrity();
				}
			}

			if(attackType == AttackType.Bomb)
			{
				TakeBluntDamage();
				DismemberBodyPartWithChance();
			}

			if (damageType == DamageType.Burn || attackType == AttackType.Fire ||
			    attackType == AttackType.Laser || attackType == AttackType.Energy)
			{
				ApplyTraumaDamage(TraumaticDamageTypes.BURN);
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
				foreach (var organ in OrganList)
				{
					var organBodyPart = organ.GetComponent<BodyPart>();
					organBodyPart.AffectDamage(subDamage / OrganList.Count, (int) damageType);
				}
			}
			else
			{
				var organBodyPart = OrganList.PickRandom().GetComponent<BodyPart>(); //It's not like you can aim for Someone's liver can you
				organBodyPart.AffectDamage(subDamage, (int) damageType);
			}
		}

		/// <summary>
		/// Heals damage taken by this body part
		/// </summary>
		public void HealDamage(GameObject healingItem, float healAmt,
			DamageType damageTypeToHeal)
		{
			AffectDamage(-healAmt, (int) damageTypeToHeal);
		}

		/// <summary>
		/// Heals damage taken by this body part
		/// </summary>
		public void HealDamage(GameObject healingItem, float healAmt,
			int damageTypeToHeal)
		{
			AffectDamage(-healAmt, damageTypeToHeal);
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
			var ProcessingRadiation = RadiationStacks * 0.001f;
			if (ProcessingRadiation < 20 && ProcessingRadiation > 0.5f)
			{
				ProcessingRadiation = 20;
			}

			AffectDamage(-ProcessingRadiation, (int) DamageType.Radiation);
			AffectDamage(ProcessingRadiation * 0.05f, (int) DamageType.Tox);
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
				currentPierceDamageLevel = TraumaDamageLevel.NONE;
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
	}
}
