using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
///  Extended interaction logic for grilles. Checks if the performer should be electrocuted.
/// </summary>
[CreateAssetMenu(fileName = "GrilleInteraction", menuName = "Interaction/TileInteraction/GrilleInteraction")]
public class GrilleInteraction : DeconstructWhenItemUsed
{
	private float voltage;

	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		// Make sure performer is near the grille.
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (ElectrocutionCriteriaMet(interaction))
		{
			// What's a better practice for the below line?
			var severity = (new Electrocution()).ElectrocutePlayer(
				interaction.Performer, interaction.TargetCellPos, interaction.BasicTile.DisplayName, voltage);
			if (severity >= Electrocution.Severity.Painful)
			{
				return false;
			}
		}

		return base.WillInteract(interaction, side);
	}

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

		// RNG the chance of electrocution. (Somewhat arbitrary value chosen) should be none here
		// float shockChance = Random.value;
		// if (shockChance > 0.6f) return false;

		// All checks passed, electrocute the performer!
		return true;
	}
}
