using UnityEngine;

/// <summary>
/// Interaction logic for grilles. Electrocutes the performer, should that be possible.
///  Needs to come before other interactions.
/// </summary>
[CreateAssetMenu(fileName = "ElectrifiedGrilleInteraction",
	menuName = "Interaction/TileInteraction/ElectrifiedGrilleInteraction")]
public class ElectrifiedGrilleInteraction : TileInteraction
{
	TileApply interaction;

	// We electrocute the performer in WillInteract() instead of ServerPerformInteraction()
	// because we would like for the performer to be able to perform other interactions
	// if the electrocution was merely mild.
	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		this.interaction = interaction;

		if (!DefaultWillInteract.Default(interaction, side)) return false;

		// Let only the server continue, as ServerGetGrilleVoltage() depends on the server-side-only electrical system.
		if (side == NetworkSide.Client) return true;

		bool othersWillInteract = false;
		foreach (var otherInteraction in interaction.BasicTile.TileInteractions)
		{
			if (otherInteraction == this) continue;
			if (otherInteraction.WillInteract(interaction, side))
			{
				othersWillInteract = true;
				break;
			}
		}

		// If no other grille interactions were available, and the performer intends to harm: attack!
		// This is somewhat of a hack; other interactions should be handled by InteractionUtils,
		// however the server will not process other interactions (only other tile interactions) if this WillInteract()
		// returns true on the client (which it must, as the electrical system runs server-side only).
		// This means attack interactions would be incorrectly ignored.
		if (!othersWillInteract && (interaction.Intent == Intent.Harm || interaction.HandObject != null))
		{
			var matrix = interaction.TileChangeManager.MetaTileMap.Layers[LayerType.Grills].matrix;
			var weaponNA = interaction.Performer.GetComponent<WeaponNetworkActions>();
			weaponNA.ServerPerformMeleeAttack(
					matrix.transform.parent.gameObject, interaction.TargetVector, BodyPartType.None, interaction.BasicTile.LayerType);
		}

		return Electrocute(ServerGetGrilleVoltage());
	}

	bool Electrocute(float voltage)
	{
		var performerLHB = interaction.Performer.GetComponent<LivingHealthBehaviour>();
		var electrocutionExposure = new Electrocution(voltage, interaction.WorldPositionTarget, interaction.BasicTile.DisplayName);
		var severity = performerLHB.Electrocute(electrocutionExposure);

		// If the electrocution was painful, return true to stop other interactions.
		if (severity >= LivingShockResponse.Painful) return true;

		return false;
	}

	/// <summary>
	/// Checks if the grille's location has exposed floor plating,
	/// that a cable overlap exists there,
	/// and returns the highest voltage detected (for now).
	/// </summary>
	/// <returns>The voltage found on the cable</returns>
	private float ServerGetGrilleVoltage()
	{
		Vector3Int targetCellPos = interaction.TargetCellPos;
		MetaTileMap metaTileMap = interaction.TileChangeManager.MetaTileMap;
		Matrix matrix = metaTileMap.Layers[LayerType.Underfloor].matrix;

		// Check if the floor plating is exposed.
		if (metaTileMap.HasTile(targetCellPos, LayerType.Floors, true)) return 0;

		// Check for cables underneath the grille.
		var eConns = matrix.GetElectricalConnections(targetCellPos);
		if (eConns == null) return 0;

		// Get the highest voltage and whether there is a connection overlap.
		// The current powernet implementation means the cable
		// will only report a voltage if there is current flow, it seems.
		// That's why we cannot simply the overlap connection's voltage,
		// and both ends of the cable need to be connected to the powernet.
		//
		// One possible workaround is to allow a Connection.Overlap to draw
		// a small amount of current, so that it registers a voltage.
		// Then, we don't need to check for the highest voltage and not worry
		// about whether the overlap is actually connected to a live cable.
		bool overlapExists = false;
		float voltage = 0;
		foreach (var conn in eConns)
		{
			if (conn.WireEndA == Connection.Overlap || conn.WireEndB == Connection.Overlap)
			{
				overlapExists = true;
			}

			ElectricityFunctions.WorkOutActualNumbers(conn);
			if (conn.Data.ActualVoltage > voltage) voltage = conn.Data.ActualVoltage;
		}

		// Check that there is a cable overlap.
		if (!overlapExists) return 0;

		// All checks passed, electrocute the performer!
		return voltage;
	}
}
