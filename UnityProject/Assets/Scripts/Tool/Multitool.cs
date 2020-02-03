using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Multitool : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{

	public APC APCBuffer;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		// Use default interaction checks
		if (!Validations.HasTarget(interaction)) return false;
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
				{
					Chat.AddExamineMsgToClient("You set the power device to use the APC in the buffer");					PoweredDevice.SetAPC(APCBuffer);
				}
				else { 
					Chat.AddExamineMsgToClient("Your buffer is empty fill it with something");
				}
			}
			APC _APC = interaction.TargetObject.GetComponent<APC>();
			if (_APC != null)
			{
				Chat.AddExamineMsgToClient("You set the internal buffer of the multitool to the APC");
				APCBuffer = _APC;
			}
		}

	}
}
