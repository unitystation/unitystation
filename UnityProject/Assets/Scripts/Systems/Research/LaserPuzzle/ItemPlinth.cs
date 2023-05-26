using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Util;
using Weapons.Projectiles;
using Weapons.Projectiles.Behaviours;

public class ItemPlinth : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>, IOnHitDetect
{
	private UniversalObjectPhysics UniversalObjectPhysics;

	public event Action OnItemChange;


	public Pickupable DisplayedItem;

	[SyncVar]
	public bool HasItem = false;

	public void Awake()
	{
		UniversalObjectPhysics = this.GetComponentCustom<UniversalObjectPhysics>();
	}

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
			HasItem = false;
			UniversalObjectPhysics.BuckleObjectToThis(null);
			Inventory.ServerAdd(DisplayedItem, interaction.HandSlot);

			OnItemChange?.Invoke();
		}
		else
		{
			HasItem = true;
			DisplayedItem = interaction.HandSlot.Item;
			Inventory.ServerDrop(interaction.HandSlot);

			UniversalObjectPhysics.BuckleObjectToThis(DisplayedItem.UniversalObjectPhysics);
			OnItemChange?.Invoke();
		}

	}

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
				Design.Technology = TechnologyLaser.ShotFrom.researchServer.Techweb.nodes.PickRandom().technology;
				Design.Colour = Design.Technology.Colour;
			}
		}

		var Identifier = DisplayedItem.GetComponent<PrefabTracker>();

		if (Identifier == null)
		{
			Logger.LogError($"aaa get rid of non-parented prefabs!, Missing PrefabTracker on item prefab for {DisplayedItem.name} " );
			return;
		}

		if (TechnologyLaser.ShotFrom.researchServer.Techweb.TestedPrefabs.Contains(Identifier.ForeverID) == false)
		{
			Chat.AddActionMsgToChat(this.gameObject, $"{DisplayedItem.gameObject.ExpensiveName()} Explodes Shooting out many bright beams");

			gameObject.GetComponent<Collider2D>().enabled = false;

			foreach (var Design in DisplayedItem.GetComponent<ItemResearchPotential>().TechWebDesigns)
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


		//TODO Destroy item here

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
