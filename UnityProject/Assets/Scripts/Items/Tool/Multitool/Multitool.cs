using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Electricity;
using System.Text;

// TODO: namespace me
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
		if (Validations.IsTarget(gameObject, interaction))
		{
			return;
		}

		var multitoolBases = interaction.TargetObject.GetComponents<ISetMultitoolBase>();
		foreach (var multitoolBase in multitoolBases)
		{
			if (Buffer == null || MultiMaster)
			{
				if (multitoolBase is ISetMultitoolMaster master)
				{
					ConfigurationBuffer = master.ConType;
					ListBuffer.Add(master);
					MultiMaster = master.MultiMaster;
					Chat.AddExamineMsgFromServer(
						interaction.Performer,
						$"You add the <b>{interaction.TargetObject.ExpensiveName()}</b> to the multitool's master buffer.");
					return;
				}
			}

			if (Buffer == null)
			{
				continue;
			}

			if (ConfigurationBuffer != multitoolBase.ConType)
			{
				continue;
			}

			switch (multitoolBase)
			{
				case ISetMultitoolSlave slave:
					slave.SetMaster(Buffer);
					Chat.AddExamineMsgFromServer(interaction.Performer,
						$"You connect the <b>{interaction.TargetObject.ExpensiveName()}</b> " +
						$"to the master device <b>{(Buffer as Component)?.gameObject.ExpensiveName()}</b>.");
					return;
				case ISetMultitoolSlaveMultiMaster slaveMultiMaster:
					slaveMultiMaster.SetMasters(ListBuffer);
					Chat.AddExamineMsgFromServer(interaction.Performer,
						$"You connect the <b>{interaction.TargetObject.ExpensiveName()}</b> to the master devices in the buffer.");
					return;
				default:
					Chat.AddExamineMsgFromServer(interaction.Performer,
						"This only seems to have the capability of <b>writing</b> to the buffer.");
					return;
			}
		}

		PrintElectricalThings(interaction);
	}

	public void PrintElectricalThings(PositionalHandApply interaction)
	{
		Vector3Int worldPosInt = interaction.WorldPositionTarget.To2Int().To3Int();
		MatrixInfo matrixinfo = MatrixManager.AtPoint(worldPosInt, true);
		var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrixinfo);
		var matrix = interaction.Performer.GetComponentInParent<Matrix>();
		var electricalNodes = matrix.GetElectricalConnections(localPosInt);

		string message = "The multitool couldn't find anything electrical here.";
		if (electricalNodes.Count > 0)
		{
			message = "The multitool's display lights up:\n";
		}

		StringBuilder sb = new StringBuilder(message);
		foreach (var node in electricalNodes)
		{
			sb.AppendLine(node.ShowInGameDetails());
		}

		electricalNodes.Clear();
		ElectricalPool.PooledFPCList.Add(electricalNodes);
		Chat.AddExamineMsgFromServer(interaction.Performer, sb.ToString());
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		Chat.AddExamineMsgFromServer(interaction.Performer, "You clear the multitool's internal buffer.");
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
	GeneralSwitch,
	Turret
}
