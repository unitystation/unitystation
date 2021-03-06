using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mirror;
using NaughtyAttributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace HealthV2
{
	//Used for handling multiple sprites meant for base limbs and stuff
	//for example if have 2 arms this would generate a Sprite for each arm
	public class RootBodyPartContainer : MonoBehaviour, IBodyPartDropDownOrgans, IServerSpawn
	{
		[SerializeField]
		[Required("Need a health master to send updates too." +
		          "Will attempt to find a components in its parents if not already set in editor.")]
		public LivingHealthMasterBase healthMaster = null;

		private ItemStorage storage;

		public List<BodyPart> OptionalOrgans => optionalOrgans;


		[SerializeField] private List<BodyPart> optionalOrgans = new List<BodyPart>();


		[SerializeField] public BodyPartType bodyPartType;

		public void OnSpawnServer(SpawnInfo info)
		{
			healthMaster = GetComponentInParent<LivingHealthMasterBase>();
			PlayerSprites = GetComponentInParent<PlayerSprites>();
		}

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


		public RootBodyPartController RootBodyPartController;

		public List<uint> InternalNetIDs;

		public void UpdateChildren(List<uint> NewInternalNetIDs )
		{
			List<SpriteHandler> SHS = new List<SpriteHandler>();
			InternalNetIDs = NewInternalNetIDs;
			foreach (var ID in InternalNetIDs)
			{

				if (NetworkIdentity.spawned.ContainsKey(ID) && NetworkIdentity.spawned[ID] != null)
				{
					var OB = NetworkIdentity.spawned[ID].gameObject.transform;

					var SHSs = OB.GetComponentsInChildren<SpriteHandler>();
					// foreach (var SH in SHSs)
					// {
						// var Net= SpriteHandlerManager.GetRecursivelyANetworkBehaviour(SH.gameObject);
						// SpriteHandlerManager.UnRegisterHandler(Net, SH);
					// }

					OB.parent = this.transform;
					OB.localScale = Vector3.one;
					OB.localPosition = Vector3.zero;
					OB.localRotation = Quaternion.identity;

					foreach (var SH in SHSs)
					{
						SHS.Add(SH);



						// var Net = SpriteHandlerManager.GetRecursivelyANetworkBehaviour(SH.gameObject);
						// SpriteHandlerManager.RegisterHandler(Net,SH );
					}
					var BPS = OB.GetComponent<BodyPartSprites>();
					if (PlayerSprites.Addedbodypart.Contains(BPS) == false)
					{
						PlayerSprites.Addedbodypart.Add(BPS);
					}
				}

			}

			// RequestForceSpriteUpdate.Send(SpriteHandlerManager.Instance, SHS);
		}



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
				SetupSpritesNID(implant);
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
				RemoveSpritesNID(implant);
			}
		}


		public void SetupSpritesNID(BodyPart implant )
		{

			int i = 0;
			bool IsSurfaceSprite = implant.isSurface;
			var sprites = implant.GetBodyTypeSprites(PlayerSprites.ThisCharacter.BodyType);
			foreach (var Sprite in sprites.Item2)
			{
				var Newspite = Spawn.ServerPrefab(implant.SpritePrefab.gameObject, Vector3.zero, this.transform)
					.GameObject.GetComponent<BodyPartSprites>();
				Newspite.transform.localPosition = Vector3.zero;
				//Newspite.name = implant.name;
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
				Newspite.UpdateSpritesForImplant(implant, implant.ClothingHide, Sprite, this, newOrder);
				InternalNetIDs.Add(Newspite.GetComponent<NetworkIdentity>().netId);

				i++;
			}
			RootBodyPartController.RequestUpdate(this);
		}

		public void RemoveSpritesNID(BodyPart implant)
		{
			foreach (var BodyPart in implant.GetAllBodyPartsAndItself(new List<BodyPart>()))
			{
				if (ImplantBaseSpritesDictionary.ContainsKey(BodyPart) == false) continue;

				var oldone = ImplantBaseSpritesDictionary[BodyPart];
				foreach (var Sprite in oldone)
				{
					InternalNetIDs.Remove(Sprite.GetComponent<NetworkIdentity>().netId);
					if (BodyPart.isSurface)
					{
						PlayerSprites.SurfaceSprite.Remove(Sprite);
					}

					BodyPart.RelatedPresentSprites.Remove(Sprite);
					PlayerSprites.Addedbodypart.Remove(Sprite);
					Destroy(Sprite.gameObject);
				}
				RootBodyPartController.RequestUpdate(this);
				ImplantBaseSpritesDictionary.Remove(BodyPart);
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

		//yes is uesd
		public virtual void SubBodyPartRemoved(BodyPart implant)
		{
			RemoveSpritesNID(implant);
		}


		//yes is uesd
		public virtual void SubBodyPartAdded(BodyPart implant)
		{
			SetupSpritesNID(implant);
		}


		public void TakeDamage(GameObject damagedBy, float damage,
			AttackType attackType, DamageType damageType, bool SplitDamage = false)
		{
			//Logger.Log("dmg  > " + damage + "attackType > " + attackType + " damageType > " + damageType);
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
				if (SplitDamage)
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