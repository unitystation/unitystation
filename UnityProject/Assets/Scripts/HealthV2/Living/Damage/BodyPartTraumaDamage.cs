using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace HealthV2
{
	public partial class BodyPart
	{

		[Header("Trauma Damage settings")]
		[SerializeField] private float maximumInternalBleedDamage = 100;
		[SerializeField] private Vector2 minMaxInternalBleedingBloodLoss = new Vector2(2,4);
		[SerializeField] private Vector2 minMaxExternalBleedingValues    = new Vector2(2, 14);

		public Vector2 MinMaxExternalBleedingValues => minMaxExternalBleedingValues;
		private bool isBleedingInternally = false;
		private bool isBleedingExternally = false;
		public bool IsBleedingInternally => isBleedingInternally;
		public bool IsBleedingExternally => isBleedingExternally;
		public float MaximumInternalBleedDamage => maximumInternalBleedDamage;

		private float currentInternalBleedingDamage = 0;

		[SerializeField]
		private Color bodyPartColorWhenCharred = Color.black;

		private TraumaDamageLevel currentPierceDamageLevel = TraumaDamageLevel.NONE;
		private TraumaDamageLevel currentSlashDamageLevel  = TraumaDamageLevel.NONE;
		private TraumaDamageLevel currentBurnDamageLevel   = TraumaDamageLevel.NONE;
		private TraumaDamageLevel currentBluntDamageLevel  = TraumaDamageLevel.NONE; //TODO : MERGE #7434 TO FINISH #7432

		public TraumaDamageLevel CurrentPierceDamageLevel => currentPierceDamageLevel;
		public TraumaDamageLevel CurrentSlashDamageLevel  => currentSlashDamageLevel ;
		public TraumaDamageLevel CurrentBurnDamageLevel   => currentBurnDamageLevel;
		public TraumaDamageLevel CurrentBluntDamageLevel  => currentBluntDamageLevel;


		public DamageSeverity GibsOnSeverityLevel = DamageSeverity.Max;
		public int GibChance = 15;

		/// <summary>
		/// How likely does the contents of this body part's storage to spill out?
		/// </summary>
		[SerializeField,
		Tooltip("How likely does the contents of this body part's storage to spill out when a large enough cut exists?"),
		Range(0,1.0f)]
		private float spillChanceWhenCutPresent = 0.5f;

		public bool CanBleedInternally = false;

		public bool CanBeBroken        = false;


		/// <summary>
		/// How much damage can this body part last before it breaks/gibs/Disembowles?
		/// <summary>
		public float DamageThreshold = 21f;

		[SerializeField] private DamageSeverity BoneFracturesOnDamageSevarity = DamageSeverity.Moderate;
		[SerializeField] private DamageSeverity BoneBreaksOnDamageSevarity    = DamageSeverity.Bad;


		[SerializeField] private bool gibsEntireBodyOnRemoval = false;

		public float CurrentInternalBleedingDamage
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
		public void ApplyTraumaDamage(TraumaticDamageTypes damageType = TraumaticDamageTypes.SLASH, bool ignoreSeverityCheck = false)
		{
			if(Severity < DamageSeverity.LightModerate && ignoreSeverityCheck == false) return;
			//We use dismember protection chance because it's the most logical value.
			if(DMMath.Prob(SelfArmor.DismembermentProtectionChance * 100) == false)
			{
				if (damageType == TraumaticDamageTypes.SLASH)  currentSlashDamageLevel   += 1;
				if (damageType == TraumaticDamageTypes.PIERCE) currentPierceDamageLevel  += 1;
			}
			//Burn and blunt damage checks for it's own armor damage type.
			if (damageType == TraumaticDamageTypes.BURN)
			{
				TakeBurnDamage();
			}

			if (damageType == TraumaticDamageTypes.BLUNT)
			{
				if (DMMath.Prob(SelfArmor.Melee * 100) == false)
				{
					TakeBluntDamage();
				}
			}
		}

		public void TakeBluntDamage()
		{
			void TakeBluntLogic(BodyPart bodyPart)
			{
				bodyPart.currentBluntDamageLevel += 1;
				Chat.AddActionMsgToChat(HealthMaster.gameObject,
					$"You hear a loud crack from your {BodyPartReadableName}.",
					$"A loud crack can be heard from {HealthMaster.playerScript.visibleName}.");
			}

			foreach (ItemSlot slot in OrganStorage.GetIndexedSlots())
			{
				if (slot.IsEmpty) return;
				if (slot.Item.gameObject.TryGetComponent<BodyPart>(out var bodyPart))
				{
					if (bodyPart.CanBeBroken) TakeBluntLogic(bodyPart);
				}
			}
		}

		[ContextMenu("Debug - Apply 25 Slash Damage")]
		private void DEBUG_ApplyTestSlash()
		{
			ApplyTraumaDamage(TraumaticDamageTypes.SLASH, true);
		}

		[ContextMenu("Debug - Apply 25 Pierce Damage")]
		private void DEBUG_ApplyTestPierce()
		{
			ApplyTraumaDamage(TraumaticDamageTypes.PIERCE, true);
		}

		private void CheckCharredBodyPart()
		{
			if (currentBurnDamageLevel == TraumaDamageLevel.SERIOUS) //So we can do this once.
			{
				foreach (var sprite in RelatedPresentSprites)
				{
					sprite.baseSpriteHandler.SetColor(bodyPartColorWhenCharred);
				}
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
				if(randomCustomBodyPart != null) randomCustomBodyPart.ApplyInternalDamage();
			}

			if (currentPierceDamageLevel >= TraumaDamageLevel.SMALL) StartCoroutine(ExternalBleedingLogic());
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
			if(isBleedingExternally || IsSurface == false) yield break;
			bool willCloseOnItsOwn = false;
			isBleedingExternally = true;
			IsBleeding = true;
			StartCoroutine(Bleedout());
			if(currentSlashDamageLevel <= TraumaDamageLevel.SERIOUS || currentPierceDamageLevel <= TraumaDamageLevel.SMALL)
			{
				willCloseOnItsOwn = true;
			}
			if(willCloseOnItsOwn)
			{
				yield return WaitFor.Seconds(128);
				if(currentSlashDamageLevel <= TraumaDamageLevel.SERIOUS || currentPierceDamageLevel <= TraumaDamageLevel.SMALL)
				{
					StopExternalBleeding();
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
					HealthMaster.CirculatorySystem.Bleed(UnityEngine.Random.Range(MinMaxExternalBleedingValues.x, MinMaxExternalBleedingValues.y));
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
			float damageToTake = UnityEngine.Random.Range(minMaxInternalBleedingBloodLoss.x, minMaxInternalBleedingBloodLoss.y);
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
			if (GibChance == 0)
				return;
			var armorChanceModifer = GibChance - SelfArmor.DismembermentProtectionChance;
			if (Severity == DamageSeverity.Max)
			{
				armorChanceModifer += 25; //Make it more likely that the bodypart can be gibbed in it's worst condition.
			}

			var chance = UnityEngine.Random.Range(0, 100);
			if(chance < armorChanceModifer)
			{
				HealthMaster.DismemberingBodyParts.Add(this);
			}
		}

		private void TakeBurnDamage()
		{
			if(SelfArmor.Fire < TotalDamage)
			{
				currentBurnDamageLevel += 1;
				CheckCharredBodyPart();
				AshBodyPart();
			}
		}

		/// <summary>
		/// Turns this body part into ash while protecting items inside of that cannot be ashed.
		/// </summary>
		private void AshBodyPart()
		{
			if(currentBurnDamageLevel >= TraumaDamageLevel.CRITICAL)
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
			if(currentPierceDamageLevel >= TraumaDamageLevel.SERIOUS)
			{
				if(OrganList.Count != 0)
				{
					Disembowel();
				}
			}

			if (currentSlashDamageLevel >= TraumaDamageLevel.SERIOUS) StartCoroutine(ExternalBleedingLogic());
			if(Severity >= GibsOnSeverityLevel && lastDamage >= DamageThreshold || currentSlashDamageLevel > TraumaDamageLevel.CRITICAL)
			{
				DismemberBodyPartWithChance();
			}
		}

		public void HealTraumaticDamage(TraumaticDamageTypes damageTypeToHeal)
		{
			if (damageTypeToHeal == TraumaticDamageTypes.BURN)
			{
				currentBurnDamageLevel -= 1;
			}
			if (damageTypeToHeal == TraumaticDamageTypes.SLASH)
			{
				currentSlashDamageLevel -= 1;
			}
			if (damageTypeToHeal == TraumaticDamageTypes.PIERCE)
			{
				currentPierceDamageLevel -= 1;
			}
		}
	}
}