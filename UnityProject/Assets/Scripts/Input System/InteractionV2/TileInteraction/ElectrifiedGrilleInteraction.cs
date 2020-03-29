using System;
using UnityEngine;

using System.Linq;
using Mirror;
//using System.Collections.Generic;

/// <summary>
///  Extended interaction logic for grilles.
///  Checks if the performer should be electrocuted. Needs to come before other interactions.
/// </summary>
[CreateAssetMenu(fileName = "ElectrifiedGrilleInteraction",
	menuName = "Interaction/TileInteraction/ElectrifiedGrilleInteraction")]
public class ElectrifiedGrilleInteraction : TileInteraction//, INodeControl
{
	[SyncVar(hook = nameof(SyncVoltage))]
	public float voltageSync;
	public ElectricalNodeControl ElectricalNodeControl;

	private float voltage = 0;

	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		Debug.LogError("Running " + side.ToString() + "-side!");

		// Make sure performer is near the grille.
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (!ElectrocutionCriteriaMet(interaction, side)) return false;

		Debug.LogError("Electrocution criteria met for " + side.ToString() + ", check voltage: " + voltage.ToString());

		var severity = (new Electrocution()).GetPlayerSeverity(interaction.Performer, voltage);

		// If the potential electrocution is painful, return true to stop other interactions.
		if (severity > Electrocution.Severity.Mild) return true;

		return false;
	}

	public override void ServerPerformInteraction(TileApply interaction)
	{
		(new Electrocution()).ElectrocutePlayer(
			interaction.Performer, interaction.TargetCellPos, interaction.BasicTile.DisplayName, voltage);
	}

	/// <summary>
    /// Checks if the grille's location has exposed floor plating,
    /// that cables and a machine connector exists there, and writes the
    /// highest voltage detected to the class property voltage.
    /// </summary>
    /// <param name="interaction">TileApply interaction</param>
    /// <returns>Boolean</returns>
	private bool ElectrocutionCriteriaMet(TileApply interaction, NetworkSide side)
	{
		Debug.LogError("Checking electrocution criteria...");

		Vector3Int targetCellPos = interaction.TargetCellPos;
		Matrix matrix = interaction.Performer.GetComponentInParent<Matrix>();
		MetaTileMap metaDataLayer = matrix.GetComponentInParent<MetaTileMap>();

		// Check if the floor plating is exposed.
		if (metaDataLayer.HasTile(targetCellPos, LayerType.Floors, true)) return false;

		// Check for cables underneath the grille.
		var eConns = matrix.GetElectricalConnections(targetCellPos);
		Debug.LogError("eConns count: " + eConns.Count());
		if (eConns == null) return false;

		// Get the highest voltage and whether there is a machine connector.
		// The current powernet implementation means the cable
		// will only report a voltage if there is current flow, it seems.
		// That's why we cannot simply read the connector voltage, and both ends of the cable
		// need to be connected to the powernet.
		bool connectorExists = false;
		// Unfortunately client won't report conn.InData.Categorytype and conn.Data.ActualVoltage correctly.
		if (side == NetworkSide.Client)
        {
			foreach (var conn in eConns)
            {
				Debug.LogError(conn);
				if (conn.ToString().Contains("MachineConnector"))
                {
					connectorExists = true;
					continue; // Connector won't report a voltage.
                }
				float resistance = ElectricityFunctions.WorkOutActualNumbers(conn).Item1;
				float actualVoltage = ElectricityFunctions.WorkOutActualNumbers(conn).Item2;
				float current = ElectricityFunctions.WorkOutActualNumbers(conn).Item3;
				float myVoltage = MyOwnVoltage(conn);
				var getENCComponent = conn.GetComponent<ElectricalNodeControl>();
				var getENCComponentInParent = conn.GetComponentInParent<ElectricalNodeControl>();
				Debug.LogError(" > Actual voltage found: " + actualVoltage.ToString());
				Debug.LogError(" > Resistance: " + resistance.ToString());
				Debug.LogError(" > Current: " + current.ToString());
				Debug.LogError(" > My voltage found: " + myVoltage.ToString());
				Debug.LogError(" > Voltage update: " + conn.Data.ActualVoltage.ToString());
				Debug.LogError(" > Alt. current: " + conn.Data.CurrentInWire.ToString());
				Debug.LogError(" > Alt. resistance: " + conn.Data.EstimatedResistance.ToString());
				Debug.LogError(" > GetENCComponent: " + ((getENCComponent) ? getENCComponent.ToString() : "null"));
				Debug.LogError(" > GetENCComponentInParent: " + ((getENCComponentInParent) ? getENCComponentInParent.ToString() : "null"));
				if (actualVoltage > voltage) voltage = actualVoltage;
            }
        }
		else
        {
			foreach (var conn in eConns)
            {
				Debug.LogError(conn);
				if (conn.InData.Categorytype == PowerTypeCategory.LowMachineConnector
					|| conn.InData.Categorytype == PowerTypeCategory.MediumMachineConnector
					|| conn.InData.Categorytype == PowerTypeCategory.HighMachineConnector)
                {
					connectorExists = true;
					continue; // Connector won't report a voltage.
                }

				Debug.LogError(" > New voltage found: " + conn.Data.ActualVoltage.ToString());
				if (conn.Data.ActualVoltage > voltage) voltage = conn.Data.ActualVoltage;
			}
        }

		// Check that there is a machine connector.
		if (!connectorExists) return false;

		// All checks passed, electrocute the performer!
		return true;
	}

	//public void PowerNetworkUpdate()
	//{
	//	SyncVoltage(voltageSync, ElectricalNodeControl.Node.Data.ActualVoltage);
	//}

	private void SyncVoltage(float oldVoltage, float newVoltage)
	{
		voltageSync = newVoltage;
	}


	// Copy of WorkOutVoltage();
	private float MyOwnVoltage(ElectricalOIinheritance electricItem)
    {
		Debug.LogError("Running MyOwnVoltage...");
		float voltage = 0;

		foreach (var supply in electricItem.Data.SupplyDependent)
        {
			Debug.LogError(" > > " + supply.ToString());
			voltage += supply.Value.SourceVoltages;
        }

		electricItem.Data.ActualVoltage = voltage;

		return voltage;
    }
}
