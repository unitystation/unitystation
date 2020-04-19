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
			return true;
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

			PrintElectricalThings(interaction);
		}
	}

	public void PrintElectricalThings(PositionalHandApply interaction)
	{
		Vector3Int worldPosInt = interaction.WorldPositionTarget.To2Int().To3Int();
		MatrixInfo matrixinfo = MatrixManager.AtPoint(worldPosInt, true);
		var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrixinfo);
		var matrix = interaction.Performer.GetComponentInParent<Matrix>();
		var MetaDataNode = matrix.GetElectricalConnections(localPosInt);
		string ToReturn = "The Multitool Display lights up with \n"
		                  + "Number of electrical objects present : " + MetaDataNode.Count + "\n";
		foreach (var D in MetaDataNode) {
			ToReturn = ToReturn + D.ShowInGameDetails() + "\n";
		}
		MetaDataNode.Clear();
		ElectricalPool.PooledFPCList.Add(MetaDataNode);
		Chat.AddExamineMsgFromServer(interaction.Performer, ToReturn);
	}


	public void ServerPerformInteraction(HandActivate interaction)
	{
		Chat.AddExamineMsgFromServer(interaction.Performer, "Conveyor Belt buffer cleared");
		ConveyorBeltBuffer.Clear();
	}
}
