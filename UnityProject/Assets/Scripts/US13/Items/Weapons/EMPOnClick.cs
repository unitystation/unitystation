using System;
using UnityEngine;
using US13.Core.Cooldowns;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Items.Others;
using US13.Systems.Explosions;
using Random = UnityEngine.Random;

public class EMPOnClick : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, ICooldown
{

	public FlashLight FlashLight;

	public float DefaultTime => 1;


	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.TargetObject == gameObject) return false;

		var IEmp = interaction.TargetObject.GetComponent<IEmpAble>();
		if (IEmp == null) return false;
		return true;
	}


	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		if (FlashLight != null)
		{
			if (FlashLight.IsOn == false) return;
		}


		if (Cooldowns.TryStart(interaction, this, NetworkSide.Server))
		{
			var IEmp = interaction.TargetObject.GetComponent<IEmpAble>();
			IEmp.OnEmp(Random.Range(2, 500)); //? idk How to balance
		}

	}
}
