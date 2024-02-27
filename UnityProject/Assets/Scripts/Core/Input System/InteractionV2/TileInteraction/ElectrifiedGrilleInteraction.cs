using HealthV2;
using Objects;
using TileManagement;
using UnityEngine;
using Systems.Electricity;
using Systems.Explosions;

namespace Tiles
{
	/// <summary>
	/// Interaction logic for grilles. Electrocutes the performer, should that be possible.
	///  Needs to come before other interactions.
	/// </summary>
	[CreateAssetMenu(fileName = "ElectrifiedGrilleInteraction",
		menuName = "Interaction/TileInteraction/ElectrifiedGrilleInteraction")]
	public class ElectrifiedGrilleInteraction : TileInteraction, IBumpableObject
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
				var matrix = interaction.TileChangeManager.MetaTileMap.Layers[LayerType.Grills].Matrix;
				var weaponNA = interaction.Performer.GetComponent<WeaponNetworkActions>();
				weaponNA.ServerPerformMeleeAttack(
						matrix.transform.parent.gameObject, interaction.TargetVector, BodyPartType.None, interaction.BasicTile.LayerType);
			}

			var metaTileMap = interaction.TileChangeManager.MetaTileMap;
			return Electrocute(ServerGetGrilleVoltage(interaction.TargetCellPos, interaction.TileChangeManager.MetaTileMap, metaTileMap.Layers[LayerType.Electrical].Matrix));
		}

		private bool Electrocute(float voltage)
		{
			var performerLHB = interaction.Performer.GetComponent<LivingHealthMasterBase>();
			var electrocutionExposure = new Electrocution(voltage, interaction.WorldPositionTarget, interaction.BasicTile.DisplayName);
			var severity = performerLHB.Electrocute(electrocutionExposure);

			// If the electrocution was painful, return true to stop other interactions.
			if (severity >= LivingShockResponse.Painful) return true;

			return false;
		}

		private void Electrocute(LivingHealthMasterBase target)
		{
			var electrocutionExposure = new Electrocution
			(ServerGetGrilleVoltage(
				target.gameObject.TileLocalPosition().To3Int() + target.playerScript.CurrentDirection.ToLocalVector3Int(),
				target.RegisterTile.TileChangeManager.MetaTileMap,
				target.RegisterTile.Matrix
			), target.gameObject.AssumedWorldPosServer(), ignoreProtection: true); //since you're running head first into this, we skip the protection check on equipment.
			var severity = target.Electrocute(electrocutionExposure);

			// If the electrocution was painful, return true to stop other interactions.
			if (severity >= LivingShockResponse.Painful) SparkUtil.TrySpark(target.gameObject.AssumedWorldPosServer());
		}

		/// <summary>
		/// Checks if the grille's location has exposed floor plating,
		/// that a cable overlap exists there,
		/// and returns the highest voltage detected (for now).
		/// </summary>
		/// <returns>The voltage found on the cable</returns>
		private float ServerGetGrilleVoltage(Vector3Int targetCellPos, MetaTileMap metaTileMap, Matrix matrix)
		{
			// Check if the floor plating is exposed.
			if (metaTileMap.HasTile(targetCellPos, LayerType.Floors)) return 0;
			if (metaTileMap.HasTile(targetCellPos, LayerType.Windows) && metaTileMap.HasTile(targetCellPos, LayerType.Grills)) return 0;

			// Check for cables underneath the grille.
			var eConns = matrix.GetElectricalConnections(targetCellPos);
			if (eConns == null) return 0;

			// Get the highest voltage
			float voltage = 0;
			foreach (var conn in eConns.List)
			{
				ElectricityFunctions.WorkOutActualNumbers(conn);
				if (conn.Data.ActualVoltage > voltage) voltage = conn.Data.ActualVoltage;
			}

			// All checks passed, electrocute the performer!
			return voltage;
		}

		public void OnBump(GameObject bumpedBy, GameObject client)
		{
			if (bumpedBy == null || bumpedBy.TryGetComponent<LivingHealthMasterBase>(out var health) == false) return;
			Electrocute(health);
		}
	}
}
