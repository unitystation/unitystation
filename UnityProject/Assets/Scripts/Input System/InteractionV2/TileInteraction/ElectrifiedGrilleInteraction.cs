using System;
using UnityEngine;
using System.Diagnostics;

/// <summary>
///  Extended interaction logic for grilles. Checks if the performer should be electrocuted. Needs to come before other interactions.
/// </summary>
[CreateAssetMenu(fileName = "ElectrifiedGrilleInteraction", menuName = "Interaction/TileInteraction/ElectrifiedGrilleInteraction")]
public class ElectrifiedGrilleInteraction : TileInteraction
{
	private float voltage;

	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		// Make sure performer is near the grille.
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (!ElectrocutionCriteriaMet(interaction)) return false;

		var severity = (new Electrocution()).GetPlayerSeverity(interaction.Performer, voltage);

		// If the potential electrocution is painful, return true to stop
		// other interactions.
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
	private bool ElectrocutionCriteriaMet(TileApply interaction)
	{
		Vector3Int targetCellPos = interaction.TargetCellPos;
		Matrix matrix = interaction.Performer.GetComponentInParent<Matrix>();
		MetaTileMap metaDataLayer = matrix.GetComponentInParent<MetaTileMap>();

		// Check if the floor plating is exposed.
		if (metaDataLayer.HasTile(targetCellPos, LayerType.Floors, true)) return false;

		// Check for cables underneath the grille.
		var eConns = matrix.GetElectricalConnections(targetCellPos);
		if (eConns == null) return false;

		// Get the highest voltage and whether there is a machine connector.
		// Unfortunately, the current powernet implementation means the cable will only report
		// a voltage if both ends of the cable are connected to a consumer/producer, it seems.
		// That's why we cannot simply read the connector voltage, and both ends of the cable
		// need to be connected to the powernet.
		bool connectorExists = false;
		this.voltage = 0.0f;
		foreach (var conn in eConns)
		{
			if (conn.InData.Categorytype == PowerTypeCategory.LowMachineConnector
				|| conn.InData.Categorytype == PowerTypeCategory.MediumMachineConnector
				|| conn.InData.Categorytype == PowerTypeCategory.HighMachineConnector)
			{
				connectorExists = true;
				continue; // Connector won't report a voltage
			}
			if (conn.Data.ActualVoltage > this.voltage) this.voltage = conn.Data.ActualVoltage;
		}

		// Check that there is a machine connector.
		if (!connectorExists) return false;

		// All checks passed, electrocute the performer!
		return true;
	}
}
