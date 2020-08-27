using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Multitool : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, IInteractable<HandActivate>
{
	public List<ISetMultitoolMaster> ListBuffer = new List<ISetMultitoolMaster>();
	public bool MultiMaster = false;

	public ISetMultitoolMaster Buffer
	{
		get
		{
			if (ListBuffer.Count > 0)
			{
				return ListBuffer[0];
			}

			return null;
		}
	}

	public MultitoolConnectionType ConfigurationBuffer = MultitoolConnectionType.Empty;

	//public APC APCBuffer;
	//public List<ConveyorBelt> ConveyorBeltBuffer = new List<ConveyorBelt>();

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		// Use default interaction checks
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (!Validations.IsTarget(gameObject, interaction))
		{
			ISetMultitoolBase MultitoolBase = interaction.TargetObject.GetComponent<ISetMultitoolBase>();
			if (MultitoolBase != null)
			{
				return true;
			}

			//conveyorbelt
			/*ConveyorBelt conveyorBelt = interaction.TargetObject.GetComponent<ConveyorBelt>();
			if (conveyorBelt != null)
			{
				return true;
			}
			ConveyorBeltSwitch conveyorBeltSwitch = interaction.TargetObject.GetComponent<ConveyorBeltSwitch>();
			if (conveyorBeltSwitch != null)
			{
				return true;
			}*/
			return true;
		}

		return false;
	}


	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		if (!Validations.IsTarget(gameObject, interaction))
		{
			var MultitoolBases = interaction.TargetObject.GetComponents<ISetMultitoolBase>();
			foreach (var MultitoolBase in MultitoolBases)
			{
				if (Buffer == null || MultiMaster)
				{
					ISetMultitoolMaster Master = (MultitoolBase as ISetMultitoolMaster);
					if (Master != null)
					{
						ConfigurationBuffer = Master.ConType;
						ListBuffer.Add(Master);
						MultiMaster = Master.MultiMaster;
						Chat.AddExamineMsgFromServer(interaction.Performer,
							"You add the master component " + interaction.TargetObject.ExpensiveName() +
							" to the Multi-tools buffer");
						return;
					}
				}

				if (Buffer != null)
				{
					if (ConfigurationBuffer == MultitoolBase.ConType)
					{
						ISetMultitoolSlave Slave = (MultitoolBase as ISetMultitoolSlave);
						if (Slave != null)
						{
							Slave.SetMaster(Buffer);
							Chat.AddExamineMsgFromServer(interaction.Performer,
								"You set the " + interaction.TargetObject.ExpensiveName() + " to use the " +
								(Buffer as Component)?.gameObject.ExpensiveName() + " in the buffer");
							return;
						}

						ISetMultitoolSlaveMultiMaster SlaveMultiMaster =
							(MultitoolBase as ISetMultitoolSlaveMultiMaster);
						if (SlaveMultiMaster != null)
						{
							SlaveMultiMaster.SetMasters(ListBuffer);
							Chat.AddExamineMsgFromServer(interaction.Performer,
								"You set the" + interaction.TargetObject.ExpensiveName() +
								" to use the devices in the buffer");
							return;
						}

						Chat.AddExamineMsgFromServer(interaction.Performer,
							"This only seems to have the capability of accepting Writing to buffer");
						return;
					}
				}
			}


			//conveyorbelt
			/*
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
			*/

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
		foreach (var D in MetaDataNode)
		{
			ToReturn = ToReturn + D.ShowInGameDetails() + "\n";
		}

		MetaDataNode.Clear();
		ElectricalPool.PooledFPCList.Add(MetaDataNode);
		Chat.AddExamineMsgFromServer(interaction.Performer, ToReturn);
	}


	public void ServerPerformInteraction(HandActivate interaction)
	{
		Chat.AddExamineMsgFromServer(interaction.Performer, "You Clear internal buffer");
		ListBuffer.Clear();
		MultiMaster = false;
		ConfigurationBuffer = MultitoolConnectionType.Empty;
	}
}

public enum MultitoolConnectionType
{
	Empty,
	APC,
	Conveyor,
	BoilerTurbine,
	ReactorChamber,
	FireAlarm,
	LightSwitch,
	DoorButton,
	GeneralSwitch
}