using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Systems.Clothing;
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
		public List<intName> InternalNetIDs = new List<intName>();

		public List<BodyPartSprites> ClientSprites = new List<BodyPartSprites>();

		public class intName
		{
			public int Int;
			public string Name;

			[NonSerialized] public BodyPartSprites RelatedSprite;

			public ClothingHideFlags ClothingHide;
			public string Data;
		}

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
			//TODO Make generic version for mobs \/
			Storage.SetRegisterPlayer(healthMaster.GetComponent<RegisterPlayer>());
		}


		public void UpdateChildren(List<intName> NewInternalNetIDs)
		{
			List<SpriteHandler> SHS = new List<SpriteHandler>();
			//InternalNetIDs = NewInternalNetIDs;
			//Destroy missing!
			//Handle duplicates



			foreach (var ID in NewInternalNetIDs)
			{
				bool Contains = false;
				foreach (var InetID in InternalNetIDs)
				{
					if (InetID.Name == ID.Name)
					{
						Contains = true;
					}
				}

				if (Contains == false)
				{
					if (CustomNetworkManager.Instance.allSpawnablePrefabs.Count > ID.Int)
					{
						var OB = Instantiate(CustomNetworkManager.Instance.allSpawnablePrefabs[ID.Int],this.gameObject.transform).transform;
						var Net = SpriteHandlerManager.GetRecursivelyANetworkBehaviour(OB.gameObject);
						var Handlers = OB.GetComponentsInChildren<SpriteHandler>();

						foreach (var SH in Handlers)
						{
							SpriteHandlerManager.UnRegisterHandler(Net, SH);
						}

						OB.SetParent(this.transform);
						OB.localScale = Vector3.one;
						OB.localPosition = Vector3.zero;
						OB.localRotation = Quaternion.identity;

						var BPS = OB.GetComponent<BodyPartSprites>();
						BPS.SetName(ID.Name);
						ClientSprites.Add(BPS);
						BPS.ParentContainer = this;
						if (PlayerSprites.Addedbodypart.Contains(BPS) == false)
						{
							PlayerSprites.Addedbodypart.Add(BPS);
						}

						foreach (var SH in Handlers)
						{
							SHS.Add(SH);
							SpriteHandlerManager.RegisterHandler(Net, SH);
						}
					}
				}
			}

			RequestForceSpriteUpdate.Send(SpriteHandlerManager.Instance, SHS);
			foreach (var ID in InternalNetIDs)
			{
				bool Contains = false;
				foreach (var InetID in NewInternalNetIDs)
				{
					if (InetID.Name == ID.Name)
					{
						Contains = true;
					}
				}

				if (Contains == false)
				{
					foreach (var bodyPartSpritese in ClientSprites.ToArray())
					{
						if (bodyPartSpritese.name == ID.Name)
						{
							if (PlayerSprites.Addedbodypart.Contains(bodyPartSpritese))
							{
								PlayerSprites.Addedbodypart.Remove(bodyPartSpritese);
							}
							ClientSprites.Remove(bodyPartSpritese);
							Destroy(bodyPartSpritese.gameObject);
						}

					}
				}
			}

			foreach (var bodyPartSpritese in ClientSprites)
			{
				foreach (var internalNetID in NewInternalNetIDs)
				{
					if (internalNetID.Name == bodyPartSpritese.name)
					{
						bodyPartSpritese.UpdateData(internalNetID.Data);
						bodyPartSpritese.UpdateHideDlags(internalNetID.ClothingHide);
					}
				}
			}
			InternalNetIDs = NewInternalNetIDs;
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
				var newSprite = Instantiate(implant.SpritePrefab.gameObject, this.transform).transform.GetComponent<BodyPartSprites>();
				var Net = SpriteHandlerManager.GetRecursivelyANetworkBehaviour(newSprite.gameObject);
				var Handlers = newSprite.GetComponentsInChildren<SpriteHandler>();

				foreach (var SH in Handlers)
				{
					SpriteHandlerManager.UnRegisterHandler(Net, SH);
				}

				newSprite.ParentContainer = this;
				var ClientData = new intName();
				ClientData.RelatedSprite = newSprite;

				ClientData.Name = implant.name + "_" + i + "_" + implant.GetInstanceID(); //is Fine because name is being Networked
				newSprite.SetName(ClientData.Name);
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
				ClientData.Int =
					CustomNetworkManager.Instance.IndexLookupSpawnablePrefabs[implant.SpritePrefab.gameObject];
				InternalNetIDs.Add(ClientData);
				foreach (var SH in Handlers)
				{
					SpriteHandlerManager.RegisterHandler(Net, SH);
				}

				i++;
				i++;
				i++;
			}


			if (implant.SetCustomisationData != "")
			{
				implant.LobbyCustomisation.OnPlayerBodyDeserialise(implant,
					implant.SetCustomisationData,
					healthMaster);
			}

			UpdateThis();

		}

		public void UpdateThis()
		{
			RootBodyPartController.RequestUpdate(this);
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
				int i = 0;
				var oldone = ImplantBaseSpritesDictionary[BodyPart];
				foreach (var Sprite in oldone)
				{
					string Name = BodyPart.name + "_" + i + "_" + implant.GetInstanceID(); //is Fine because name is being Networked
					foreach (var _intName in InternalNetIDs.ToArray())
					{
						if (Name == _intName.Name)
						{
							InternalNetIDs.Remove(_intName);
						}
					}
					if (BodyPart.IsSurface)
					{
						PlayerSprites.SurfaceSprite.Remove(Sprite);
					}

					BodyPart.RelatedPresentSprites.Remove(Sprite);
					PlayerSprites.Addedbodypart.Remove(Sprite);
					Destroy(Sprite.gameObject);
					i++;
					i++;
					i++;
				}
				UpdateThis();
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

		/// <summary>
		/// Applies Trauma damage to a body part.
		/// </summary>
		public void TakeTraumaDamage(float damage, BodyPart.TramuticDamageTypes damageType)
		{
			foreach(BodyPart limb in ContainsLimbs)
			{
				if (damageType.HasFlag(BodyPart.TramuticDamageTypes.BURN))
				{
					limb.ApplyTraumaDamage(damage, BodyPart.TramuticDamageTypes.BURN);
				}
				if (damageType.HasFlag(BodyPart.TramuticDamageTypes.SLASH))
				{
					limb.ApplyTraumaDamage(damage);
				}
				if (damageType.HasFlag(BodyPart.TramuticDamageTypes.PIERCE))
				{
					limb.ApplyTraumaDamage(damage, BodyPart.TramuticDamageTypes.PIERCE);
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
