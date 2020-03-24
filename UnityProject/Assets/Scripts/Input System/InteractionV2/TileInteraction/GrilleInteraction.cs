using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
///  Extended interaction logic for grilles. Checks if the performer should be electrocuted.
/// </summary>
[CreateAssetMenu(fileName = "GrilleInteraction", menuName = "Interaction/TileInteraction/GrilleInteraction")]
public class GrilleInteraction : DeconstructWhenItemUsed
{
	private TileApply interaction;
	private float voltage;

	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		this.interaction = interaction;

		if (!DefaultWillInteract.Default(interaction, side)) return false;

		// If true, electrocute the performer and cancel the interaction.
		if (ElectrocutionCriteriaMet())
		{
			Electrocute();
			return false;
		}
		else
        {
			return base.WillInteract(interaction, side);
        }
	}

	private bool ElectrocutionCriteriaMet()
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
		// Unfortuantely, the current powernet implementation means the cable will only report
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

		// Check if the voltage is sufficient enough.
		if (voltage < 120) return false;

		// Check if performer is insulated from electric shocks.
		var gloves = interaction.PerformerPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.hands).ItemAttributes;
		if (gloves != null && gloves.HasTrait(CommonTraits.Instance.Insulated)) return false;

		// RNG the chance of electrocution. (Somewhat arbitrary value chosen)
		float shockChance = Random.value;
		if (shockChance > 0.6f) return false;

		// All checks passed, electrocute the performer!
		return true;
	}

	private void Electrocute()
    {
		// TODO: Implement electrocution animation
		SoundManager.PlayAtPosition("Sparks#", interaction.WorldPositionTarget, interaction.Performer);
		interaction.Performer.GetComponent<RegisterPlayer>().ServerStun();
		SoundManager.PlayAtPosition("Bodyfall", interaction.WorldPositionTarget, interaction.Performer);
		// Remove the message when the shock animation has been implemented as it should be obvious enough.
		Chat.AddExamineMsgFromServer(interaction.Performer, "You were electrocuted!");

		// TODO: Add burn damage performer
	}
}
