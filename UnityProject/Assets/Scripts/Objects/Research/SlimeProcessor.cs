using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Objects;
using UnityEngine;

public class SlimeProcessor : MonoBehaviour, ICheckedInteractable<MouseDrop>, ICheckedInteractable<HandApply>
{

	public GameObject MonkeyCube;

	public ObjectContainer container;

	public bool processing = false;


	public int MonkeyCharges = 0;

	public int MonkeyChargesNeeded = 3;

	public SpriteHandler Sprite;

	public void Awake()
	{
		container = this.GetComponent<ObjectContainer>();
	}

	public bool WillInteract(MouseDrop interaction, NetworkSide side)
	{
		if (processing)
			return false;
		if (!Validations.CanInteract(interaction.PerformerPlayerScript, side))
			return false;
		if (!Validations.IsAdjacent(interaction.Performer, interaction.DroppedObject))
			return false;
		if (!Validations.IsAdjacent(interaction.Performer, gameObject))
			return false;
		if (interaction.Performer == interaction.DroppedObject)
			return false;

		var Health = interaction.DroppedObject.GetComponent<LivingHealthMasterBase>();
		if (Health == null) return false;

		if (side == NetworkSide.Server)
		{
			if (Health.IsDead == false) return false;
			if (Health.brain == null) return false;
			var Slime = Health.brain.GetComponent<SlimeCore>();
			if (Slime != null)
			{
				return true;
			}

			var MonkeyBrain = Health.brain.GetComponent<MonkeyBrain>();
			if (MonkeyBrain != null)
			{
				return true;
			}
		}
		else
		{
			return true;
		}



		return false;
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		return interaction.Intent != Intent.Harm;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (processing)
		{
			processing = false;
			Sprite.ChangeSprite(0);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE , Process);
			return;
		}

		if (interaction.IsAltClick)
		{
			container.RetrieveObjects();
		}
		else
		{
			processing = true;
			Sprite.ChangeSprite(1);
			UpdateManager.Add(Process, 3);
		}
	}

	public void ServerPerformInteraction(MouseDrop drop)
	{
		if (container.StoredObjectsCount >= 50)
		{
			return;
		}
		container.StoreObject(drop.DroppedObject);
	}


	public void Process()
	{
		if (container.StoredObjectsCount <= 0)
		{
			processing = false;
			Sprite.ChangeSprite(0);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE , Process);
			return;
		}


		var Remov =  container.StoredObjects.PickRandom();
		//container.StoredObjects.Remove(Remov.Key);

		var Health = Remov.Key.gameObject.GetComponent<LivingHealthMasterBase>();

		var Slime = Health.brain.GetComponent<SlimeCore>();
		if (Slime != null)
		{
			List<SlimeCore> SlimeCores = new List<SlimeCore>();
			foreach (var bp in Health.BodyPartList)
			{
				var core = bp.GetComponent<SlimeCore>();
				if (core != null)
				{
					SlimeCores.Add(core);
				}
			}


			foreach (var core in SlimeCores)
			{
				core.RelatedPart.RemoveInventoryAndBody(this.gameObject.transform.position);
				container.RetrieveObject(core.gameObject);
			}
			
			container.RetrieveObject(Remov.Key);
			_ = Despawn.ServerSingle(Health.gameObject);

			return;
		}

		var MonkeyBrain = Health.brain.GetComponent<MonkeyBrain>();
		if (MonkeyBrain != null)
		{
			MonkeyCharges++;
			if (MonkeyCharges >= MonkeyChargesNeeded)
			{

				Spawn.ServerPrefab(MonkeyCube, transform.position);

			}
			container.RetrieveObject(Remov.Key);
			_ = Despawn.ServerSingle(Health.gameObject);
			return;
		}


	}
}
