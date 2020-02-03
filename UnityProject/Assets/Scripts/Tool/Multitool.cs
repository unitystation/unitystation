using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Multitool : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{

	public APC APCBuffer;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		// Use default interaction checks
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (!Validations.IsTarget(gameObject, interaction))
		{
			APCPoweredDevice PoweredDevice = interaction.TargetObject.GetComponent<APCPoweredDevice>();
			if (PoweredDevice != null) { 
				return true;
			}
			APC _APC = interaction.TargetObject.GetComponent<APC>();
			if (_APC != null) { 
				return true;
			}
		}
		return false;
	}


	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		if (!Validations.IsTarget(gameObject, interaction))
		{
			APCPoweredDevice PoweredDevice = interaction.TargetObject.GetComponent<APCPoweredDevice>();
			if (PoweredDevice != null)
			{
				if (APCBuffer != null)
				{					PoweredDevice.SetAPC(APCBuffer);
				}
			}
			APC _APC = interaction.TargetObject.GetComponent<APC>();
			if (_APC != null)
			{
				APCBuffer = _APC;
			}
		}

	}
}
