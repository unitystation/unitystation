using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
		[Tooltip("The armor of the body part itself, ignoring the clothing.")]
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
		public float DamageEfficiencyMultiplier = 1;

		/// <summary>
		/// Modifier that multiplicatively reduces the efficiency of the body part based on damage
		/// </summary>
		[Tooltip("Modifier to reduce efficiency with as damage is taken")]
		public Modifier DamageModifier = new Modifier();

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
		public DamageSeverity Severity = DamageSeverity.LightModerate;

		/// <summary>
		/// How much damage can this body part last before it breaks/gibs/Disembowles?
		/// <summary>
		public float DamageThreshold = 18f;
		public DamageSeverity GibsOnSeverityLevel = DamageSeverity.Max;
		public float GibChance = 0.15f;

		/// <summary>
		/// When does this body part take before it's contents in it's storage spill out?
		/// </summary>
		[SerializeField, 
		Tooltip("When does the contents of this body part's storage to spill out when a large enough cut exists?")]
		private BodyPartCutSize BodyPartStorageContentsSpillOutOnCutSize = BodyPartCutSize.LARGE;

		/// <summary>
		/// When do we start applying Slash logic?
		/// </summary>
		[SerializeField, 
		Tooltip("At what cut size do we start applying slash logic?")]
		private BodyPartCutSize BodyPartSlashLogicOnCutSize = BodyPartCutSize.SMALL;

		/// <summary>
		/// When do we start applying disembowel logic?
		/// </summary>
		[SerializeField, 
		Tooltip("At what cut size do we start applying disembowel logic?")]
		private BodyPartCutSize BodyPartDisembowelLogicOnCutSize = BodyPartCutSize.MEDIUM;

		/// <summary>
		/// How likely does the contents of this body part's storage to spill out?
		/// </summary>
		[SerializeField, 
		Tooltip("How likely does the contents of this body part's storage to spill out when a large enough cut exists?"),
		Range(0,1.0f)]
		private float spillChanceWhenCutPresent = 0.5f;

		/// <summary>
		/// Does this body part have a cut and how big is it?
		/// </summary>
		private BodyPartCutSize currentCutSize = BodyPartCutSize.NONE;

		public bool CanBleedInternally = false;

		public bool CanBleedExternally = false;

		private bool isBleedingInternally = false;

		private bool isBleedingExternally = false;

		public bool IsBleedingInternally => isBleedingInternally;

		public bool IsBleedingExternally => isBleedingExternally;

		public Vector2 MinMaxInternalBleedingValues = new Vector2(5, 20);

		[SerializeField]
		private float maximumInternalBleedDamage = 100;

		public float MaximumInternalBleedDamage => maximumInternalBleedDamage;

		private float currentInternalBleedingDamage = 0;

		[SerializeField]
		private Color bodyPartColorWhenCharred = Color.black;

		private float currentSlashCutDamage = 0;
		private float currentPierceDamage   = 0;
		private float currentBurnDamage     = 0;

		[SerializeField] private float bodyPartAshesAboveThisDamage = 125;
		public float BodyPartAshesAboveThisDamage => bodyPartAshesAboveThisDamage;

		private PierceDamageLevel currentPierceDamageLevel = PierceDamageLevel.NONE;
		private SlashDamageLevel currentSlashDamageLevel = SlashDamageLevel.NONE;
		private BurnDamageLevels currentBurnDamageLevel = BurnDamageLevels.NONE;

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


		[HideInInspector] public float CurrentInternalBleedingDamage 
		{ 
			get 
			{
				return currentInternalBleedingDamage;
			}
			set
			{
				currentInternalBleedingDamage = value;
			}
		}
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

		public enum BodyPartCutSize
		{
			NONE,
			SMALL,
			MEDIUM,
			LARGE
		}

		public enum PierceDamageLevel
		{
			NONE,
			SMALL,
			MEDIUM,
			LARGE
		}

		public enum SlashDamageLevel
		{
			NONE,
			SMALL,
			MEDIUM,
			LARGE
		}

		public enum BurnDamageLevels
		{
			NONE,
			MINOR,
			MAJOR,
			CHARRED
		}

		public enum TramuticDamageTypes 
		{
			SLASH,
			PIERCE,
			BURN
		}

		public void DamageInitialisation()
		{
			this.AddModifier(DamageModifier);
		}

		/// <summary>
		/// Adjusts the appropriate damage type by the given damage amount and updates body part
		/// functionality based on its new health total
		/// </summary>
		/// <param name="damage">Damage amount</param>
		/// <param name="damageType">The type of damage</param>
		public void AffectDamage(float damage, int damageType)
		{
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
		/// <param name="damageSplit">Should the damage be divided amongst the contained body parts or applied to a random body part</param>
		public void TakeDamage(
			GameObject damagedBy,
			float damage,
			AttackType attackType,
			DamageType damageType,
			bool damageSplit = false,
			bool DamageSubOrgans = true,
			float armorPenetration = 0
		)
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
			if (DamageSubOrgans)
			{
				if (ContainBodyParts.Count > 0)
				{
					var organDamageRatingValue = SubOrganBodyPartArmour.GetRatingValue(attackType, armorPenetration);
					if (maxHealth - Damages[(int) damageType] < SubOrganDamageIncreasePoint)
					{
						organDamageRatingValue +=
							1 - ((maxHealth - Damages[(int) damageType]) / SubOrganDamageIncreasePoint);
						organDamageRatingValue = Math.Min(1, organDamageRatingValue);
					}

					var subDamage = damage * organDamageRatingValue;
					if (damageSplit)
					{
						foreach (var bodyPart in ContainBodyParts)
						{
							bodyPart.TakeDamage(damagedBy, subDamage / ContainBodyParts.Count, attackType, damageType,
								damageSplit);
						}
					}
					else
					{
						var OrganToDamage =
							ContainBodyParts.PickRandom(); //It's not like you can aim for Someone's liver can you
						OrganToDamage.TakeDamage(damagedBy, subDamage, attackType, damageType);
					}
				}
			}
			if(damageType == DamageType.Brute) //Check damage type to avoid bugs where you can blow someone's head off with a shoe.
			{
				if (attackType == AttackType.Melee || attackType == AttackType.Laser || attackType == AttackType.Energy)
				{
					CheckBodyPartIntigrity(damage);
				}
			}
			if(attackType == AttackType.Bomb)
			{
				if(damageToLimb >= DamageThreshold)
				{
					DismemberBodyPartWithChance();
				}
			}
			if(attackType == AttackType.Fire)
			{
				TakeBurnDamage(damage);
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
				currentCutSize = BodyPartCutSize.NONE;
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

			if (oldSeverity != Severity && healthMaster != null)
			{
				UpdateIcons();
			}
		}

		/// <summary>
		/// Checks if the bodypart is damaged to a point where it can be gibbed from the body
		/// </summary>
		protected void CheckBodyPartIntigrity(float lastDamage)
		{
			if(currentCutSize >= BodyPartSlashLogicOnCutSize)
			{
				if(containBodyParts.Count != 0)
				{
					Disembowel();
				}
			}
			if(Severity >= GibsOnSeverityLevel && lastDamage >= DamageThreshold)
			{
				DismemberBodyPartWithChance();
			}
		}

		/// <summary>
		/// Applies trauma damage to the body part, checks if it has enough protective armor to cancel the trauma damage
		/// and automatically checks how big is the body part's cut size.
		/// </summary>
		public void ApplyTraumaDamage(float tramuaDamage, TramuticDamageTypes damageType = TramuticDamageTypes.SLASH)
		{
			//We use dismember protection chance because it's the most logical value.
			if(DMMath.Prob(SelfArmor.DismembermentProtectionChance * 100) == false)
			{
				if(damageType == TramuticDamageTypes.SLASH) { currentSlashCutDamage += tramuaDamage; }
				if(damageType == TramuticDamageTypes.PIERCE) { currentPierceDamage += tramuaDamage; }
				CheckCutSize();
			}
			//Burn damage checks for it's own armor damage type.
			if (damageType == TramuticDamageTypes.BURN)
			{
				//Large cuts and parts in terrible condition means less protective flesh against fire.
				if(currentSlashDamageLevel == SlashDamageLevel.LARGE || Severity >= DamageSeverity.Critical)
				{
					TakeBurnDamage(tramuaDamage * 1.25f);
				}
				else
				{
					TakeBurnDamage(tramuaDamage);
				}
			}
		}

		[ContextMenu("Debug - Apply 25 Slash Damage")]
		private void DEBUG_ApplyTestSlash()
		{
			ApplyTraumaDamage(25);
		}

		[ContextMenu("Debug - Apply 25 Pierce Damage")]
		private void DEBUG_ApplyTestPierce()
		{
			ApplyTraumaDamage(25, TramuticDamageTypes.PIERCE);
		}

		/// <summary>
		/// Checks how big is the cut is right now.
		/// </summary>
		private void CheckCutSize()
		{
			if(currentSlashCutDamage <= 0)
			{
				currentCutSize = BodyPartCutSize.NONE;
			}
			else if(currentSlashCutDamage > 25)
			{
				currentCutSize = BodyPartCutSize.SMALL;
			}
			else if(currentSlashCutDamage > 50)
			{
				currentCutSize = BodyPartCutSize.MEDIUM;
			}
			else if(currentSlashCutDamage > 75)
			{
				currentCutSize = BodyPartCutSize.LARGE;
			}

			if(currentCutSize >= BodyPartSlashLogicOnCutSize && CanBleedExternally)
			{
				StartCoroutine(ExternalBleedingLogic());
			}
		}

		/// <summary>
		/// Checks if the cut is big enough for the contained organs to escape.
		/// If the cut isn't big enough or has failed a chance check, apply internal damage + bleeding.
		/// </summary>
		private void Disembowel()
		{
			BodyPart randomBodyPart = ContainBodyParts.GetRandom();
			BodyPart randomCustomBodyPart = OptionalOrgans.GetRandom();
			if(currentCutSize >= BodyPartStorageContentsSpillOutOnCutSize)
			{
				float chance = UnityEngine.Random.Range(0.0f, 1.0f);
				if(chance >= spillChanceWhenCutPresent)
				{
					randomBodyPart.RemoveFromBodyThis();
					if(randomCustomBodyPart != null)
					{
						randomCustomBodyPart.RemoveFromBodyThis();
					}
				}
				else
				{
					randomBodyPart.ApplyInternalDamage();
					if(randomCustomBodyPart != null)
					{
						randomCustomBodyPart.ApplyInternalDamage();
					}
				}
			}
			else
			{
				randomBodyPart.ApplyInternalDamage();
				if(randomCustomBodyPart != null)
				{
					randomCustomBodyPart.ApplyInternalDamage();
				}
			}
		}

		/// <summary>
		/// Enables internal damage logic.
		/// </summary>
		[ContextMenu("Debug - Apply Internal Damage")]
		private void ApplyInternalDamage()
		{
			if(CanBleedInternally)
			{
				isBleedingInternally = true;
			}
		}

		/// <summary>
		/// The logic executed for when a body part is externally bleeding.
		/// checks if bleeding stops on it's own over time.
		/// </summary>
		public IEnumerator ExternalBleedingLogic()
		{
			if(isBleedingExternally)
			{
				yield break;
			}
			bool willCloseOnItsOwn = false;
			isBleedingExternally = true;
			StartCoroutine(Bleedout());
			CheckCutSize();
			if(currentSlashDamageLevel != SlashDamageLevel.LARGE || currentPierceDamageLevel == PierceDamageLevel.SMALL)
			{
				willCloseOnItsOwn = true;
			}
			if(willCloseOnItsOwn)
			{
				yield return WaitFor.Seconds(128);
				CheckCutSize();
				if(currentSlashDamageLevel != SlashDamageLevel.LARGE || currentPierceDamageLevel == PierceDamageLevel.SMALL)
				{
					isBleedingExternally = false;
				}
			}
		}

		/// <summary>
		/// Limb bleed logic, continues on until isBleedingExternally is false.
		/// </summary>
		private IEnumerator Bleedout()
		{
			while(isBleedingExternally)
			{
				yield return WaitFor.Seconds(4f);
				if(healthMaster != null || healthMaster.CirculatorySystem != null) //This is to prevent rare moments where body parts still attempt to bleed when they no longer should.
				{
					healthMaster.CirculatorySystem.Bleed(UnityEngine.Random.Range(MinMaxInternalBleedingValues.x, MinMaxInternalBleedingValues.y));
				}
			}
		}

		/// <summary>
		/// Stops a limb's external bleeding.
		/// Does not reset cutsize or any damages.
		/// </summary>
		public void StopExternalBleeding()
		{
			if(isBleedingExternally)
			{
				isBleedingExternally = false;
				StopCoroutine(Bleedout());
			}
		}

		/// <summary>
		/// Internal Bleeding logic, damage types can be overriden.
		/// </summary>
		public void InternalBleedingLogic(AttackType attackType = AttackType.Internal, DamageType damageType = DamageType.Brute)
		{
			float damageToTake = UnityEngine.Random.Range(MinMaxInternalBleedingValues.x, MinMaxInternalBleedingValues.y);
			if(currentInternalBleedingDamage >= maximumInternalBleedDamage)
			{
				BodyPart currentParent = GetParent();
				if(currentParent != null)
				{
					currentParent.TakeDamage(null, damageToTake, attackType, damageType, damageSplit: false, false, 0);
				}
			}
			else
			{
				currentInternalBleedingDamage += damageToTake;
			}
		}

		/// <summary>
		/// Checks if the player is lucky enough and is wearing enough protective armor to avoid getting his bodypart removed.
		/// </summary>
		private void DismemberBodyPartWithChance()
		{
			float chance = UnityEngine.Random.RandomRange(0.0f, 1.0f);
			float armorChanceModifer = GibChance + SelfArmor.DismembermentProtectionChance;
			if(Severity == DamageSeverity.Max || currentCutSize == BodyPartCutSize.LARGE){armorChanceModifer -= 0.25f;} //Make it more likely that the bodypart can be gibbed in it's worst condition.
			if(chance >= armorChanceModifer)
			{
				RemoveFromBodyThis();
			}
		}

		private void TakeBurnDamage(float burnDamage)
		{
			if(SelfArmor.Fire < burnDamage)
			{
				currentBurnDamage += burnDamage;
				CheckBurnDamageLevels();
			}
		}

		/// <summary>
		/// Checks and sets what damage level this body part is on, once it becomes charred; the game displays it being charred.
		/// </summary>
		private void CheckBurnDamageLevels()
		{
			if (currentBurnDamage <= 0)
			{
				currentBurnDamageLevel = BurnDamageLevels.NONE;
			}
			if (currentBurnDamage >= 25)
			{
				currentBurnDamageLevel = BurnDamageLevels.MINOR;
			}
			if (currentBurnDamage >= 50)
			{
				currentBurnDamageLevel = BurnDamageLevels.MAJOR;
			}
			if (currentBurnDamage >= 75)
			{
				if(currentBurnDamageLevel != BurnDamageLevels.CHARRED) //So we can do this once.
				{
					var spritesList = Root.ImplantBaseSpritesDictionary.Values;
					foreach (var sprites in spritesList)
					{
						foreach(var sprite in sprites)
						{
							sprite.baseSpriteHandler.SetColor(bodyPartColorWhenCharred);
						}
					}
				}
				currentBurnDamageLevel = BurnDamageLevels.CHARRED;
				AshBodyPart();
			}
		}


		/// <summary>
		/// Turns this body part into ash while protecting items inside of that cannot be ashed.
		/// </summary>
		private void AshBodyPart()
		{
			if(currentBurnDamageLevel == BurnDamageLevels.CHARRED && currentBurnDamage > bodyPartAshesAboveThisDamage)
			{
				IEnumerable<ItemSlot> internalItemList = Storage.GetItemSlots();
				IEnumerable<ItemSlot> PlayerItemList = healthMaster.PlayerScriptOwner.ItemStorage.GetItemSlots();
				foreach(ItemSlot item in internalItemList)
				{
					if(item.ItemAttributes != null) //Incase this is an empty slot
					{
						if (item.ItemAttributes.CannotBeAshed)
						{
							Inventory.ServerDespawn(item);
						}
					}
					var organ = item.ItemObject?.GetComponent<BodyPart>();
					if (organ != null)
					{
						if (organ.DeathOnRemoval)
						{
							HealthMaster.Death();
						}
					}
				}
				if(PlayerItemList != null) //In case this is not a player
				{
					foreach (ItemSlot item in PlayerItemList)
					{
						if (item.ItemAttributes != null)
						{
							if (item.ItemAttributes.CannotBeAshed)
							{
								Inventory.ServerDrop(item);
							}
							else
							{
								Inventory.ServerDespawn(item);
							}
						}
					}
				}
				if (DeathOnRemoval)
				{
					healthMaster.Death();
				}
				_ = Spawn.ServerPrefab(Storage.AshPrefab, HealthMaster.gameObject.RegisterTile().WorldPosition);
				_ = Despawn.ServerSingle(this.gameObject);
			}
		}

		/// <summary>
		/// Returns current burn damage
		/// </summary>
		/// <returns>currentBurnDamage</returns>
		public float GetCurrentBurnDamage()
		{
			return currentBurnDamage;
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
