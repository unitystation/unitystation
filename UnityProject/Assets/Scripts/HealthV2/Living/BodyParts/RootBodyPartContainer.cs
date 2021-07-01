using System.Collections;
using System.Collections.Generic;
using Mirror;
using NaughtyAttributes;
using Newtonsoft.Json;
using UnityEngine;

namespace HealthV2
{
	/// <summary>
	/// A container for multiple body parts each with sprites to be rendered on the player.  Used to
	/// group two or more like body parts, eg two arms, two legs, etc and generate a sprite for each.
	/// </summary>
	public class RootBodyPartContainer : MonoBehaviour, IBodyPartDropDownOrgans, IServerSpawn
	{
		[Required("Need a health master to send updates too. " +
				  "Will attempt to find a components in its parents if not already set in editor.")]
		[SerializeField] public LivingHealthMasterBase healthMaster = null;

		/// <summary>
		/// Storage container for things (typcially other organs) held within this body part
		/// </summary>
		[Tooltip("Things (eg other organs) held within this")]
		public ItemStorage Storage;

		/// <summary>
		/// The storage container for the items held by the limbs
		/// </summary>
		public ItemStorage ItemStorage;

		[Tooltip("List of optional body added to this, eg what wings a Moth has")]
		[SerializeField] private List<BodyPart> optionalOrgans = new List<BodyPart>();
		/// <summary>
		/// The list of body parts that are allowed to be stored inside this body part container
		/// </summary>
		public List<BodyPart> OptionalOrgans => optionalOrgans;

		/// <summary>
		/// The category that this body part container falls under for purposes of targeting with the UI
		/// </summary>
		[Tooltip("The category that this falls under for targeting purposes")]
		[SerializeField] public BodyPartType BodyPartType;

		/// <summary>
		/// Player sprites for rendering equipment and clothing on the body part container
		/// </summary>
		[Tooltip("Player sprites for rendering equipment and clothing on this")]
		public PlayerSprites PlayerSprites;

		/// <summary>
		/// The prefab sprites for this body part container
		/// </summary>
		[Tooltip("The prefab sprites for this")]
		public BodyPartSprites PrefabToSpawn = null;

		/// <summary>
		/// A dictionary of all of the body parts and their list of associated sprites contained within
		/// this body part container.
		/// </summary>
		[Tooltip("all of the body parts and their list of associated sprites contained within this")]
		public Dictionary<BodyPart, List<BodyPartSprites>> ImplantBaseSpritesDictionary =
			new Dictionary<BodyPart, List<BodyPartSprites>>();

		/// <summary>
		/// The list of individual limbs contained within this Body Part Container
		/// </summary>
		[Tooltip("Individual limbs contained within this")]
		public List<BodyPart> ContainsLimbs = new List<BodyPart>();

		[HideInInspector] public bool IsBleeding = false;

		/// <summary>
		/// How much blood does the body lose when there is lost limbs in this container?
		/// </summary>
		[SerializeField, Tooltip("How much blood does the body lose when there is lost limbs in this container?")]
		private float limbLossBleedingValue = 35f;

		public RootBodyPartController RootBodyPartController;

		/// <summary>
		/// The list of the internal net ids of the body parts contained within this container
		/// </summary>
		[Tooltip("The internal net ids of the body parts contained within this")]
		public List<uint> InternalNetIDs;

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
				if (b.BodyPartType.Equals(BodyPartType))
				{
					Debug.Log(b);
				}

				//TODO: Do we need to add listeners for implant removal
			}

			if (Storage == null)
			{
				Storage = GetComponent<ItemStorage>();
			}

			Storage.ServerInventoryItemSlotSet += ImplantAdded;
		}

		public void UpdateChildren(List<uint> NewInternalNetIDs)
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

		/// <summary>
		/// Transfers an item from an item slot to the body part container's internal storage, usually
		/// another body part
		/// </summary>
		/// <param name="ItemSlot">Item Slot to transfer from</param>
		public virtual void AddBodyPartSlot(ItemSlot ItemSlot)
		{
			Storage.ServerTryTransferFrom(ItemSlot);
		}

		/// <summary>
		/// Adds a new body part to this body part container, and removes the old part whose place is
		/// being taken if possible
		/// </summary>
		/// <param name="prevImplant">Old body part to be removed</param>
		/// <param name="newImplant">New body part to be added</param>
		public virtual void ImplantAdded(Pickupable prevImplant, Pickupable newImplant)
		{
			//Check what's being added and add sprites if appropriate
			if (newImplant)
			{
				BodyPart implant = newImplant.GetComponent<BodyPart>();
				ContainsLimbs.Add(implant);
				healthMaster.AddNewImplant(implant);
				SubBodyPartAdded(implant);
				implant.Root = this;
				implant.HealthMaster = healthMaster;
				implant.SetUpSystems();
				SetupSpritesNID(implant);
			}

			//Remove sprites if appropriate
			if (prevImplant)
			{
				BodyPart implant = prevImplant.GetComponent<BodyPart>();
				ContainsLimbs.Remove(implant);
				healthMaster.RemoveImplant(implant);
				implant.RemovedFromBody(healthMaster);
				implant.HealthMaster = null;
				implant.Root = null;
				RemoveSpritesNID(implant);
			}
		}

		/// <summary>
		/// Sets up the sprite of a specified body part and adds its Net ID to InternalNetIDs
		/// </summary>
		/// <param name="implant">Body Part to display</param>
		public void SetupSpritesNID(BodyPart implant)
		{

			int i = 0;
			bool isSurfaceSprite = implant.IsSurface;
			var sprites = implant.GetBodyTypeSprites(PlayerSprites.ThisCharacter.BodyType);
			foreach (var Sprite in sprites.Item2)
			{
				var newSprite = Spawn.ServerPrefab(implant.SpritePrefab.gameObject, Vector3.zero, this.transform)
					.GameObject.GetComponent<BodyPartSprites>();
				newSprite.transform.localPosition = Vector3.zero;
				//newSprite.name = implant.name;
				PlayerSprites.Addedbodypart.Add(newSprite);
				if (ImplantBaseSpritesDictionary.ContainsKey(implant) == false)
				{
					ImplantBaseSpritesDictionary[implant] = new List<BodyPartSprites>();
				}

				if (isSurfaceSprite)
				{
					PlayerSprites.SurfaceSprite.Add(newSprite);
				}

				implant.RelatedPresentSprites.Add(newSprite);
				ImplantBaseSpritesDictionary[implant].Add(newSprite);
				var newOrder = new SpriteOrder(sprites.Item1);
				newOrder.Add(i);
				newSprite.UpdateSpritesForImplant(implant, implant.ClothingHide, Sprite, this, newOrder);
				InternalNetIDs.Add(newSprite.GetComponent<NetworkIdentity>().netId);

				i++;
				i++;
			}
			RootBodyPartController.RequestUpdate(this);
			
			if (implant.SetCustomisationData != "")
			{
				implant.LobbyCustomisation.OnPlayerBodyDeserialise(implant,
					implant.SetCustomisationData,
					healthMaster);
			}

		}

		/// <summary>
		/// Removes a body part from the container, including its sprite and Net ID
		/// </summary>
		/// <param name="implant">Body Part to remove</param>
		public void RemoveSpritesNID(BodyPart implant)
		{
			foreach (var BodyPart in implant.GetAllBodyPartsAndItself(new List<BodyPart>()))
			{
				if (ImplantBaseSpritesDictionary.ContainsKey(BodyPart) == false) continue;

				var oldone = ImplantBaseSpritesDictionary[BodyPart];
				foreach (var Sprite in oldone)
				{
					InternalNetIDs.Remove(Sprite.GetComponent<NetworkIdentity>().netId);
					if (BodyPart.IsSurface)
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

		/// <summary>
		/// Removes a specified item (usually a body part) from the body part containers internal storage
		/// </summary>
		/// <param name="inOrgan">Item to remove</param>
		public virtual void RemoveSpecifiedFromThis(GameObject inOrgan)
		{
			Storage.ServerTryRemove(inOrgan);
		}

		/// <summary>
		/// Removes all limbs from the container and drops all items that they are holding
		/// </summary>
		public void RemoveLimbs()
		{
			if (ItemStorage == null) ItemStorage = this.GetComponent<ItemStorage>();
			ItemStorage.ServerDropAll();
			PlayerSprites.livingHealthMasterBase.RootBodyPartContainers.Remove(this);
		}

		public virtual void ImplantUpdate()
		{
			foreach (BodyPart prop in ContainsLimbs)
			{
				prop.ImplantUpdate();
			}
		}

		/// <summary>
		/// Updates the body part container and all contained body parts relative to their related
		/// systems (default: blood system, radiation damage) each second.
		/// </summary>
		public virtual void ImplantPeriodicUpdate()
		{
			foreach (BodyPart prop in ContainsLimbs)
			{
				prop.ImplantPeriodicUpdate();
			}
			if(IsBleeding)
			{
				healthMaster.CirculatorySystem.Bleed(limbLossBleedingValue);
			}
		}

		/// <summary>
		/// Removes a specified body part contained within this body part container from the host body system.
		/// Called by contained body parts. See BodyPart's "Body Part Storage Methods" for more info
		/// </summary>
		/// <param name="implant">Body Part to be removed</param>
		public virtual void SubBodyPartRemoved(BodyPart implant)
		{
			RemoveSpritesNID(implant);
			IsBleeding = true;
		}

		/// <summary>
		/// Adds a specified body part contained within this body part container to the host body system.
		/// Called by contained body parts. See BodyPart's "Body Part Storage Methods" for more info
		/// </summary>
		/// <param name="implant">Body Part to be added</param>
		public virtual void SubBodyPartAdded(BodyPart implant)
		{
			SetupSpritesNID(implant);
		}

		/// <summary>
		/// Damages the body parts contained within the body part container. Damage will either be spread evenly across all
		/// contained body parts (spread damage like a shotgun) or dealt to one body part (pinpoint damage like a knife)
		/// Damaged body parts will assign damage to their contained body parts according to BodyPart.TakeDamage.
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
			float armorPenetration = 0,
			bool damageSplit = false
		)
		{
			//Logger.Log("dmg  > " + damage + "attackType > " + attackType + " damageType > " + damageType);
			//This is so you can still hit for example the Second Head of a double-headed thing, can be changed if we find a better solution for aiming at Specific body parts
			if (damageSplit || attackType == AttackType.Bomb || attackType == AttackType.Fire || attackType == AttackType.Rad)
			{
				//We don't use foreach to avoid errors when the list gets modifed.
				for(int limbCount = ContainsLimbs.Count - 1; limbCount >= 0; limbCount--)
				{
					ContainsLimbs[limbCount].TakeDamage(damagedBy,
						damage / ContainsLimbs.Count,
						attackType,
						damageType,
						damageSplit,
						armorPenetration: armorPenetration
						);
				}
			}
			else
			{
				var OrganToDamage = ContainsLimbs.PickRandom();
				if (OrganToDamage != null)
				{
					OrganToDamage.TakeDamage(
						damagedBy,
						damage,
						attackType,
						damageType,
						damageSplit,
						armorPenetration: armorPenetration
					);
				}
			}
		}

		public void TakeTraumaDamage(float damage, BodyPart.TramuticDamageTypes damageType)
		{
			//Check if we do more than one type of trauma damage at once.
			//If true, don't run the indivual case check.
			//We also do a check for a body part's existence in case it got removed by the previous damage.
			foreach(BodyPart limb in ContainsLimbs)
			{
				if(damageType.HasFlag(BodyPart.TramuticDamageTypes.BURN) && damageType.HasFlag(BodyPart.TramuticDamageTypes.SLASH)
				|| damageType.HasFlag(BodyPart.TramuticDamageTypes.BURN) && damageType.HasFlag(BodyPart.TramuticDamageTypes.PIERCE))
				{
					if (damageType.HasFlag(BodyPart.TramuticDamageTypes.SLASH))
					{
						limb.ApplyTraumaDamage(damage);
					}
					else
					{
						limb.ApplyTraumaDamage(damage, BodyPart.TramuticDamageTypes.PIERCE);
					}
					limb.ApplyTraumaDamage(damage, BodyPart.TramuticDamageTypes.BURN);
					return;
				}
				switch (damageType)
				{
					case BodyPart.TramuticDamageTypes.SLASH:
						limb.ApplyTraumaDamage(damage);
						continue;
					case BodyPart.TramuticDamageTypes.BURN:
						limb.ApplyTraumaDamage(damage, BodyPart.TramuticDamageTypes.BURN);
						continue;
					case BodyPart.TramuticDamageTypes.PIERCE:
						limb.ApplyTraumaDamage(damage, BodyPart.TramuticDamageTypes.PIERCE);
						break;
				}
			}
		}

		/// <summary>
		/// Removes damage from limbs. Currently heals all limbs contained in the container by the heal amount
		/// </summary>
		/// <param name="healingItem">The game object performing the healing</param>
		/// <param name="healAmt">Amount each limb heals</param>
		/// <param name="damageTypeToHeal">The type of damage to heal</param>
		public void HealDamage(GameObject healingItem, int healAmt,
			DamageType damageTypeToHeal)
		{
			foreach (var limb in ContainsLimbs)
			{
				//yes It technically duplicates the healing but, I've would feel pretty robbed if There was a damage on one limb Of 50
				//and I used a bandage of 50 and only healed 25,  if the healing was split across the two limbs
				limb.HealDamage(healingItem, healAmt, (int)damageTypeToHeal);
			}
		}

		public void HealTraumaDamage(float healAmt, BodyPart.TramuticDamageTypes typeToHeal)
		{
			foreach(BodyPart limb in ContainsLimbs)
			{
				limb.HealTraumaticDamage(healAmt, typeToHeal);
			}
		}
	}
}
