using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace HealthV2
{
	public partial class BodyPart
	{
		
		private bool isBleedingInternally = false;

		private bool isBleedingExternally = false;

		public bool IsBleedingInternally => isBleedingInternally;

		public bool IsBleedingExternally => isBleedingExternally;

		public Vector2 MinMaxInternalBleedingValues = new Vector2(5, 20);


		[SerializeField] private float maximumInternalBleedDamage = 100;
		public float InternalBleedingBloodLoss = 12;
		public float ExternalBleedingBloodLoss = 6;

		[SerializeField, Range(1.25f, 12.0f)] private float baseTraumaDamageMultiplier = 2.0f;

		public float MaximumInternalBleedDamage => maximumInternalBleedDamage;

		private float currentInternalBleedingDamage = 0;

		[SerializeField]
		private Color bodyPartColorWhenCharred = Color.black;

		private float currentSlashCutDamage= 0;
		public float CurrentSlashCutDamage => currentSlashCutDamage;
		private float currentPierceDamage  = 0;
		public float CurrentPierceDamage   => currentPierceDamage;
		private float currentBurnDamage    = 0;
		public float CurrentBurnDamage     => currentBurnDamage;

		[SerializeField] private float bodyPartAshesAboveThisDamage = 125;
		public float BodyPartAshesAboveThisDamage => bodyPartAshesAboveThisDamage;

		private PierceDamageLevel currentPierceDamageLevel = PierceDamageLevel.NONE;
		private SlashDamageLevel currentSlashDamageLevel = SlashDamageLevel.NONE;
		private BurnDamageLevels currentBurnDamageLevel = BurnDamageLevels.NONE;

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

		/// <summary>
		/// How much damage can this body part last before it breaks/gibs/Disembowles?
		/// <summary>
		public float DamageThreshold = 18f;
		

		[SerializeField] private bool gibsEntireBodyOnRemoval = false;

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
		/// Applies trauma damage to the body part, checks if it has enough protective armor to cancel the trauma damage
		/// and automatically checks how big is the body part's cut size.
		/// </summary>
		public void ApplyTraumaDamage(float tramuaDamage, TraumaticDamageTypes damageType = TraumaticDamageTypes.SLASH)
		{
			//We use dismember protection chance because it's the most logical value.
			if(DMMath.Prob(SelfArmor.DismembermentProtectionChance * 100) == false)
			{
				if (damageType == TraumaticDamageTypes.SLASH) { currentSlashCutDamage += MultiplyTraumaDamage(tramuaDamage); }
				if (damageType == TraumaticDamageTypes.PIERCE) { currentPierceDamage += MultiplyTraumaDamage(tramuaDamage); }
				CheckCutSize();
			}
			//Burn damage checks for it's own armor damage type.
			if (damageType == TraumaticDamageTypes.BURN)
			{
				TakeBurnDamage(MultiplyTraumaDamage(tramuaDamage));
			}
		}

		private float MultiplyTraumaDamage(float baseDamage)
		{
			if (currentBurnDamageLevel >= BurnDamageLevels.CHARRED || currentCutSize >= BodyPartCutSize.LARGE
			|| Severity >= DamageSeverity.Critical)
			{
				return baseDamage * (baseTraumaDamageMultiplier + 0.25f);
			}
			else if (currentBurnDamageLevel >= BurnDamageLevels.MAJOR || currentCutSize >= BodyPartCutSize.MEDIUM
			|| Severity >= DamageSeverity.Bad)
			{
				return baseDamage * (baseTraumaDamageMultiplier + 0.15f);
			}
			else if (currentBurnDamageLevel >= BurnDamageLevels.MINOR || currentCutSize >= BodyPartCutSize.SMALL
			|| Severity >= DamageSeverity.LightModerate)
			{
				return baseDamage * baseTraumaDamageMultiplier;
			}
			else
			{
				return baseDamage;
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
			ApplyTraumaDamage(25, TraumaticDamageTypes.PIERCE);
		}

		/// <summary>
		/// Checks how big is the cut is right now.
		/// </summary>
		private void CheckCutSize()
		{
			CheckTraumaDamageLevels();
			if(currentSlashCutDamage <= 0)
			{
				currentCutSize = BodyPartCutSize.NONE;
			}
			else if(currentSlashCutDamage > 75)
			{
				currentCutSize = BodyPartCutSize.LARGE;
			}
			else if(currentSlashCutDamage > 50)
			{
				currentCutSize = BodyPartCutSize.MEDIUM;
			}
			else if(currentSlashCutDamage > 25)
			{
				currentCutSize = BodyPartCutSize.SMALL;
			}

			if(currentCutSize >= BodyPartSlashLogicOnCutSize && CanBleedExternally)
			{
				StartCoroutine(ExternalBleedingLogic());
			}
		}

		private void CheckTraumaDamageLevels()
		{
			//Slash
			if(currentSlashCutDamage <= 0)
			{
				currentSlashDamageLevel = SlashDamageLevel.NONE;
			}
			if(currentSlashCutDamage >= 25)
			{
				currentSlashDamageLevel = SlashDamageLevel.SMALL;
			}
			if(currentSlashCutDamage <= 50)
			{
				currentSlashDamageLevel = SlashDamageLevel.MEDIUM;
			}
			else
			{
				currentSlashDamageLevel = SlashDamageLevel.LARGE;
			}
			//Pierce
			if(currentPierceDamage <= 0)
			{
				currentPierceDamageLevel = PierceDamageLevel.NONE;
			}
			if(currentPierceDamage >= 25)
			{
				currentPierceDamageLevel = PierceDamageLevel.SMALL;
			}
			if(currentPierceDamage >= 50)
			{
				currentPierceDamageLevel = PierceDamageLevel.MEDIUM;
			}
			else
			{
				currentPierceDamageLevel = PierceDamageLevel.LARGE;
			}
			//burn
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
					foreach(var sprite in RelatedPresentSprites)
					{
						sprite.baseSpriteHandler.SetColor(bodyPartColorWhenCharred);
					}
				}
				currentBurnDamageLevel = BurnDamageLevels.CHARRED;
				AshBodyPart();
			}
		}

		/// <summary>
		/// Checks if the cut is big enough for the contained organs to escape.
		/// If the cut isn't big enough or has failed a chance check, apply internal damage + bleeding.
		/// </summary>
		private void Disembowel()
		{
			BodyPart randomBodyPart = OrganList.GetRandom().GetComponent<BodyPart>();
			BodyPart randomCustomBodyPart = OptionalOrgans.GetRandom();
			if(currentCutSize >= BodyPartStorageContentsSpillOutOnCutSize)
			{
				float chance = UnityEngine.Random.Range(0.0f, 1.0f);
				if(chance >= spillChanceWhenCutPresent)
				{
					HealthMaster.DismemberingBodyParts.Add(randomBodyPart);
					if(randomCustomBodyPart != null)
					{
						HealthMaster.DismemberingBodyParts.Add(randomCustomBodyPart);
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
			IsBleeding = true;
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
					IsBleeding = false;
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
				if (IsBleeding)
				{
					HealthMaster.CirculatorySystem.Bleed(UnityEngine.Random.Range(MinMaxInternalBleedingValues.x, MinMaxInternalBleedingValues.y));
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
				BodyPart currentParent = ContainedIn;
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
				HealthMaster.DismemberingBodyParts.Add(this);
			}
		}

		private void TakeBurnDamage(float burnDamage)
		{
			if(SelfArmor.Fire < burnDamage)
			{
				currentBurnDamage += burnDamage;
				CheckTraumaDamageLevels();
			}
		}

		/// <summary>
		/// Turns this body part into ash while protecting items inside of that cannot be ashed.
		/// </summary>
		private void AshBodyPart()
		{
			if(currentBurnDamageLevel == BurnDamageLevels.CHARRED && currentBurnDamage > bodyPartAshesAboveThisDamage)
			{
				IEnumerable<ItemSlot> internalItemList = OrganStorage.GetItemSlots();
				foreach(ItemSlot item in internalItemList)
				{
					Integrity itemObject = item.ItemObject.OrNull()?.GetComponent<Integrity>();
					if(itemObject != null) //Incase this is an empty slot
					{
						if (itemObject.CannotBeAshed || itemObject.Resistances.Indestructable)
						{
							Inventory.ServerDrop(item);
						}
					}
					var organ = item.ItemObject.OrNull()?.GetComponent<BodyPart>();
					if (organ != null)
					{
						if (organ.gibsEntireBodyOnRemoval)
						{
							HealthMaster.Gib();
							return;
						}
						if (organ.DeathOnRemoval)
						{
							HealthMaster.Death();
						}
					}
				}
				if (DeathOnRemoval)
				{
					HealthMaster.Death();
				}
				_ = Spawn.ServerPrefab(OrganStorage.AshPrefab, HealthMaster.gameObject.RegisterTile().WorldPosition);
				_ = Despawn.ServerSingle(this.gameObject);
			}
		}

		
		/// <summary>
		/// Checks if the bodypart is damaged to a point where it can be gibbed from the body
		/// </summary>
		protected void CheckBodyPartIntigrity(float lastDamage)
		{
			if(currentCutSize >= BodyPartSlashLogicOnCutSize)
			{
				if(OrganList.Count != 0)
				{
					Disembowel();
				}
			}
			if(Severity >= GibsOnSeverityLevel && lastDamage >= DamageThreshold)
			{
				DismemberBodyPartWithChance();
			}
		}

		public void HealTraumaticDamage(float healAmount, TraumaticDamageTypes damageTypeToHeal)
		{
			if (damageTypeToHeal == TraumaticDamageTypes.BURN)
			{
				currentBurnDamage -= healAmount;
			}
			if (damageTypeToHeal == TraumaticDamageTypes.SLASH)
			{
				currentSlashCutDamage -= healAmount;
			}
			if (damageTypeToHeal == TraumaticDamageTypes.PIERCE)
			{
				currentPierceDamage -= healAmount;
			}
			CheckCutSize();
			CheckTraumaDamageLevels();
		}
	}
}