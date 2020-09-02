using System;
using System.Collections;
using System.Collections.Generic;
using Doors;
using UnityEngine;

public class PressureModule : DoorModuleBase
{
	[SerializeField]
	[Tooltip("If the door shows a pressure warning and is used again within this duration, it will open.")]
	private float warningCooldown = 2f;

	[SerializeField]
	private bool enablePressureWarning = true;

	private int pressureThresholdCaution = 30; // kPa, both thresholds arbitrarily chosen
	private int pressureThresholdWarning = 120;

	private bool warningActive = false;

	[SerializeField]
	private string warningSFX;

	public override ModuleSignal OpenInteraction(HandApply interaction)
	{
		return ModuleSignal.Continue;
	}

	public override ModuleSignal ClosedInteraction(HandApply interaction)
	{
		if (!warningActive)
		{
			if (DoorUnderPressure())
			{
				SoundManager.PlayAtPosition(warningSFX, master.gameObject.AssumedWorldPosServer());
				StartCoroutine(ResetWarning());
				return ModuleSignal.ContinueWithoutDoorStateChange;
			}
		}
		return ModuleSignal.Continue;
	}

	public override bool CanDoorStateChange()
	{
		return true;
	}

	/// <summary>
	///  Checks each side of the door, returns true if not considered safe and updates pressureLevel.
	///  Used to allow the player to be made aware of the pressure difference for safety.
	/// </summary>
	/// <returns></returns>
	private bool DoorUnderPressure()
	{
		if (!enablePressureWarning)
		{
			// Pressure warning system is disabled, so pretend everything is fine.
			return false;
		}

		// Obtain the adjacent tiles to the door.
		var upMetaNode = MatrixManager.GetMetaDataAt(master.RegisterTile.WorldPositionServer + Vector3Int.up);
		var downMetaNode = MatrixManager.GetMetaDataAt(master.RegisterTile.WorldPositionServer + Vector3Int.down);
		var leftMetaNode = MatrixManager.GetMetaDataAt(master.RegisterTile.WorldPositionServer + Vector3Int.left);
		var rightMetaNode = MatrixManager.GetMetaDataAt(master.RegisterTile.WorldPositionServer + Vector3Int.right);

		// Only find the pressure comparison if both opposing sides are atmos. passable.
		// If both sides are not atmos. passable, then we don't care about the pressure difference.
		var vertPressureDiff = 0.0;
		var horzPressureDiff = 0.0;
		if (!upMetaNode.IsOccupied || !downMetaNode.IsOccupied)
		{
			vertPressureDiff = Math.Abs(upMetaNode.GasMix.Pressure - downMetaNode.GasMix.Pressure);
		}
		if (!leftMetaNode.IsOccupied || !rightMetaNode.IsOccupied)
		{
			horzPressureDiff = Math.Abs(leftMetaNode.GasMix.Pressure - rightMetaNode.GasMix.Pressure);
		}

		// Set pressureLevel according to the pressure difference found.
		if (vertPressureDiff >= pressureThresholdWarning || horzPressureDiff >= pressureThresholdWarning)
		{
			return true;
		}
		else if (vertPressureDiff >= pressureThresholdCaution || horzPressureDiff >= pressureThresholdCaution)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	private IEnumerator ResetWarning()
	{
		yield return WaitFor.Seconds(warningCooldown);
		warningActive = false;
	}
}
