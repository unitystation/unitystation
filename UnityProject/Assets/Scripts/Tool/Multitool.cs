using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Multitool : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, IInteractable<HandActivate>
{

	public APC APCBuffer;
	public List<ConveyorBelt> ConveyorBeltBuffer = new List<ConveyorBelt>();

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

			//conveyorbelt
			ConveyorBelt conveyorBelt = interaction.TargetObject.GetComponent<ConveyorBelt>();
			if (conveyorBelt != null)
			{
				return true;
			}
			ConveyorBeltSwitch conveyorBeltSwitch = interaction.TargetObject.GetComponent<ConveyorBeltSwitch>();
			if (conveyorBeltSwitch != null)
			{
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
					Chat.AddExamineMsgFromServer(interaction.Performer, "You set the power device to use the APC in the buffer");
					PoweredDevice.SetAPC(APCBuffer);
				}
				else {
					Chat.AddExamineMsgFromServer(interaction.Performer, "Your buffer is empty fill it with something");
				}
			}
			APC _APC = interaction.TargetObject.GetComponent<APC>();
			if (_APC != null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "You set the internal buffer of the multitool to the APC");
				APCBuffer = _APC;
			}

			//conveyorbelt
			ConveyorBelt conveyorBelt = interaction.TargetObject.GetComponent<ConveyorBelt>();
			if (conveyorBelt != null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "You set the internal buffer of the multitool to the Conveyor Belt");
				ConveyorBeltBuffer.Add(conveyorBelt);
			}
			ConveyorBeltSwitch conveyorBeltSwitch = interaction.TargetObject.GetComponent<ConveyorBeltSwitch>();
			if (conveyorBeltSwitch != null)
			{
				if (ConveyorBeltBuffer != null)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "You set the Conveyor Belt Switch to use the Conveyor Belt in the buffer");
					conveyorBeltSwitch.AddConveyorBelt(ConveyorBeltBuffer);
				}
				else
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "Your Conveyor Belt buffer is empty fill it with something");
				}
			}
		}
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		Chat.AddExamineMsgFromServer(interaction.Performer, "Conveyor Belt buffer cleared");
		ConveyorBeltBuffer.Clear();
	}
}
