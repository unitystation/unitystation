using System;
using System.Collections;
using System.Collections.Generic;
using InGameEvents;
using Logs;
using Mirror;
using Systems.Explosions;
using UnityEngine;
using Util;
using Weapons.Projectiles;
using Weapons.Projectiles.Behaviours;

public class ItemPlinth : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>, IOnHitDetect
{
	[SerializeField] private SpriteHandler itemSpriteHandler;

	private ItemStorage itemStorage;

	public event Action OnItemChange;

	public Pickupable DisplayedItem { get; private set; }

	public bool HasItem => itemStorage.GetIndexedItemSlot(0).IsOccupied;

	public void Awake()
	{
		itemStorage = this.GetComponentCustom<ItemStorage>();
		OnItemChange += UpdateItemDisplay;
	}

	#region Interaction

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		if (Validations.IsTarget(gameObject, interaction) == false) return false;

		if (interaction.IsAltClick) return false;

		if (HasItem)
		{
			if (interaction.HandSlot.Item != null) return false;
		}
		else
		{
			if (interaction.HandSlot.Item == null) return false;
		}


		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		if (HasItem)
		{
			Inventory.ServerTransfer(itemStorage.GetIndexedItemSlot(0), interaction.HandSlot);
			DisplayedItem = null;
			OnItemChange?.Invoke();
		}
		else
		{
			DisplayedItem = interaction.HandSlot.Item;
			Inventory.ServerTransfer(interaction.HandSlot, itemStorage.GetNextEmptySlot());
			OnItemChange?.Invoke();
		}

	}

	private void UpdateItemDisplay()
	{
		if(DisplayedItem == null)
		{
			itemSpriteHandler.ChangeSprite(0);
			return;
		}

		itemSpriteHandler.SetSpriteSO(DisplayedItem.GetComponentInChildren<SpriteHandler>().GetCurrentSpriteSO());
	}

	#endregion

	public void OnHitDetect(OnHitDetectData data)
	{
		if (DisplayedItem == null) return;
		var TechnologyLaser = data.BulletObject.GetComponent<ContainsResearchData>();

		if (TechnologyLaser == null) return;
		if (TechnologyLaser.ResearchData.Technology != null) return;


		foreach (var Design in DisplayedItem.GetComponent<ItemResearchPotential>().TechWebDesigns)
		{
			if (Design.Technology == null)
			{
				Design.Technology = TechnologyLaser.ShotFrom.researchServer.Techweb.AvailableTech.PickRandom();
				Design.Colour = Design.Technology.ColourPublic;
			}
		}

		var Identifier = DisplayedItem.GetComponent<PrefabTracker>();

		if (Identifier == null)
		{
			Loggy.LogError($"aaa get rid of non-parented prefabs!, Missing PrefabTracker on item prefab for {DisplayedItem.name} " );
			return;
		}

		bool TooPure = false;

		if (TechnologyLaser.ShotFrom.researchServer.Techweb.TestedPrefabs.Contains(Identifier.ForeverID) == false)
		{
			var Technology = DisplayedItem.GetComponent<ItemResearchPotential>();
			if (Technology.IsTooPure)
			{
				Chat.AddActionMsgToChat(this.gameObject, $"GORDON!!");
				InGameEventsManager.Instance.TriggerSpecificEvent("Resonance cascade", false, true);
				TooPure = true;
			}
			else
			{
				Chat.AddActionMsgToChat(this.gameObject, $"{DisplayedItem.gameObject.ExpensiveName()} Explodes Shooting out many bright beams");
			}

			gameObject.GetComponent<Collider2D>().enabled = false;
			data.HitWorldPosition = GetComponent<UniversalObjectPhysics>().OfficialPosition + new Vector3(0,0.12f,0); //The 0.12 offset centers the lasers on the center of the displayed item.

			foreach (var Design in Technology.TechWebDesigns)
			{

				foreach (var Beam in Design.Beams)
				{

					// Calculate the incoming angle using the source and target positions.
					Vector2 direction = data.BulletShootDirection;
					float incomingAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

					// Add the bending angle to the incoming angle to get the final angle.
					int finalAngle =  Mathf.RoundToInt(incomingAngle + Beam);

					// If the final angle is greater than or equal to 360 or less than 0, wrap it around.
					if (finalAngle >= 360)
					{
						finalAngle -= 360;
					}
					else if (finalAngle < 0)
					{
						finalAngle += 360;
					}

					ShootAtDirection(finalAngle, data, Design);
				}
			}

			gameObject.GetComponent<Collider2D>().enabled = true;
			TechnologyLaser.ShotFrom.researchServer.Techweb.TestedPrefabs.Add(Identifier.ForeverID);
		}
		else
		{
			Chat.AddActionMsgToChat(this.gameObject, $"{DisplayedItem.gameObject.ExpensiveName()} Fizzles and disappears, looks like all useful research has been extracted from this");
		}


		_ = Despawn.ServerSingle(itemStorage.GetIndexedItemSlot(0).ItemObject);
		DisplayedItem = null;
		OnItemChange?.Invoke();


		if (TooPure)
		{
			Explosion.StartExplosion(data.HitWorldPosition.RoundToInt(), 1500);
		}
	}

	private void ShootAtDirection(float rotationToShoot, OnHitDetectData data, TechnologyAndBeams TechnologyAndBeams )
	{
		var range = -1f;

		if (data.BulletObject.TryGetComponent<ProjectileRangeLimited>(out var rangeLimited))
		{
			range = rangeLimited.CurrentDistance;
		}

		var  Projectile = ProjectileManager.InstantiateAndShoot(data.BulletObject.GetComponent<Bullet>().PrefabName,
			VectorExtensions.DegreeToVector2(rotationToShoot), gameObject, null, BodyPartType.None, range, data.HitWorldPosition);

		var Data = Projectile.GetComponent<ContainsResearchData>();
		Data.Initialise(TechnologyAndBeams,data.BulletObject.GetComponent<ContainsResearchData>().ShotFrom);
	}
}
