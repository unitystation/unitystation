using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Tilemaps.Behaviours.Layers;
using UnityEngine;
using UnityEngine.Rendering;

public class VacpackGun : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{

	[SerializeField] private ItemStorage storage;
	public VacpackBackpack VacpackBackpack;

	private void Start()
	{
		if (storage == null) storage = gameObject.PickupableOrNull().ItemSlot.ItemStorage;
		VacpackBackpack = storage.GetComponent<VacpackBackpack>();

	}

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (Validations.CanInteract(interaction.PerformerPlayerScript,side, false) == false) return false;

		var Distance = (interaction.Performer.AssumedWorldPosServer() - interaction.WorldPositionTarget.To3()).magnitude;

		if (Distance > 4.5f) return false;


		var hit =  MatrixManager.Linecast(interaction.Performer.AssumedWorldPosServer(),
			LayerTypeSelection.Walls | LayerTypeSelection.Windows, null, interaction.WorldPositionTarget.To3());

		if (hit.ItHit) return false;

		if (interaction.TargetObject == gameObject) return false;
		if (interaction.TargetObject == null) return false;
		if (interaction.TargetObject != null)
		{
			var health = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
			if (health == null)
			{
				var Matrix = interaction.TargetObject.GetComponent<NetworkedMatrix>();
				if (Matrix == null) return false;
			}
		}

		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		var Matrix = interaction.TargetObject.GetComponent<NetworkedMatrix>();
		if (Matrix == null)
		{
			var Object = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();

			if (Object.brain == null) return;
			var SlimeCore = Object.brain.GetComponent<SlimeCore>();

			if (SlimeCore == null)
			{
				var MonkeyBrain = Object.brain.GetComponent<MonkeyBrain>();
				if (MonkeyBrain == null)
				{
					return;
				}
			}

			VacpackBackpack.TryStore(interaction.TargetObject);
		}
		else
		{
			VacpackBackpack.TryReleasedAt(interaction.WorldPositionTarget);
		}
	}
}
