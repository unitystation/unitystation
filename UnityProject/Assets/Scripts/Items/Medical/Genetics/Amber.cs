using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Amber : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	public Stackable Stacking;

	public void Start()
	{
		Stacking = this.GetComponent<Stackable>();
	}

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.TargetObject == gameObject) return false;
		if ( Validations.HasComponent<DNAConsole>(interaction.TargetObject) == false) return false;
		return true;
	}


	public void ServerPerformInteraction(PositionalHandApply interaction)
	{

		var DNAConsole = interaction.TargetObject.GetComponent<DNAConsole>();
		if (DNAConsole != null)
		{
			DNAConsole.AddAmber();
			if (Stacking != null)
			{
				Stacking.ServerConsume(1);
			}
			else
			{
				_ = Despawn.ServerSingle(gameObject);
			}
		}
	}
}
