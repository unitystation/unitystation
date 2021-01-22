using System.Collections;
using System.Collections.Generic;
using Mono.CecilX;
using NaughtyAttributes;
using UnityEngine;

namespace HealthV2
{
	//Used for handling multiple sprites meant for base limbs and stuff
	//for example if have 2 arms this would generate a Sprite for each arm
	public class RootBodyPartContainer : MonoBehaviour, IBodyPartDropDownOrgans
	{
		[SerializeField]
		[Required("Need a health master to send updates too." +
		          "Will attempt to find a components in its parents if not already set in editor.")]
		public LivingHealthMasterBase healthMaster = null;

		private ItemStorage storage;

		public List<BodyPart> OptionalOrgans => optionalOrgans;


		[SerializeField] private List<BodyPart> optionalOrgans = new List<BodyPart>();


		[SerializeField] public BodyPartType bodyPartType;

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
				int i = 0;
				bool IsSurfaceSprite = implant.isSurface;
				var sprites = implant.GetBodyTypeSprites(PlayerSprites.ThisCharacter.BodyType);
				foreach (var Sprite in sprites.Item2)
				{
					var Newspite = Instantiate(implant.SpritePrefab, this.transform);
					Newspite.name = implant.name;
					PlayerSprites.Addedbodypart.Add(Newspite);
					if (ImplantBaseSpritesDictionary.ContainsKey(implant) == false)
					{
						ImplantBaseSpritesDictionary[implant] = new List<BodyPartSprites>();
					}

					if (IsSurfaceSprite)
					{
						PlayerSprites.SurfaceSprite.Add(Newspite);
					}

					implant.RelatedPresentSprites.Add(Newspite);
					ImplantBaseSpritesDictionary[implant].Add(Newspite);
					var newOrder = new SpriteOrder(sprites.Item1);
					newOrder.Add(i);
					Newspite.UpdateSpritesForImplant(implant, Sprite, this, newOrder);
					i++;
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
						if (BodyPart.isSurface)
						{
							PlayerSprites.SurfaceSprite.Remove(Sprite);
						}
						BodyPart.RelatedPresentSprites.Remove(Sprite);
						PlayerSprites.Addedbodypart.Remove(Sprite);
						Destroy(Sprite.gameObject);
					}

					ImplantBaseSpritesDictionary.Remove(BodyPart);
				}
			}
		}

		public virtual void RemoveSpecifiedFromThis(GameObject inOrgan)
		{
			storage.ServerTryRemove(inOrgan);
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
			var sprites = implant.GetBodyTypeSprites(PlayerSprites.ThisCharacter.BodyType);
			foreach (var Sprite in sprites.Item2)
			{
				var Newspite = Instantiate(implant.SpritePrefab, this.transform);
				PlayerSprites.Addedbodypart.Add(Newspite);
				if (ImplantBaseSpritesDictionary.ContainsKey(implant) == false)
				{
					ImplantBaseSpritesDictionary[implant] = new List<BodyPartSprites>();
				}

				implant.RelatedPresentSprites.Add(Newspite);
				ImplantBaseSpritesDictionary[implant].Add(Newspite);
				Newspite.UpdateSpritesForImplant(implant, Sprite, this, sprites.Item1);
			}
		}


		public void TakeDamage(GameObject damagedBy, float damage,
			AttackType attackType, DamageType damageType)
		{
			Logger.Log("dmg  > " + damage + "attackType > " + attackType + " damageType > " + damageType);
			//This is so you can still hit for example the Second Head of a double-headed thing, can be changed if we find a better solution for aiming at Specific body parts
			if (attackType == AttackType.Bomb || attackType == AttackType.Fire || attackType == AttackType.Rad)
			{
				foreach (var ContainsLimb in ContainsLimbs)
				{
					ContainsLimb.TakeDamage(damagedBy, damage / ContainsLimbs.Count, attackType, damageType);
				}
			}
			else
			{
				var OrganToDamage = ContainsLimbs.PickRandom();
				OrganToDamage.TakeDamage(damagedBy, damage, attackType, damageType);
			}
		}

		public void HealDamage(GameObject healingItem, int healAmt,
			DamageType damageTypeToHeal)
		{
			foreach (var limb in ContainsLimbs)
			{
				//yes It technically duplicates the healing but, I've would feel pretty robbed if There was a damage on one limb Of 50
				//and I used a bandage of 50 and only healed 25,  if the healing was split across the two limbs
				limb.HealDamage(healingItem, healAmt, (int) damageTypeToHeal);
			}
		}
	}
}