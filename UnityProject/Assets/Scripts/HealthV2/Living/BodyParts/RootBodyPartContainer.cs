using System.Collections;
using System.Collections.Generic;
using Mono.CecilX;
using NaughtyAttributes;
using UnityEngine;

namespace HealthV2
{
	//Used for handling multiple sprites meant for base limbs and stuff
	//for example if have 2 arms this would generate a Sprite for each arm
	public class RootBodyPartContainer : MonoBehaviour
	{

		[SerializeField]
		[Required("Need a health master to send updates too." +
		          "Will attempt to find a components in its parents if not already set in editor.")]
		public LivingHealthMasterBase healthMaster = null;

		private ItemStorage storage;


		[SerializeField]
		public BodyPartType bodyPartType;

		void Awake()
		{
			healthMaster = GetComponentInParent<LivingHealthMasterBase>();
			foreach (BodyPartSprites b in GetComponentsInParent<BodyPartSprites>())
			{
				if (b.bodyPartType.Equals(bodyPartType))
				{
					Debug.Log(b);
				}

				//TODO: Do we need to add listeners for implant removal
			}

			if (storage == null)
			{
				storage = GetComponent<ItemStorage>();
			}

			storage.ServerInventoryItemSlotSet += ImplantAdded;
		}

		public ItemStorage ItemStorage;

		public PlayerSprites PlayerSprites;

		public BodyPartSprites PrefabToSpawn = null;

		public Dictionary<BodyPart, List<BodyPartSprites>> ImplantBaseSpritesDictionary =
			new Dictionary<BodyPart, List<BodyPartSprites>>();




		public List<BodyPart> ContainsLimbs = new List<BodyPart>();

		public virtual void ImplantAdded(Pickupable prevImplant, Pickupable newImplant)
		{

			//Check what's being added and add sprites if appropriate
			if (newImplant)
			{
				BodyPart implant = newImplant.GetComponent<BodyPart>();
				ContainsLimbs.Add(implant);
				healthMaster.AddNewImplant(implant);
				implant.AddedToBody(healthMaster);
				implant.Root = this;
				implant.healthMaster = healthMaster;
				foreach (var Sprite in implant.LimbSpriteData)
				{
					var Newspite = Instantiate(implant.SpritePrefab, this.transform);
					PlayerSprites.Addedbodypart.Add(Newspite);
					if (ImplantBaseSpritesDictionary.ContainsKey(implant) == false)
					{
						ImplantBaseSpritesDictionary[implant] = new List<BodyPartSprites>();
					}
					implant.RelatedPresentSprites.Add(Newspite);
					ImplantBaseSpritesDictionary[implant].Add(Newspite);
					Newspite.UpdateSpritesForImplant(implant, Sprite, this);
				}
			}

			//Remove sprites if appropriate
			if (prevImplant)
			{
				BodyPart implant = prevImplant.GetComponent<BodyPart>();
				ContainsLimbs.Remove(implant);
				healthMaster.RemoveImplant(implant);
				implant.RemovedFromBody(healthMaster);
				implant.healthMaster = null;
				implant.Root = null;
				foreach (var BodyPart in implant.GetAllBodyPartsAndItself(new List<BodyPart>()))
				{
					var oldone = ImplantBaseSpritesDictionary[BodyPart];
					foreach (var Sprite in oldone)
					{
						BodyPart.RelatedPresentSprites.Remove(Sprite);
						PlayerSprites.Addedbodypart.Remove(Sprite);
						Destroy(Sprite.gameObject);
					}

					ImplantBaseSpritesDictionary.Remove(BodyPart);
				}
			}
		}

		public void RemoveLimbs()
		{
			if (ItemStorage == null) ItemStorage = this.GetComponent<ItemStorage>();
			ItemStorage.ServerDropAll();
			PlayerSprites.livingHealthMasterBase.RootBodyPartContainers.Remove(this);
			Destroy(gameObject); //?
		}
		public virtual void ImplantUpdate(LivingHealthMasterBase healthMaster)
		{
			foreach (BodyPart prop in ContainsLimbs)
			{
				prop.ImplantUpdate(healthMaster);
			}
		}

		public virtual void ImplantPeriodicUpdate(LivingHealthMasterBase healthMaster)
		{
			foreach (BodyPart prop in ContainsLimbs)
			{
				prop.ImplantPeriodicUpdate(healthMaster);
			}
		}

		public virtual void SubBodyPartRemoved(BodyPart implant)
		{
			foreach (var BodyPart in implant.GetAllBodyPartsAndItself(new List<BodyPart>()))
			{
				var oldone = ImplantBaseSpritesDictionary[BodyPart];
				foreach (var Sprite in oldone)
				{
					BodyPart.RelatedPresentSprites.Remove(Sprite);
					PlayerSprites.Addedbodypart.Remove(Sprite);
					Destroy(Sprite.gameObject);
				}

				ImplantBaseSpritesDictionary.Remove(BodyPart);
			}
		}

		public virtual void SubBodyPartAdded(BodyPart implant)
		{
			foreach (var Sprite in implant.LimbSpriteData)
			{
				var Newspite = Instantiate(implant.SpritePrefab, this.transform);
				PlayerSprites.Addedbodypart.Add(Newspite);
				if (ImplantBaseSpritesDictionary.ContainsKey(implant) == false)
				{
					ImplantBaseSpritesDictionary[implant] = new List<BodyPartSprites>();
				}
				implant.RelatedPresentSprites.Add(Newspite);
				ImplantBaseSpritesDictionary[implant].Add(Newspite);
				Newspite.UpdateSpritesForImplant(implant, Sprite, this);
			}
		}


		public void TakeDamage(GameObject damagedBy, float damage,
			AttackType attackType, DamageType damageType)
		{
			//This is so you can still hit for example the Second Head of a double-headed thing, can be changed if we find a better solution for aiming at Specific body parts
			if (attackType == AttackType.Bomb || attackType == AttackType.Fire || attackType == AttackType.Rad)
			{
				foreach (var ContainsLimb in ContainsLimbs)
				{
					ContainsLimb.TakeDamage(damagedBy,damage/ContainsLimbs.Count,attackType,damageType);
				}
			}
			else
			{
				var OrganToDamage =	ContainsLimbs.PickRandom();
				OrganToDamage.TakeDamage(damagedBy,damage,attackType,damageType);
			}


		}

		public void HealDamage(GameObject healingItem, int healAmt,
			DamageType damageTypeToHeal)
		{
			foreach (var limb in ContainsLimbs)
			{
				//yes It technically duplicates the healing but, I've would feel pretty robbed if There was a damage on one limb Of 50
				//and I used a bandage of 50 and only healed 25,  if the healing was split across the two limbs
				limb.HealDamage(healingItem, healAmt, damageTypeToHeal);
			}
		}
	}
}